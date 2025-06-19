using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace GloomeClasses.src.Smith {
    public class BlockEntityAdvancedBloomery : BlockEntity, IHeatSource {
        private ILoadedSound ambientSound;
        private BloomeryContentsRenderer renderer;
        private static SimpleParticleProperties breakSparks;
        private static SimpleParticleProperties smallMetalSparks;
        private static SimpleParticleProperties smoke;
        private BlockFacing ownFacing;
        internal InventoryGeneric bloomeryInv;
        private bool burning;
        private double burningUntilTotalDays;
        private double burningStartTotalDays;
        private const int FuelCapacity = 4;

        public AssetLocation FuelSoundLocation => new AssetLocation("sounds/block/charcoal");
        public AssetLocation OreSoundLocation => new AssetLocation("sounds/block/loosestone");

        public bool IsBurning => burning;
        private ItemSlot FuelSlot => bloomeryInv[0];
        private ItemSlot OreSlot => bloomeryInv[1];
        private ItemSlot OutSlot => bloomeryInv[2];
        private ItemStack FuelStack => FuelSlot.Itemstack;
        private ItemStack OreStack => OreSlot.Itemstack;
        private ItemStack OutStack => OutSlot.Itemstack;

        private int OreCapacity => Ore2FuelRatio * 8;

        private int Ore2FuelRatio {
            get {
                int valueOrDefault = (OreStack?.Collectible.CombustibleProps?.SmeltedRatio).GetValueOrDefault(1);
                return (OreStack?.ItemAttributes?["bloomeryFuelRatio"].AsInt(valueOrDefault)).GetValueOrDefault(valueOrDefault);
            }
        }

        static BlockEntityAdvancedBloomery() {
            smallMetalSparks = new SimpleParticleProperties(2f, 5f, ColorUtil.ToRgba(255, 255, 233, 83), new Vec3d(), new Vec3d(), new Vec3f(-3f, 8f, -3f), new Vec3f(3f, 12f, 3f), 0.1f, 1f, 0.25f, 0.25f, EnumParticleModel.Quad);
            smallMetalSparks.WithTerrainCollision = false;
            smallMetalSparks.VertexFlags = 128;
            smallMetalSparks.AddPos.Set(0.0625, 0.0, 0.0625);
            smallMetalSparks.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.QUADRATIC, -0.5f);
            smallMetalSparks.AddPos.Set(0.25, 0.1875, 0.25);
            smallMetalSparks.ParticleModel = EnumParticleModel.Cube;
            smallMetalSparks.LifeLength = 0.04f;
            smallMetalSparks.MinQuantity = 1f;
            smallMetalSparks.AddQuantity = 1f;
            smallMetalSparks.MinSize = 0.2f;
            smallMetalSparks.MaxSize = 0.2f;
            smallMetalSparks.GravityEffect = 0f;
            breakSparks = new SimpleParticleProperties(40f, 80f, ColorUtil.ToRgba(255, 255, 233, 83), new Vec3d(), new Vec3d(), new Vec3f(-1f, 0.5f, -1f), new Vec3f(2f, 1.5f, 2f), 0.5f, 1f, 0.25f, 0.25f);
            breakSparks.VertexFlags = 128;
            breakSparks.AddPos.Set(0.25, 0.25, 0.25);
            breakSparks.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.25f);
            smoke = new SimpleParticleProperties(1f, 1f, ColorUtil.ToRgba(128, 110, 110, 110), new Vec3d(), new Vec3d(), new Vec3f(-0.2f, 0.3f, -0.2f), new Vec3f(0.2f, 0.3f, 0.2f), 2f, 0f, 0.5f, 1f, EnumParticleModel.Quad);
            smoke.SelfPropelled = true;
            smoke.OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -255f);
            smoke.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, 2f);
        }

        public BlockEntityAdvancedBloomery() {
            bloomeryInv = new InventoryGeneric(3, "advbloomery-1", null);
        }

        public override void Initialize(ICoreAPI api) {
            base.Initialize(api);
            bloomeryInv.LateInitialize("advbloomery-1", api);
            RegisterGameTickListener(OnGameTick, 100);
            updateSoundState();
            if (api.Side == EnumAppSide.Client) {
                ICoreClientAPI coreClientAPI = (ICoreClientAPI)api;
                coreClientAPI.Event.RegisterRenderer(renderer = new BloomeryContentsRenderer(Pos, coreClientAPI), EnumRenderStage.Opaque, "advbloomery");
                UpdateRenderer();
            }

            ownFacing = BlockFacing.FromCode(api.World.BlockAccessor.GetBlock(Pos).LastCodePart());
        }

        private void UpdateRenderer() {
            float fillLevel = Math.Min(14f, (float)FuelSlot.StackSize + 8f * (float)OreSlot.StackSize / (float)OreCapacity + (float)OutSlot.StackSize);
            renderer.SetFillLevel(fillLevel);
            double val = Math.Min(1.0, 24.0 * (Api.World.Calendar.TotalDays - burningStartTotalDays));
            double val2 = Math.Min(1.0, 24.0 * (burningUntilTotalDays - Api.World.Calendar.TotalDays));
            double num = Math.Max(0.0, Math.Min(val, val2) * 250.0);
            renderer.glowLevel = (burning ? ((int)num) : 0);
        }

        public void updateSoundState() {
            if (burning) {
                startSound();
            } else {
                stopSound();
            }
        }

        public void startSound() {
            if (ambientSound == null) {
                ICoreAPI api = Api;
                if (api != null && api.Side == EnumAppSide.Client) {
                    ambientSound = (Api as ICoreClientAPI).World.LoadSound(new SoundParams {
                        Location = new AssetLocation("sounds/environment/fire.ogg"),
                        ShouldLoop = true,
                        Position = Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                        DisposeOnFinish = false,
                        Volume = 0.3f,
                        Range = 8f
                    });
                    ambientSound.Start();
                }
            }
        }

        public void stopSound() {
            if (ambientSound != null) {
                ambientSound.Stop();
                ambientSound.Dispose();
                ambientSound = null;
            }
        }

        private void OnGameTick(float dt) {
            if (Api.Side == EnumAppSide.Client) {
                UpdateRenderer();
                if (burning) {
                    EmitParticles();
                }
            }

            if (burning && Api.Side == EnumAppSide.Server && burningUntilTotalDays < Api.World.Calendar.TotalDays) {
                DoSmelt();
            }
        }

        private void DoSmelt() {
            CombustibleProperties combustibleProps = OreStack.Collectible.CombustibleProps;
            if (combustibleProps != null) {
                int num = OreStack.StackSize / combustibleProps.SmeltedRatio;
                JsonObject itemAttributes = OreStack.ItemAttributes;
                if (itemAttributes != null && itemAttributes.IsTrue("mergeUnitsInBloomery")) {
                    OutSlot.Itemstack = combustibleProps.SmeltedStack.ResolvedItemstack.Clone();
                    OutStack.StackSize = 1;
                    float num2 = (float)OreStack.StackSize / (float)combustibleProps.SmeltedRatio;
                    OutStack.Attributes.SetFloat("units", num2 * 100f);
                } else if (OreStack.Collectible.Code.Path == "stainless-steel-mix") {
                    OutSlot.Itemstack = new ItemStack(Api.World.GetItem(new AssetLocation("ingot-stainlesssteel")));
                    OutStack.StackSize *= num;
                } else {
                    OutSlot.Itemstack = combustibleProps.SmeltedStack.ResolvedItemstack.Clone();
                    OutStack.StackSize *= num;
                }

                OutStack.Collectible.SetTemperature(Api.World, OutSlot.Itemstack, 900f);
                FuelSlot.Itemstack = null;
                OreStack.StackSize -= num * combustibleProps.SmeltedRatio;
                if (OreSlot.StackSize == 0) {
                    OreSlot.Itemstack = null;
                }

                burning = false;
                burningUntilTotalDays = 0.0;
                MarkDirty();
            }
        }

        private void EmitParticles() {
            if (Api.World.Rand.Next(5) > 0) {
                smoke.MinPos.Set((double)Pos.X + 0.5 - 0.125, (float)(Pos.Y + 1) + 0.625f, (double)Pos.Z + 0.5 - 0.125);
                smoke.AddPos.Set(0.25, 0.0, 0.25);
                Api.World.SpawnParticles(smoke);
            }

            if (renderer.glowLevel > 80 && Api.World.Rand.Next(3) == 0) {
                Vec3f normalf = ownFacing.Normalf;
                Vec3d minPos = smallMetalSparks.MinPos;
                minPos.Set((double)Pos.X + 0.5, Pos.Y, (double)Pos.Z + 0.5);
                minPos.Sub((double)normalf.X * 0.375 + 0.125, 0.0, (double)normalf.Z * 0.375 + 0.125);
                smallMetalSparks.MinPos = minPos;
                smallMetalSparks.VertexFlags = (byte)renderer.glowLevel;
                smallMetalSparks.MinVelocity = new Vec3f(-0.5f - normalf.X, -0.3f, -0.5f - normalf.Z);
                smallMetalSparks.AddVelocity = new Vec3f(1f - normalf.X, 0.6f, 1f - normalf.Z);
                Api.World.SpawnParticles(smallMetalSparks);
            }
        }

        public bool CanAdd(ItemStack stack, int quantity = 1) {
            if (IsBurning) {
                return false;
            }

            if (OutSlot.StackSize > 0) {
                return false;
            }

            CombustibleProperties combustibleProperties = stack?.Collectible.CombustibleProps;
            if (combustibleProperties == null) {
                return false;
            }

            if ( stack.Collectible.Code.Path == "stainless-steel-mix" || combustibleProperties.SmeltedStack != null && combustibleProperties.MeltingPoint < 1600 && combustibleProperties.MeltingPoint >= 1000) {
                if (OreSlot.StackSize + quantity > OreCapacity) {
                    return false;
                }

                if (!OreSlot.Empty && !OreStack.Equals(Api.World, stack, GlobalConstants.IgnoredStackAttributes)) {
                    return false;
                }

                return true;
            }

            if (combustibleProperties.BurnTemperature >= 1200 && combustibleProperties.BurnDuration > 30f) {
                if (FuelSlot.StackSize + quantity > FuelCapacity) {
                    return false;
                }

                if (!FuelSlot.Empty && !FuelStack.Equals(Api.World, stack, GlobalConstants.IgnoredStackAttributes)) {
                    return false;
                }

                return true;
            }

            return false;
        }

        public bool TryAdd(IPlayer byPlayer, int quantity = 1) {
            ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (IsBurning) {
                return false;
            }

            if (OutSlot.StackSize > 0) {
                return false;
            }

            if (activeHotbarSlot.Itemstack == null) {
                return false;
            }

            CombustibleProperties combustibleProps = activeHotbarSlot.Itemstack.Collectible.CombustibleProps;
            if ( activeHotbarSlot.Itemstack.Collectible.Code.Path != "stainless-steel-mix" && combustibleProps == null) {
                return true;
            }

            if ( activeHotbarSlot.Itemstack.Collectible.Code.Path == "stainless-steel-mix" || combustibleProps.SmeltedStack != null && combustibleProps.MeltingPoint < 1600 && combustibleProps.MeltingPoint >= 1000) {
                if (activeHotbarSlot.TryPutInto(Api.World, OreSlot, Math.Min(OreCapacity - OreSlot.StackSize, quantity)) > 0) {
                    MarkDirty();
                    Api.World.PlaySoundAt(OreSoundLocation, Pos, 0.0, byPlayer);
                    return true;
                }

                return false;
            }

            if (combustibleProps.BurnTemperature >= 1200 && combustibleProps.BurnDuration > 30f) {
                int num = (int)Math.Ceiling((float)OreSlot.StackSize / (float)Ore2FuelRatio);
                if (activeHotbarSlot.TryPutInto(Api.World, FuelSlot, Math.Min(num - FuelSlot.StackSize, quantity)) > 0) {
                    MarkDirty();
                    Api.World.PlaySoundAt(FuelSoundLocation, Pos, 0.0, byPlayer);
                    return true;
                }

                return false;
            }

            return false;
        }

        public bool TryIgnite() {
            if (!CanIgnite() || burning) {
                return false;
            }

            if (!Api.World.BlockAccessor.GetBlock(Pos.UpCopy()).Code.Path.Contains("bloomerychimneyadvanced")) {
                return false;
            }

            burning = true;
            burningUntilTotalDays = Api.World.Calendar.TotalDays + 5.0 / 12.0;
            burningStartTotalDays = Api.World.Calendar.TotalDays;
            MarkDirty();
            updateSoundState();
            return true;
        }

        public bool CanIgnite() {
            if (!burning && FuelSlot.StackSize > 0 && OreSlot.StackSize > 0) {
                return (float)OreSlot.StackSize / (float)FuelSlot.StackSize <= (float)Ore2FuelRatio;
            }

            return false;
        }

        public override void OnBlockBroken(IPlayer byPlayer = null) {
            if (burning) {
                Vec3d vec3d = Pos.ToVec3d().Add(0.5, 0.5, 0.5);
                bloomeryInv.DropSlots(vec3d, 0, 2);
                breakSparks.MinPos = Pos.ToVec3d().AddCopy(vec3d.X - 0.25, vec3d.Y - 0.25, vec3d.Z - 0.25);
                Api.World.SpawnParticles(breakSparks);
            } else {
                bloomeryInv.DropAll(Pos.ToVec3d().Add(0.5, 0.5, 0.5));
            }
        }

        public override void OnBlockUnloaded() {
            base.OnBlockUnloaded();
            renderer?.Dispose();
            ambientSound?.Dispose();
        }

        public override void OnBlockRemoved() {
            renderer?.Dispose();
            ambientSound?.Dispose();
            base.OnBlockRemoved();
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving) {
            base.FromTreeAttributes(tree, worldForResolving);
            bloomeryInv.FromTreeAttributes(tree);
            burning = tree.GetInt("burning") > 0;
            burningUntilTotalDays = tree.GetDouble("burningUntilTotalDays");
            burningStartTotalDays = tree.GetDouble("burningStartTotalDays");
            updateSoundState();
        }

        public override void ToTreeAttributes(ITreeAttribute tree) {
            base.ToTreeAttributes(tree);
            bloomeryInv.ToTreeAttributes(tree);
            tree.SetInt("burning", burning ? 1 : 0);
            tree.SetDouble("burningUntilTotalDays", burningUntilTotalDays);
            tree.SetDouble("burningStartTotalDays", burningStartTotalDays);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc) {
            if (Api.World.EntityDebugMode && forPlayer != null && (forPlayer.WorldData?.CurrentGameMode).GetValueOrDefault() == EnumGameMode.Creative) {
                dsc.AppendLine(string.Format("Burning: {3}, Current total days: {0}, BurningStart total days: {1}, BurningUntil total days: {2}", Api.World.Calendar.TotalDays, burningStartTotalDays, burningUntilTotalDays, burning));
            }

            for (int i = 0; i < 3; i++) {
                ItemStack itemstack = bloomeryInv[i].Itemstack;
                if (itemstack != null) {
                    if (dsc.Length == 0) {
                        dsc.AppendLine(Lang.Get("Contents:"));
                    }

                    dsc.AppendLine("  " + itemstack.StackSize + "x " + itemstack.GetName());
                }
            }

            base.GetBlockInfo(forPlayer, dsc);
        }

        public float GetHeatStrength(IWorldAccessor world, BlockPos heatSourcePos, BlockPos heatReceiverPos) {
            return IsBurning ? 7 : 0;
        }
    }
}
