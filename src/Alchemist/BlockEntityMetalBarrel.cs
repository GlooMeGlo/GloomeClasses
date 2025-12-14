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
        private bool AlchemistBarrel; // true if an alchemist has opened this barrel
        private float heatedTemp = 20;
        protected string Type;
        private double lastCheckedTotalHours;

        public int CapacityLitres { get; set; } = 60;


        public override string InventoryClassName => "metalbarrel";

        public bool CanSeal {
            get {
                FindMatchingRecipe();
                if (CurrentRecipe != null && CurrentRecipe.SealHours > 0.0) {
                    // if it's an alchemy recipe, only allow sealing if this is an alchemist's barrel
                    if (CurrentAlcRecipe != null) {
                        return AlchemistBarrel;
                    }
                    // vanilla recipes can be sealed by anyone still!
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
                // check if barrel is now empty, reset alchemist mode if so
                if (inventory[0].Empty && inventory[1].Empty && AlchemistBarrel) {
                    AlchemistBarrel = false;
                    Api.Logger.Debug("[MetalBarrel] Barrel emptied - disabled alchemy mode at {0}", Pos);
                }

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

            Api.Logger.Debug("[MetalBarrel] FindMatchingRecipe called at {0} (AlchemistBarrel: {1}, SealedByTT: {2})",
                Pos, AlchemistBarrel, SealedByTT);

            var recipes = new List<BarrelRecipe>();
            var alcRecipes = new List<AlchemyBarrelRecipe>();
            recipes.Clear();
            recipes.AddRange(Api.GetBarrelRecipes());

            // load alchemy recipes if this is an alchemist's barrel
            if (AlchemistBarrel) {
                var glooRecipeLoader = Api.ModLoader.GetModSystem<GloomeClassesRecipeRegistry>();
                if (glooRecipeLoader != null) {
                    alcRecipes.AddRange(glooRecipeLoader.GetAlchemistBarrelRecipes(Type));
                    Api.Logger.Debug("[MetalBarrel] Loaded {0} alchemy recipes for {1} barrel at {2}",
                        alcRecipes.Count, Type, Pos);
                }
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

            // check alchemy recipes, but only allow them to be used if sealed by an alchemist
            if (CurrentRecipe == null && alcRecipes.Count > 0) {
                foreach (AlchemyBarrelRecipe recipe in alcRecipes) {
                    if (!recipe.Matches(array, out var outputStackSize)) {
                        continue;
                    }

                    ignoreChange = true;
                    if (recipe.SealHours > 0.0) {
                        // store the recipe match, but only set it if opened/sealed by alchemist
                        CurrentAlcRecipe = recipe;
                        CurrentRecipe = recipe;
                        CurrentOutSize = outputStackSize;
                        Api.Logger.Debug("[MetalBarrel] Matched alchemy recipe: {0} (temp req: {1}) at {2}",
                            recipe.Code, recipe.TempRequired, Pos);
                    } else {
                        // instant recipes can be used by everyone
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

            // vanilla barrel recipes work for everyone
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
            }
            // alchemy recipes that don't require heat: only work if this is an alchemist's barrel AND sealed
            else if (CurrentAlcRecipe != null && CurrentAlcRecipe.TempRequired < 0) {
                if (AlchemistBarrel && Sealed && CurrentAlcRecipe.TryCraftNow(Api, currentTotalHours - SealedSinceTotalHours, heatedTemp,
                [
                inventory[0],
                inventory[1]
                ])) {
                    MarkDirty(redrawOnClient: true);
                    Api.World.BlockAccessor.MarkBlockEntityDirty(Pos);
                    Sealed = false;
                    SealedByTT = false;
                }
            }
            // alchemy recipes that require heat: only work if this is an alchemist's barrel AND heated
            else if (CurrentAlcRecipe != null && CurrentAlcRecipe.TempRequired > 0) {
                if (!Heated && heatedTemp > 0) {
                    Heated = true;
                    SealedSinceTotalHours = currentTotalHours;
                    Api.Logger.Debug("[MetalBarrel] Heating started at {0} (temp: {1}, alchemist barrel: {2})",
                        Pos, heatedTemp, AlchemistBarrel);
                }
                // only process heated alchemy recipes if this is an alchemist's barrel
                if (Heated && AlchemistBarrel && CurrentAlcRecipe.TryCraftNow(Api, currentTotalHours - SealedSinceTotalHours, heatedTemp,
                [
                inventory[0],
                inventory[1]
                ])) {
                    Api.Logger.Debug("[MetalBarrel] Alchemy recipe {0} completed at {1}!", CurrentAlcRecipe.Code, Pos);
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

            // when gasifier starts heating, just update the temp
            // alchemy recipes are checked automatically in FindMatchingRecipe
            // and will only complete if SealedByTT is true
            Api.Logger.VerboseDebug("[MetalBarrel] Temperature updated to {0} at {1}", heatedTemp, Pos);
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

        public void SealBarrel(IPlayer player = null) {
            if (!Sealed) {
                // check if the sealing player is an alchemist
                if (player != null) {
                    string classcode = player.Entity.WatchedAttributes.GetString("characterClass");
                    var charclass = player.Entity.Api.ModLoader.GetModSystem<CharacterSystem>()
                        .characterClasses.FirstOrDefault(c => c.Code == classcode);
                    SealedByTT = charclass != null && charclass.Traits.Contains("temporaltransmutation");

                    Api.Logger.Debug("[MetalBarrel] Sealed by {0} (class: {1}, is alchemist: {2}) at {3}",
                        player.PlayerName, classcode ?? "none", SealedByTT, Pos);
                } else if (OpenedByTT) {
                    // fallback for programmatic sealing (if opened by TT before)
                    SealedByTT = true;
                }

                Sealed = true;
                SealedSinceTotalHours = Api.World.Calendar.TotalHours;
                MarkDirty(redrawOnClient: true);
            }
        }

        public void CheckAndSetAlchemistPlayer(IPlayer player) {
            // server-side check: mark barrel as alchemist mode if an alchemist interacts
            if (Api?.Side != EnumAppSide.Server || AlchemistBarrel) {
                return; // already marked or not on server
            }

            string classcode = player.Entity.WatchedAttributes.GetString("characterClass");
            var charclass = player.Entity.Api.ModLoader.GetModSystem<CharacterSystem>()
                .characterClasses.FirstOrDefault(c => c.Code == classcode);

            if (charclass != null && charclass.Traits.Contains("temporaltransmutation")) {
                AlchemistBarrel = true;
                MarkDirty(redrawOnClient: true);
                Api.Logger.Debug("[MetalBarrel] Alchemist {0} interacted - enabled alchemy mode at {1}",
                    player.PlayerName, Pos);
            }
        }

        public void TriggerRecipeCheck() {
            // public method to trigger recipe matching from external sources (like gasifier)
            FindMatchingRecipe();
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
            // check if player is an alchemist
            string classcode = byPlayer.Entity.WatchedAttributes.GetString("characterClass");
            CharacterClass charclass = byPlayer.Entity.Api.ModLoader.GetModSystem<CharacterSystem>()
                .characterClasses.FirstOrDefault(c => c.Code == classcode);
            bool isAlchemist = charclass != null && charclass.Traits.Contains("temporaltransmutation");

            Api.Logger.Debug("[MetalBarrel] GUI opened by {0} (class: {1}, is alchemist: {2}) at {3}",
                byPlayer.PlayerName, classcode ?? "none", isAlchemist, Pos);

            if (invDialog == null) {
                ICoreClientAPI capi = Api as ICoreClientAPI;
                invDialog = new GuiDialogMetalBarrel(Lang.Get("Barrel"), Inventory, Pos, Api as ICoreClientAPI);

                invDialog.OnClosed += delegate {
                    invDialog = null;
                    capi.Network.SendBlockEntityPacket(Pos, 1001);
                    capi.Network.SendPacketClient(Inventory.Close(byPlayer));
                };

                invDialog.OpenSound = AssetLocation.Create("game:sounds/block/barrelopen", base.Block.Code.Domain);
                invDialog.CloseSound = AssetLocation.Create("game:sounds/block/barrelclose", base.Block.Code.Domain);
                invDialog.TryOpen();
                capi.Network.SendPacketClient(Inventory.Open(byPlayer));
                capi.Network.SendBlockEntityPacket(Pos, 1000);

                // send alchemist status to server. this marks the barrel as "alchemist mode"
                if (isAlchemist) {
                    capi.Network.SendBlockEntityPacket(Pos, 1002);
                }
            } else {
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
                OpenedByTT = false;  // reset temporary flag when GUI closes
            }

            if (packetid == 1000) {
                player.InventoryManager?.OpenInventory(Inventory);
            }

            if (packetid == 1002) {
                // mark that an alchemist opened this barrel - this persists!
                OpenedByTT = true;
                AlchemistBarrel = true;
                MarkDirty(redrawOnClient: true);
                Api.Logger.Debug("[MetalBarrel] Alchemist {0} opened barrel - enabled alchemy mode at {1}",
                    player.PlayerName, Pos);
            }

            if (packetid == 1337) {
                // seal barrel, pass the player who sealed it
                SealBarrel(player);
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
            AlchemistBarrel = tree.GetBool("alchemistBarrel");

            Api?.Logger.Debug("[MetalBarrel] Loaded from save at {0}: AlchemistBarrel={1}, SealedByTT={2}, Sealed={3}, Heated={4}",
                Pos, AlchemistBarrel, SealedByTT, Sealed, tree.GetBool("heated"));

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
            tree.SetBool("alchemistBarrel", AlchemistBarrel);
            tree.SetDouble("sealedSinceTotalHours", SealedSinceTotalHours);
            tree.SetDouble("lastCheckedTotalHours", lastCheckedTotalHours);
            tree.SetFloat("heatedTemp", heatedTemp);
            tree.SetBool("heated", Heated);

            Api?.Logger.Debug("[MetalBarrel] Saved to disk at {0}: AlchemistBarrel={1}, SealedByTT={2}, Sealed={3}, Heated={4}",
                Pos, AlchemistBarrel, SealedByTT, Sealed, Heated);
        }

        internal MeshData GenMesh() {
            if (ownBlock == null) {
                return null;
            }

            MeshData mesh = ownBlock.GenMesh(inventory[0].Itemstack, inventory[1].Itemstack, Sealed, Pos);

            // apply vertex flags to enable liquid rendering effects
            if (mesh.CustomInts != null) {
                int[] CustomInts = mesh.CustomInts.Values;
                int count = mesh.CustomInts.Count;
                for (int i = 0; i < CustomInts.Length; i++) {
                    if (i >= count) break;
                    CustomInts[i] |= VertexFlags.LiquidWeakWaveBitMask   // enable weak water wavy
                                    | VertexFlags.LiquidWeakFoamBitMask;  // enable weak foam
                }
            }

            return mesh;
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
