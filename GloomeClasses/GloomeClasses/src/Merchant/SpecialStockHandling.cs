using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace GloomeClasses.src.Merchant {
    public class SpecialStockHandling {
        //
        // Merchant Player goes up to Trader. Asks trader for their special stock using a unique chat option only available for players with the trait.
        // This then causes the Trader Entity to receieve the opensilvertonguetrade action, which will grab the trader's current inventory and store it for later to replace it with the special inventory.
        // - Save inventory to watched attributes under the temp main inventory, then set the Trader's inventory to the special one.
        // - If the special stock inventory for this trader is null, create a new inventory and store it until refresh or unload/shutdown
        // - This needs a prefix on Dialog_DialogTriggers
        // The player can buy or sell whatever for this inventory! When closed, it needs to then save the special inventory's data and then reset the trader's inventory to it's original.
        // - Save the special stock inventory to it's own tree, and reload the main inventory back onto the entity and clear the temp main tree
        // - This needs a transpiler on OnGameTick after the inventory is closed
        //
        // When it comes time to refresh it, clear the recorded inventory to allow for a new one to be created next time a merchant attempts to access it.
        // - Access the watched attributes on serverside and just remove the tree holding the info.
        // - Prefix on RefreshBuyingSellingInventory
        //

        public const string SpecialStockAttribute = "specialStockInventory";
        public const string TempMainAttribute = "tempMainInventory";

        public static void LoadAndOpenSpecialStock(ref string value, EntityTradingHumanoid trader) {
            ITreeAttribute tree = new TreeAttribute();
            trader.Inventory.ToTreeAttributes(tree);
            trader.WatchedAttributes[TempMainAttribute] = tree;

            if (trader.WatchedAttributes.HasAttribute(SpecialStockAttribute)) {
                trader.Inventory.FromTreeAttributes(trader.WatchedAttributes.GetTreeAttribute(SpecialStockAttribute));
                trader.WatchedAttributes.MarkAllDirty();
            } else {
                RefreshSpecialStock(trader, trader.Inventory);
            }

            value = "opentrade";
        }

        //Copied over from EntityTradingHumanoid's RefreshBuyingSellingInventory! Will need updating if that ever changes.
        private static void RefreshSpecialStock(EntityTradingHumanoid trader, InventoryTrader specialStock) {
            if (trader.TradeProps == null) return;

            trader.TradeProps.Buying.List.Shuffle(trader.World.Rand);
            int buyingQuantity = Math.Min(trader.TradeProps.Buying.List.Length, trader.TradeProps.Buying.MaxItems);

            trader.TradeProps.Selling.List.Shuffle(trader.World.Rand);
            int sellingQuantity = Math.Min(trader.TradeProps.Selling.List.Length, trader.TradeProps.Selling.MaxItems);

            // Pick quantity items from the trade list that the trader doesn't already sell
            // Slots 0..15: Selling slots
            // Slots 16..19: Buying cart
            // Slots 20..35: Buying slots
            // Slots 36..39: Selling cart
            // Slot 40: Money slot

            Stack<TradeItem> newBuyItems = new Stack<TradeItem>();
            Stack<TradeItem> newsellItems = new Stack<TradeItem>();

            ItemSlotTrade[] sellingSlots = specialStock.SellingSlots;
            ItemSlotTrade[] buyingSlots = specialStock.BuyingSlots;

            #region Avoid duplicate sales

            string[] ignoredAttributes = GlobalConstants.IgnoredStackAttributes.Append("condition");

            for (int i = 0; i < trader.TradeProps.Selling.List.Length; i++) {
                if (newsellItems.Count >= sellingQuantity) break;

                TradeItem item = trader.TradeProps.Selling.List[i];
                if (!item.Resolve(trader.World, "specialStockTradeItem resolver")) continue;

                bool alreadySelling = sellingSlots.Any((slot) => slot?.Itemstack != null && slot.TradeItem.Stock > 0 && item.ResolvedItemstack?.Equals(trader.World, slot.Itemstack, ignoredAttributes) == true);

                if (!alreadySelling) {
                    newsellItems.Push(item);
                }
            }

            for (int i = 0; i < trader.TradeProps.Buying.List.Length; i++) {
                if (newBuyItems.Count >= buyingQuantity) break;

                TradeItem item = trader.TradeProps.Buying.List[i];
                if (!item.Resolve(trader.World, "specialStockTradeItem resolver")) continue;

                bool alreadySelling = buyingSlots.Any((slot) => slot?.Itemstack != null && slot.TradeItem.Stock > 0 && item.ResolvedItemstack?.Equals(trader.World, slot.Itemstack, ignoredAttributes) == true);

                if (!alreadySelling) {
                    newBuyItems.Push(item);
                }
            }
            #endregion

            replaceSpecialStockItems(newBuyItems, buyingSlots, buyingQuantity, EnumTradeDirection.Buy, trader);
            replaceSpecialStockItems(newsellItems, sellingSlots, sellingQuantity, EnumTradeDirection.Sell, trader);

            ITreeAttribute tree = trader.WatchedAttributes.GetOrAddTreeAttribute(SpecialStockAttribute);
            specialStock.ToTreeAttributes(tree);
            trader.Inventory = specialStock;
            trader.WatchedAttributes.MarkAllDirty();
        }

        private static void replaceSpecialStockItems(Stack<TradeItem> newItems, ItemSlotTrade[] slots, int quantity, EnumTradeDirection tradeDir, EntityTradingHumanoid trader) {
            HashSet<int> refreshedSlots = new HashSet<int>();

            for (int i = 0; i < quantity; i++) {
                if (newItems.Count == 0) break;

                TradeItem newTradeItem = newItems.Pop();

                if (newTradeItem.ResolvedItemstack.Collectible is ITradeableCollectible itc) {
                    if (!itc.ShouldTrade(trader, newTradeItem, tradeDir)) {
                        i--;
                        continue;
                    }
                }

                int duplSlotIndex = slots.IndexOf((bslot) => bslot.Itemstack != null && bslot.TradeItem.Stock == 0 && newTradeItem?.ResolvedItemstack.Equals(trader.World, bslot.Itemstack, GlobalConstants.IgnoredStackAttributes) == true);

                ItemSlotTrade intoSlot;

                // The trader already sells this but is out of stock - replace
                if (duplSlotIndex != -1) {
                    intoSlot = slots[duplSlotIndex];
                    refreshedSlots.Add(duplSlotIndex);
                } else {
                    while (refreshedSlots.Contains(i)) i++;
                    if (i >= slots.Length) break;
                    intoSlot = slots[i];
                    refreshedSlots.Add(i);
                }

                var titem = newTradeItem.Resolve(trader.World);
                if (titem.Stock > 0) {
                    intoSlot.SetTradeItem(titem);
                    intoSlot.MarkDirty();
                }
            }
        }

        public static void PutAwaySpecialStock(EntityTradingHumanoid trader) {
            if (trader.interactingWithPlayer == null || trader.interactingWithPlayer.Count == 0) {
                if (trader.WatchedAttributes.HasAttribute(TempMainAttribute)) {
                    var specialStockTree = trader.WatchedAttributes.GetOrAddTreeAttribute(SpecialStockAttribute);
                    trader.Inventory.ToTreeAttributes(specialStockTree);
                    var mainStock = trader.WatchedAttributes.GetTreeAttribute(TempMainAttribute);
                    trader.Inventory.FromTreeAttributes(mainStock);
                    trader.WatchedAttributes.RemoveAttribute(TempMainAttribute);
                    trader.WatchedAttributes.MarkAllDirty();
                }
            }
        }

        public static void ClearSpecialStockAttribute(EntityTradingHumanoid trader) {
            if (trader.WatchedAttributes.HasAttribute(SpecialStockAttribute)) {
                trader.WatchedAttributes.RemoveAttribute(SpecialStockAttribute);
                trader.WatchedAttributes.MarkAllDirty();
            }
        }
    }
}
