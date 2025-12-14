using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace GloomeClasses.src.Alchemist {

    public class BlockGassifier : Block, IIgnitable, IHeatSource {

        WorldInteraction[] openDoorInteraction;
        WorldInteraction[] closeDoorInteraction;
        WorldInteraction[] addFuelInteractions;
        WorldInteraction[] igniteInteractions;

        public override void OnLoaded(ICoreAPI api) {
            base.OnLoaded(api);

            if (api.Side.IsServer()) {
                return;
            }

            ICoreClientAPI capi = api as ICoreClientAPI;
            List<ItemStack> canIgniteStacks = BlockBehaviorCanIgnite.CanIgniteStacks(api, true);

            openDoorInteraction = ObjectCacheUtil.GetOrCreate(capi, "openGassifierInteraction", () => {
                return new WorldInteraction[] {
                    new() {
                        ActionLangCode = "blockhelp-gassifier-opendoor",
                        MouseButton = EnumMouseButton.Right
                    }
                };
            });

            closeDoorInteraction = ObjectCacheUtil.GetOrCreate(capi, "closeGassifierInteraction", () => {
                return new WorldInteraction[] {
                    new() {
                        ActionLangCode = "blockhelp-gassifier-closedoor",
                        MouseButton = EnumMouseButton.Right
                    }
                };
            });

            addFuelInteractions = ObjectCacheUtil.GetOrCreate(capi, "addFuelToGassifierInteraction", () => {
                return new WorldInteraction[] {
                    new() {
                        ActionLangCode = "blockhelp-gassifier-refuel",
                        MouseButton = EnumMouseButton.Right
                    }
                };
            });

            igniteInteractions = ObjectCacheUtil.GetOrCreate(capi, "igniteGassifierInteraction", () => {
                return new WorldInteraction[] {
                    new() {
                        ActionLangCode = "blockhelp-gassifier-ignite",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = [.. canIgniteStacks],
                        GetMatchingStacks = (wi, bs, es) => {
                            BlockEntityGassifier beg = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityGassifier;
                            if (beg?.Inventory.FuelSlot != null && !beg.Inventory.FuelSlot.Empty && !beg.IsBurning) {
                                return wi.Itemstacks;
                            }
                            return null;
                        }
                    }
                };
            });
        }

        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer) {
            string baseInfo = base.GetPlacedBlockInfo(world, pos, forPlayer);

            if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityGassifier beg) {
                // show fuel amount
                string fuelInfo;
                if (!beg.Inventory.FuelSlot.Empty) {
                    ItemStack fuelStack = beg.Inventory.FuelSlot.Itemstack;
                    fuelInfo = Lang.Get("Fuel: {0}x {1}", fuelStack.StackSize, fuelStack.GetName());
                } else {
                    fuelInfo = Lang.Get("Fuel: 0");
                }

                // show burn completion (if burning)
                if (beg.IsBurning && beg.maxFuelBurnTime > 0) {
                    float burnProgress = (1f - (beg.fuelBurnTime / beg.maxFuelBurnTime)) * 100f;
                    fuelInfo += "\n" + Lang.Get("Burn completion: {0}%", burnProgress.ToString("0.0"));
                } else {
                    fuelInfo += "\n" + Lang.Get("Burn completion: 0%");
                }

                // always show temperature
                fuelInfo += "\n" + Lang.Get("Temperature: {0}°C", (int)beg.furnaceTemperature);

                // show helpful message if hot and ready to re-ignite but door is open
                if (!beg.IsBurning && beg.canIgniteFuel && !beg.Inventory.FuelSlot.Empty && beg.IsOpen()) {
                    fuelInfo += "\n" + Lang.Get("Close door to ignite");
                }

                return string.IsNullOrEmpty(baseInfo) ? fuelInfo : baseInfo + "\n" + fuelInfo;
            }

            return baseInfo;
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer) {
            List<WorldInteraction> interactions = [];

            Block block = world.BlockAccessor.GetBlock(selection.Position);
            var openOrClosed = block.Variant["type"];

            if (openOrClosed == "open") {
                interactions.AddRange(closeDoorInteraction);
                BlockEntityGassifier beg = world.BlockAccessor.GetBlockEntity(selection.Position) as BlockEntityGassifier;
                if (beg != null && (beg.Inventory.FuelSlot.Empty || beg.Inventory.FuelSlot.StackSize < beg.Inventory.FuelSlot.MaxSlotStackSize)) {
                    interactions.AddRange(addFuelInteractions);
                }
                if (beg != null && !beg.Inventory.FuelSlot.Empty) {
                    interactions.AddRange(igniteInteractions);
                }
            } else if (openOrClosed == "closed") {
                interactions.AddRange(openDoorInteraction);
            }

            return [.. interactions];
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
            if (blockSel != null && !world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use)) {
                return false;
            }

            ItemStack stack = byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack;
            BlockEntityGassifier beg = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityGassifier;
            if (beg == null) return base.OnBlockInteractStart(world, byPlayer, blockSel);

            // handle torch/ignition first (with ignitable item)
            if (stack?.Block != null && stack.Block.HasBehavior<BlockBehaviorCanIgnite>() && beg.GetIgnitableState(0) == EnumIgniteState.Ignitable) {
                return false;
            }

            // handle fuel addition (right-click with fuel while door is open) - like bloomery
            if (beg.IsOpen() && stack != null && stack.Collectible.CombustibleProps != null) {
                var success = beg.Inventory.AddFuel(byPlayer.InventoryManager.ActiveHotbarSlot);

                if (success && world.Side.IsServer()) {
                    var loc = stack.ItemAttributes?["placeSound"].Exists == true ? AssetLocation.Create(stack.ItemAttributes["placeSound"].AsString(), stack.Collectible.Code.Domain) : null;
                    if (loc != null) {
                        api.World.PlaySoundAt(loc.WithPathPrefixOnce("sounds/"), blockSel.Position.X, blockSel.Position.InternalY, blockSel.Position.Z, null, 0.88f + (float)api.World.Rand.NextDouble() * 0.24f, 16);
                    }
                    (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                }

                // always return true to prevent item placement, even if fuel slot is full
                return true;
            }

            // handle door toggle
            if (!beg.IsBurning) {
                if (world.Side.IsServer()) {
                    byPlayer.Entity.World.PlaySoundAt(new AssetLocation("game:sounds/block/metaldoor.ogg"), byPlayer.Entity.Pos.X, byPlayer.Entity.Pos.Y, byPlayer.Entity.Pos.Z, null, true, 32f, 1f);
                }
                ToggleDoor(world, blockSel);
                return true;
            }

            // always return true to prevent default item placement behavior (like bloomery does)
            return true;
        }

        private void ToggleDoor(IWorldAccessor world, BlockSelection blockSel) {
            Block block = world.BlockAccessor.GetBlock(blockSel.Position);
            string openCloseState = block?.Variant["type"];
            string sideState = block?.Variant["side"];

            if (openCloseState != null && sideState != null) {
                if (openCloseState == "open") {
                    Block closed = world.GetBlock(new AssetLocation("gloomeclasses:gassifier-closed-" + sideState));
                    world.BlockAccessor.ExchangeBlock(closed.Id, blockSel.Position);
                } else {
                    Block open = world.GetBlock(new AssetLocation("gloomeclasses:gassifier-open-" + sideState));
                    world.BlockAccessor.ExchangeBlock(open.Id, blockSel.Position);
                }
            }
        }

        public float GetHeatStrength(IWorldAccessor world, BlockPos heatSourcePos, BlockPos heatReceiverPos) {
            if (api.World.BlockAccessor.GetBlockEntity(heatSourcePos) is not BlockEntityGassifier beg)
            {
                return 0f;
            }
            return beg.IsBurning ? 10f : (beg.IsSmoldering ? 0.25f : 0f);
        }

        public EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting) {
            if (api.World.BlockAccessor.GetBlockEntity(pos) is not BlockEntityGassifier beg)
            {
                return EnumIgniteState.NotIgnitable;
            }
            return beg.GetIgnitableState(secondsIgniting);
        }

        public void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling) {
            if (api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityGassifier beg && !beg.canIgniteFuel)
            {
                beg.canIgniteFuel = true;

                // automatically close the door after torch ignition to start burning
                if (beg.IsOpen() && api.Side == EnumAppSide.Server)
                {
                    Block block = api.World.BlockAccessor.GetBlock(pos);
                    string sideState = block?.Variant["side"];
                    if (sideState != null)
                    {
                        Block closed = api.World.GetBlock(new AssetLocation("gloomeclasses:gassifier-closed-" + sideState));
                        if (closed != null)
                        {
                            api.World.BlockAccessor.ExchangeBlock(closed.Id, pos);
                            api.World.PlaySoundAt(new AssetLocation("game:sounds/block/metaldoor.ogg"), pos.X, pos.Y, pos.Z, null, true, 32f, 1f);
                        }
                    }
                }
            }
            handling = EnumHandling.PreventDefault;
        }

        public EnumIgniteState OnTryIgniteStack(EntityAgent byEntity, BlockPos pos, ItemSlot slot, float secondsIgniting) {
            if (api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityGassifier beg && beg.IsOpen() && beg.IsBurning)
            {
                return secondsIgniting > 2 ? EnumIgniteState.IgniteNow : EnumIgniteState.Ignitable;
            }
            return EnumIgniteState.NotIgnitable;
        }
    }
}
