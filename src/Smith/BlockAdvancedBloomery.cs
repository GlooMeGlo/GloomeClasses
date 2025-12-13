using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace GloomeClasses.src.Smith {

    //This is basically just the vanilla Bloomery copied over and edited to be _slightly different!_
    public class BlockAdvancedBloomery : Block, IIgnitable {
        private WorldInteraction[] interactions;

        public override void OnLoaded(ICoreAPI api) {
            base.OnLoaded(api);
            if (api.Side != EnumAppSide.Client) {
                return;
            }

            ICoreClientAPI capi = api as ICoreClientAPI;
            interactions = ObjectCacheUtil.GetOrCreate(api, "advBloomeryBlockInteractions", delegate {
                List<ItemStack> smeltables = [];
                List<ItemStack> fuels = [];
                List<ItemStack> ingiters = BlockBehaviorCanIgnite.CanIgniteStacks(api, withFirestarter: false);
                foreach (CollectibleObject collectible in api.World.Collectibles) {
                    if (collectible.CombustibleProps != null) {
                        if (collectible.CombustibleProps.SmeltedStack != null && collectible.CombustibleProps.MeltingPoint < 1600) {
                            List<ItemStack> handBookStacks = collectible.GetHandBookStacks(capi);
                            if (handBookStacks != null) {
                                smeltables.AddRange(handBookStacks);
                            }
                        } else if (collectible.CombustibleProps.BurnTemperature >= 1200 && collectible.CombustibleProps.BurnDuration > 30f) {
                            List<ItemStack> handBookStacks2 = collectible.GetHandBookStacks(capi);
                            if (handBookStacks2 != null) {
                                fuels.AddRange(handBookStacks2);
                            }
                        } else if (collectible.Code.Path == "stainless-steel-mix") {
                            List<ItemStack> handBookStacks = collectible.GetHandBookStacks(capi);
                            if (handBookStacks != null) {
                                smeltables.AddRange(handBookStacks);
                            }
                        }
                    }
                }

                return new WorldInteraction[4]
                {
                new() {
                    ActionLangCode = "blockhelp-bloomery-heatable",
                    HotKeyCode = null,
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = [.. smeltables],
                    GetMatchingStacks = GetMatchingStacks
                },
                new() {
                    ActionLangCode = "blockhelp-bloomery-heatablex4",
                    HotKeyCode = "ctrl",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = [.. smeltables],
                    GetMatchingStacks = GetMatchingStacks
                },
                new() {
                    ActionLangCode = "blockhelp-bloomery-fuel",
                    HotKeyCode = null,
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = [.. fuels],
                    GetMatchingStacks = GetMatchingStacks
                },
                new() {
                    ActionLangCode = "blockhelp-bloomery-ignite",
                    HotKeyCode = "shift",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = [.. ingiters],
                    GetMatchingStacks = (wi, bs, es) => (api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityAdvancedBloomery blockEntityAdvBloomery && blockEntityAdvBloomery.CanIgnite() && !blockEntityAdvBloomery.IsBurning && api.World.BlockAccessor.GetBlock(bs.Position.UpCopy()).Code.Path.Contains("bloomerychimneyadvanced")) ? wi.Itemstacks : null
                }
                };
            });
        }

        private ItemStack[] GetMatchingStacks(WorldInteraction wi, BlockSelection blockSelection, EntitySelection entitySelection) {
            if (api.World.BlockAccessor.GetBlockEntity(blockSelection.Position) is not BlockEntityAdvancedBloomery blockEntityAdvBloomery || wi.Itemstacks.Length == 0) {
                return null;
            }

            List<ItemStack> list = [];
            ItemStack[] itemstacks = wi.Itemstacks;
            foreach (ItemStack itemStack in itemstacks) {
                if (blockEntityAdvBloomery.CanAdd(itemStack)) {
                    list.Add(itemStack);
                }
            }

            return [.. list];
        }

        EnumIgniteState IIgnitable.OnTryIgniteStack(EntityAgent byEntity, BlockPos pos, ItemSlot slot, float secondsIgniting) {
            return EnumIgniteState.NotIgnitable;
        }

        public EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting) {
            if (!(byEntity.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityAdvancedBloomery).CanIgnite()) {
                return EnumIgniteState.NotIgnitablePreventDefault;
            }

            if (!(secondsIgniting > 4f)) {
                return EnumIgniteState.Ignitable;
            }

            return EnumIgniteState.IgniteNow;
        }

        public void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling) {
            handling = EnumHandling.PreventDefault;
            (byEntity.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityAdvancedBloomery)?.TryIgnite();
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
            ItemStack itemstack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
            if (itemstack != null && itemstack.Class == EnumItemClass.Block && itemstack.Collectible.Code.PathStartsWith("bloomerychimneyadvanced")) {
                if (world.BlockAccessor.GetBlock(blockSel.Position.UpCopy()).IsReplacableBy(itemstack.Block)) {
                    itemstack.Block.DoPlaceBlock(world, byPlayer, new BlockSelection {
                        Position = blockSel.Position.UpCopy(),
                        Face = BlockFacing.UP
                    }, itemstack);
                    world.PlaySoundAt(Sounds?.Place, blockSel.Position, 0.5, byPlayer, randomizePitch: true, 16f);
                    if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative) {
                        byPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(1);
                    }
                }

                return true;
            }

            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityAdvancedBloomery blockEntityAdvBloomery) {
                if (itemstack == null) {
                    return true;
                }

                if (blockEntityAdvBloomery.TryAdd(byPlayer, (!byPlayer.Entity.Controls.CtrlKey) ? 1 : 5) && world.Side == EnumAppSide.Client) {
                    (byPlayer as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                }
            }

            return true;
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f) {
            Block block = world.BlockAccessor.GetBlock(pos.UpCopy());
            if (block.Code.Path == "bloomerychimneyadvanced") {
                block.OnBlockBroken(world, pos.UpCopy(), byPlayer, dropQuantityMultiplier);
            }

            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
        }

        public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer) {
            return GetHandbookDropsFromBreakDrops(handbookStack, forPlayer);
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f) {
            List<ItemStack> list = [];
            for (int i = 0; i < Drops.Length; i++) {
                if (Drops[i].Tool.HasValue && (byPlayer == null || Drops[i].Tool != byPlayer.InventoryManager.ActiveTool)) {
                    continue;
                }

                ItemStack nextItemStack = Drops[i].GetNextItemStack(dropQuantityMultiplier);
                if (nextItemStack != null) {
                    list.Add(nextItemStack);
                    if (Drops[i].LastDrop) {
                        break;
                    }
                }
            }

            return [.. list];
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer) {
            return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }
    }
}
