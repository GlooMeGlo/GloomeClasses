using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GloomeClasses.src.Alchemist {

    public class BlockEntityMetalBarrel : BlockEntityLiquidContainer {
        private GuiDialogMetalBarrel invDialog;
        private MeshData currentMesh;
        private BlockMetalBarrel ownBlock;
        public bool Heated;
        public bool Sealed;
        private bool SealedByTT;
        public double SealedSinceTotalHours;
        public BarrelRecipe CurrentRecipe;
        public AlchemyBarrelRecipe CurrentAlcRecipe;
        public int CurrentOutSize;
        private bool ignoreChange;
        private bool OpenedByTT;
        private float heatedTemp = 20;
        protected string Type;
        private double lastCheckedTotalHours;

        public int CapacityLitres { get; set; } = 60;


        public override string InventoryClassName => "metalbarrel";

        public bool CanSeal {
            get {
                FindMatchingRecipe();
                if (CurrentRecipe != null && CurrentRecipe.SealHours > 0.0) {
                    return true;
                }

                return false;
            }
        }

        public BlockEntityMetalBarrel() {
            inventory = new InventoryGeneric(2, null, null, (id, self) => (id == 0) ? ((ItemSlot)new ItemSlotBarrelInput(self)) : ((ItemSlot)new ItemSlotLiquidOnly(self, 50f)))
            {
                BaseWeight = 1f,
                OnGetSuitability = GetSuitability
            };
            inventory.SlotModified += Inventory_SlotModified;
            inventory.OnAcquireTransitionSpeed += Inventory_OnAcquireTransitionSpeed1;
        }

        private float Inventory_OnAcquireTransitionSpeed1(EnumTransitionType transType, ItemStack stack, float mul) {
            if (Sealed && CurrentRecipe != null && CurrentRecipe.SealHours > 0.0) {
                return 0f;
            }

            return mul;
        }

        private float GetSuitability(ItemSlot sourceSlot, ItemSlot targetSlot, bool isMerge) {
            if (targetSlot == inventory[1] && inventory[0].StackSize > 0) {
                ItemStack itemstack = inventory[0].Itemstack;
                ItemStack itemstack2 = sourceSlot.Itemstack;
                if (itemstack.Collectible.Equals(itemstack, itemstack2, GlobalConstants.IgnoredStackAttributes)) {
                    return -1f;
                }
            }

            return (isMerge ? (inventory.BaseWeight + 3f) : (inventory.BaseWeight + 1f)) + (float)((sourceSlot.Inventory is InventoryBasePlayer) ? 1 : 0);
        }

        protected override ItemSlot GetAutoPushIntoSlot(BlockFacing atBlockFace, ItemSlot fromSlot) {
            if (atBlockFace == BlockFacing.UP) {
                return inventory[0];
            }

            return null;
        }

        public override void Initialize(ICoreAPI api) {
            base.Initialize(api);
            ownBlock = base.Block as BlockMetalBarrel;
            BlockMetalBarrel blockBarrel = ownBlock;
            Type = blockBarrel.Variant["metal"];
            if (blockBarrel != null && (blockBarrel.Attributes?["capacityLitres"].Exists).GetValueOrDefault()) {
                CapacityLitres = ownBlock.Attributes["capacityLitres"].AsInt(50);
                (inventory[1] as ItemSlotLiquidOnly).CapacityLitres = CapacityLitres;
            }

            if (api.Side == EnumAppSide.Client && currentMesh == null) {
                currentMesh = GenMesh();
                MarkDirty(redrawOnClient: true);
            }

            if (api.Side == EnumAppSide.Server) {
                RegisterGameTickListener(OnEvery3Second, 3000);
            }

            FindMatchingRecipe();
        }

        private void Inventory_SlotModified(int slotId) {
            if (!ignoreChange && (slotId == 0 || slotId == 1)) {
                invDialog?.UpdateContents();
                ICoreAPI api = Api;
                if (api != null && api.Side == EnumAppSide.Client) {
                    currentMesh = GenMesh();
                }

                MarkDirty(redrawOnClient: true);
                FindMatchingRecipe();
            }
        }

        private void FindMatchingRecipe() {
            ItemSlot[] array = [
                inventory[0],
                inventory[1]
            ];
            CurrentRecipe = null;
            CurrentAlcRecipe = null;

            Api.Logger.Debug("[MetalBarrel] FindMatchingRecipe called at {0} (OpenedByTT: {1}, SealedByTT: {2})",
                Pos, OpenedByTT, SealedByTT);

            var recipes = new List<BarrelRecipe>();
            var alcRecipes = new List<AlchemyBarrelRecipe>();
            recipes.Clear();
            recipes.AddRange(Api.GetBarrelRecipes());
            if (OpenedByTT || SealedByTT) {
                var glooRecipeLoader = Api.ModLoader.GetModSystem<GloomeClassesRecipeRegistry>();
                if (glooRecipeLoader != null) {
                    alcRecipes.AddRange(glooRecipeLoader.GetAlchemistBarrelRecipes(Type));
                    Api.Logger.Debug("[MetalBarrel] Loaded {0} alchemy recipes for {1} barrel at {2}",
                        alcRecipes.Count, Type, Pos);
                }
            } else {
                Api.Logger.Debug("[MetalBarrel] Alchemy recipes NOT loaded (no TT access) at {0}", Pos);
            }

            foreach (BarrelRecipe recipe in recipes) {
                if (!recipe.Matches(array, out var outputStackSize)) {
                    continue;
                }

                ignoreChange = true;
                if (recipe.SealHours > 0.0) {
                    CurrentRecipe = recipe;
                    CurrentOutSize = outputStackSize;
                    Api.Logger.Debug("[MetalBarrel] Matched vanilla barrel recipe: {0} at {1}", recipe.Code, Pos);
                } else {
                    ICoreAPI api = Api;
                    if (api != null && api.Side == EnumAppSide.Server) {
                        recipe.TryCraftNow(Api, 0.0, array);
                        MarkDirty(redrawOnClient: true);
                        Api.World.BlockAccessor.MarkBlockEntityDirty(Pos);
                    }
                }

                invDialog?.UpdateContents();
                ICoreAPI api2 = Api;
                if (api2 != null && api2.Side == EnumAppSide.Client) {
                    currentMesh = GenMesh();
                    MarkDirty(redrawOnClient: true);
                }

                ignoreChange = false;
                break;
            }

            if (CurrentRecipe == null && alcRecipes.Count > 0) {
                foreach (AlchemyBarrelRecipe recipe in alcRecipes) {
                    if (!recipe.Matches(array, out var outputStackSize)) {
                        continue;
                    }

                    ignoreChange = true;
                    if (recipe.SealHours > 0.0) {
                        CurrentAlcRecipe = recipe;
                        CurrentRecipe = recipe;
                        CurrentOutSize = outputStackSize;
                        Api.Logger.Debug("[MetalBarrel] Matched alchemy recipe: {0} (temp req: {1}) at {2}",
                            recipe.Code, recipe.TempRequired, Pos);
                    } else {
                        ICoreAPI api = Api;
                        if (api != null && api.Side == EnumAppSide.Server) {
                            recipe.TryCraftNow(Api, 0.0, array);
                            MarkDirty(redrawOnClient: true);
                            Api.World.BlockAccessor.MarkBlockEntityDirty(Pos);
                        }
                    }

                    invDialog?.UpdateContents();
                    ICoreAPI api2 = Api;
                    if (api2 != null && api2.Side == EnumAppSide.Client) {
                        currentMesh = GenMesh();
                        MarkDirty(redrawOnClient: true);
                    }

                    ignoreChange = false;
                    break;
                }
            }
        }

        private void OnEvery3Second(float dt) {
            double currentTotalHours = Api.World.Calendar.TotalHours;

            // initialize lastCheckedTotalHours if this is the first check
            if (lastCheckedTotalHours == 0) {
                lastCheckedTotalHours = currentTotalHours;
            }

            // update lastCheckedTotalHours for next tick
            lastCheckedTotalHours = currentTotalHours;

            if (!inventory[0].Empty && CurrentRecipe == null && CurrentAlcRecipe == null) {
                FindMatchingRecipe();
            }

            if (heatedTemp < 20) {
                Heated = false;
            }

            if (CurrentRecipe != null && CurrentAlcRecipe == null) {
                if (Sealed && CurrentRecipe.TryCraftNow(Api, currentTotalHours - SealedSinceTotalHours,
                [
                inventory[0],
                inventory[1]
                ])) {
                    MarkDirty(redrawOnClient: true);
                    Api.World.BlockAccessor.MarkBlockEntityDirty(Pos);
                    Sealed = false;
                    SealedByTT = false;
                }
            } else if (CurrentAlcRecipe != null && CurrentAlcRecipe.TempRequired < 0) {
                if (SealedByTT && CurrentAlcRecipe.TryCraftNow(Api, currentTotalHours - SealedSinceTotalHours, heatedTemp,
                [
                inventory[0],
                inventory[1]
                ])) {
                    MarkDirty(redrawOnClient: true);
                    Api.World.BlockAccessor.MarkBlockEntityDirty(Pos);
                    Sealed = false;
                    SealedByTT = false;
                }
            } else if (CurrentAlcRecipe != null && CurrentAlcRecipe.TempRequired > 0) {
                if (!Heated && heatedTemp > 0) {
                    Heated = true;
                    SealedSinceTotalHours = currentTotalHours;
                    Api.Logger.Debug("[MetalBarrel] Heating started for recipe {0} at {1} (temp: {2})",
                        CurrentAlcRecipe.Code, Pos, heatedTemp);
                }
                if (Heated && CurrentAlcRecipe.TryCraftNow(Api, currentTotalHours - SealedSinceTotalHours, heatedTemp,
                [
                inventory[0],
                inventory[1]
                ])) {
                    Api.Logger.Debug("[MetalBarrel] Recipe {0} completed at {1}!", CurrentAlcRecipe.Code, Pos);
                    MarkDirty(redrawOnClient: true);
                    Api.World.BlockAccessor.MarkBlockEntityDirty(Pos);
                    Sealed = false;
                    SealedByTT = false;
                    Heated = false;
                }
            } else if (Sealed) {
                Sealed = false;
                SealedByTT = false;
                MarkDirty(redrawOnClient: true);
            }
        }

        public void GassifierUpdateTemp(float temp) {
            heatedTemp = temp;
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null) {
            base.OnBlockPlaced(byItemStack);
            ItemSlot itemSlot = Inventory[0];
            ItemSlot itemSlot2 = Inventory[1];
            if (!itemSlot.Empty && itemSlot2.Empty && BlockLiquidContainerBase.GetContainableProps(itemSlot.Itemstack) != null) {
                Inventory.TryFlipItems(1, itemSlot);
            }
        }

        public override void OnBlockBroken(IPlayer byPlayer = null) {
            if (!Sealed) {
                base.OnBlockBroken(byPlayer);
            }

            invDialog?.TryClose();
            invDialog = null;
        }

        public void SealBarrel() {
            if (!Sealed) {
                if (OpenedByTT) {
                    SealedByTT = true;
                }
                Sealed = true;
                SealedSinceTotalHours = Api.World.Calendar.TotalHours;
                MarkDirty(redrawOnClient: true);
            }
        }

        public void OnPlayerRightClick(IPlayer byPlayer) {
            if (!Sealed) {
                if (Api.Side == EnumAppSide.Client) {
                    ToggleInventoryDialogClient(byPlayer);
                }
                FindMatchingRecipe();
            }
        }

        protected void ToggleInventoryDialogClient(IPlayer byPlayer) {
            string classcode = byPlayer.Entity.WatchedAttributes.GetString("characterClass");
            CharacterClass charclass = byPlayer.Entity.Api.ModLoader.GetModSystem<CharacterSystem>().characterClasses.FirstOrDefault(c => c.Code == classcode);
            OpenedByTT = charclass != null && charclass.Traits.Contains("temporaltransmutation");

            Api.Logger.Debug("[MetalBarrel] GUI opened by {0} (class: {1}, has TT trait: {2}) at {3}",
                byPlayer.PlayerName, classcode ?? "none", OpenedByTT, Pos);

            if (invDialog == null) {
                ICoreClientAPI capi = Api as ICoreClientAPI;
                invDialog = new GuiDialogMetalBarrel(Lang.Get("Barrel"), Inventory, Pos, Api as ICoreClientAPI);
                invDialog.OnClosed += delegate {
                    invDialog = null;
                    capi.Network.SendBlockEntityPacket(Pos, 1001);
                    // don't send 1003 (TT=false) when GUI closes!!! preserve OpenedByTT state
                    capi.Network.SendPacketClient(Inventory.Close(byPlayer));
                };
                invDialog.OpenSound = AssetLocation.Create("game:sounds/block/barrelopen", base.Block.Code.Domain);
                invDialog.CloseSound = AssetLocation.Create("game:sounds/block/barrelclose", base.Block.Code.Domain);
                invDialog.TryOpen();
                capi.Network.SendPacketClient(Inventory.Open(byPlayer));
                capi.Network.SendBlockEntityPacket(Pos, 1000);
                if (OpenedByTT) {
                    capi.Network.SendBlockEntityPacket(Pos, 1002);
                } else {
                    capi.Network.SendBlockEntityPacket(Pos, 1003);
                }
            } else {
                ICoreClientAPI capi = Api as ICoreClientAPI;
                capi.Network.SendBlockEntityPacket(Pos, 1003);
                OpenedByTT = false;
                invDialog.TryClose();
            }
        }

        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data) {
            base.OnReceivedClientPacket(player, packetid, data);
            if (packetid < 1000) {
                Inventory.InvNetworkUtil.HandleClientPacket(player, packetid, data);
                Api.World.BlockAccessor.GetChunkAtBlockPos(Pos).MarkModified();
                return;
            }

            if (packetid == 1001) {
                player.InventoryManager?.CloseInventory(Inventory);
            }

            if (packetid == 1000) {
                player.InventoryManager?.OpenInventory(Inventory);
            }

            if (packetid == 1002) {
                OpenedByTT = true;
                Api.Logger.Debug("[MetalBarrel] Server received TT=true packet from {0} at {1}", player.PlayerName, Pos);
            }

            if (packetid == 1003) {
                OpenedByTT = false;
                Api.Logger.Debug("[MetalBarrel] Server received TT=false packet from {0} at {1}", player.PlayerName, Pos);
            }

            if (packetid == 1337) {
                SealBarrel();
            }
        }

        public override void OnReceivedServerPacket(int packetid, byte[] data) {
            base.OnReceivedServerPacket(packetid, data);
            if (packetid == 1001) {
                (Api.World as IClientWorldAccessor).Player.InventoryManager.CloseInventory(Inventory);
                OpenedByTT = false;
                invDialog?.TryClose();
                invDialog?.Dispose();
                invDialog = null;
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving) {
            base.FromTreeAttributes(tree, worldForResolving);
            Sealed = tree.GetBool("sealed");
            SealedByTT = tree.GetBool("sealedByTT");
            OpenedByTT = tree.GetBool("openedByTT");

            Api?.Logger.Debug("[MetalBarrel] Loaded from save at {0}: OpenedByTT={1}, SealedByTT={2}, Sealed={3}, Heated={4}",
                Pos, OpenedByTT, SealedByTT, Sealed, tree.GetBool("heated"));

            ICoreAPI api = Api;
            if (api != null && api.Side == EnumAppSide.Client) {
                currentMesh = GenMesh();
                MarkDirty(redrawOnClient: true);
                invDialog?.UpdateContents();
            }

            SealedSinceTotalHours = tree.GetDouble("sealedSinceTotalHours");
            lastCheckedTotalHours = tree.GetDouble("lastCheckedTotalHours");
            heatedTemp = tree.GetFloat("heatedTemp", 20);
            Heated = tree.GetBool("heated");
            if (Api != null) {
                FindMatchingRecipe();
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree) {
            base.ToTreeAttributes(tree);
            tree.SetBool("sealed", Sealed);
            tree.SetBool("sealedByTT", SealedByTT);
            tree.SetBool("openedByTT", OpenedByTT);
            tree.SetDouble("sealedSinceTotalHours", SealedSinceTotalHours);
            tree.SetDouble("lastCheckedTotalHours", lastCheckedTotalHours);
            tree.SetFloat("heatedTemp", heatedTemp);
            tree.SetBool("heated", Heated);

            Api?.Logger.Debug("[MetalBarrel] Saved to disk at {0}: OpenedByTT={1}, SealedByTT={2}, Sealed={3}, Heated={4}",
                Pos, OpenedByTT, SealedByTT, Sealed, Heated);
        }

        internal MeshData GenMesh() {
            if (ownBlock == null) {
                return null;
            }

            MeshData meshData = ownBlock.GenMesh(inventory[0].Itemstack, inventory[1].Itemstack, Sealed, Pos);
            if (meshData.CustomInts != null) {
                for (int i = 0; i < meshData.CustomInts.Count; i++) {
                    meshData.CustomInts.Values[i] |= 134217728;
                    meshData.CustomInts.Values[i] |= 67108864;
                }
            }

            return meshData;
        }

        public override void OnBlockUnloaded() {
            base.OnBlockUnloaded();
            invDialog?.Dispose();
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator) {
            mesher.AddMeshData(currentMesh);
            return true;
        }
    }
}
