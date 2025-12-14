using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace GloomeClasses.src.Alchemist {

    public class BlockEntityGassifier : BlockEntity, ITemperatureSensitive {

        private InventoryGassifier _inventory;

        // TODO: visual fuel rendering could be added in the future
        // would require a custom renderer similar to BEFirepit to display fuel mesh inside the gassifier

        public float prevFurnaceTemperature = 20;
        public float furnaceTemperature = 20;
        public int maxTemperature;
        public float fuelBurnTime;
        public float maxFuelBurnTime;
        public bool canIgniteFuel;
        public float cachedFuel;

        protected ILoadedSound burningFuel;

        public bool IsHot => IsBurning;

        public InventoryGassifier Inventory {
            get { return _inventory; }
        }

        public override void Initialize(ICoreAPI api) {
            base.Initialize(api);
            _inventory ??= new InventoryGassifier(Api, Pos);

            RegisterGameTickListener(OnBurnTick, 100);
            RegisterGameTickListener(On500msTick, 500);

            if (Api.Side.IsClient()) {
                if (IsBurning) {
                    ToggleBurningSound(true);
                }
            }
        }

        public override void OnBlockUnloaded() {
            base.OnBlockUnloaded();
            if (Api.Side.IsServer()) {
                ((ICoreServerAPI)Api).Network.BroadcastBlockEntityPacket(Pos, 1002);
            }
        }

        public virtual int EnviromentTemperature() {
            return 20;
        }

        public void CoolNow(float amountRel) {
            Api.World.PlaySoundAt(new AssetLocation("sounds/effect/extinguish"), Pos, -0.5, null, false, 16);
            fuelBurnTime -= (float)amountRel / 10f;

            if (Api.World.Rand.NextDouble() < amountRel / 5f || fuelBurnTime <= 0) {
                SetBlockState("closed");
                canIgniteFuel = false;
                fuelBurnTime = 0;
                maxFuelBurnTime = 0;
                if (Api.Side.IsServer()) {
                    ((ICoreServerAPI)Api).Network.BroadcastBlockEntityPacket(Pos, 1002);
                }
            }

            MarkDirty(true);
        }

        public bool IsSmoldering => canIgniteFuel;

        public bool IsBurning {
            get { return this.fuelBurnTime > 0; }
        }

        public bool IsOpen() {
            return Block?.Variant["type"] == "open";
        }

        public EnumIgniteState GetIgnitableState(float secondsIgniting) {
            if (Block?.Variant["type"] == "closed") {
                return EnumIgniteState.NotIgnitablePreventDefault;
            }

            if (Inventory.FuelSlot.Empty) {
                return EnumIgniteState.NotIgnitablePreventDefault;
            }

            if (IsBurning) {
                return EnumIgniteState.NotIgnitablePreventDefault;
            }

            return secondsIgniting > 3 ? EnumIgniteState.IgniteNow : EnumIgniteState.Ignitable;
        }

        private void On500msTick(float dt) {
            if (Api is ICoreServerAPI && (IsBurning || prevFurnaceTemperature != furnaceTemperature)) {
                MarkDirty();
            }

            prevFurnaceTemperature = furnaceTemperature;

            if (Api is ICoreServerAPI) {
                if (Api.World.BlockAccessor.GetBlockEntity(Pos.UpCopy()) is BlockEntityMetalBarrel metalBarrel)
                {
                    metalBarrel.GassifierUpdateTemp(furnaceTemperature);

                    // when gasifier starts heating, trigger recipe check in case ingredients were added without opening GUI
                    if (furnaceTemperature > 20 && metalBarrel.CurrentRecipe == null && metalBarrel.CurrentAlcRecipe == null) {
                        metalBarrel.TriggerRecipeCheck();
                    }

                    // always mark dirty when temperature changes to ensure barrel persists temperature
                    metalBarrel.MarkDirty();
                }
            }
        }

        private void OnBurnTick(float dt) {
            if (Api is ICoreClientAPI) {
                return;
            }

            if (fuelBurnTime > 0) {
                fuelBurnTime -= dt / 4;

                if (fuelBurnTime <= 0) {
                    fuelBurnTime = 0;
                    maxFuelBurnTime = 0;
                    if (Block?.Variant["type"] == "lit" && Inventory.FuelSlot.Empty) {
                        SetBlockState("open");
                    }
                    ((ICoreServerAPI)Api).Network.BroadcastBlockEntityPacket(Pos, 1002);
                }
            }

            if (IsBurning) {
                furnaceTemperature = ChangeTemperature(furnaceTemperature, maxTemperature, dt);
            }

            // only auto-ignite when door is closed, matching vanilla firepit behavior
            if (!IsBurning && canIgniteFuel && !Inventory.FuelSlot.Empty && !IsOpen()) {
                IgniteFuel();
            }

            if (!IsBurning) {
                furnaceTemperature = ChangeTemperature(furnaceTemperature, EnviromentTemperature(), dt);
            }
        }

        private void ToggleBurningSound(bool startSound) {
            if (startSound) {
                if (burningFuel == null || !burningFuel.IsPlaying) {
                    burningFuel = ((IClientWorldAccessor)Api.World).LoadSound(new SoundParams() {
                        Location = new AssetLocation("game:sounds/environment/fire.ogg"),
                        ShouldLoop = true,
                        Position = Pos.ToVec3f().Add(0.5f, 0.5f, 0.5f),
                        DisposeOnFinish = false,
                        Volume = 0,
                        Range = 6,
                        SoundType = EnumSoundType.Ambient
                    });

                    if (burningFuel != null) {
                        burningFuel.Start();
                        burningFuel.FadeTo(0.7, 1f, (s) => { });
                    }
                } else {
                    if (burningFuel.IsPlaying) {
                        burningFuel.FadeTo(0.7, 1f, (s) => { });
                    }
                }
            } else {
                burningFuel?.FadeOut(1f, (s) => { s.Dispose(); burningFuel = null; });
            }
        }

        public float ChangeTemperature(float fromTemp, float toTemp, float dt) {
            float diff = Math.Abs(fromTemp - toTemp);
            dt += dt * (diff / 28);

            if (diff < dt) {
                return toTemp;
            }

            if (fromTemp > toTemp) {
                dt = -dt;
            }

            if (Math.Abs(fromTemp - toTemp) < 1) {
                return toTemp;
            }

            return fromTemp + dt;
        }

        public void IgniteFuel() {
            IgniteWithFuel(Inventory.FuelSlot.Itemstack);
            Inventory.FuelSlot.Itemstack.StackSize -= 1;
            if (Inventory.FuelSlot.Itemstack.StackSize <= 0) {
                Inventory.FuelSlot.Itemstack = null;
            }
            if (Api.Side.IsServer()) {
                ((ICoreServerAPI)Api).Network.BroadcastBlockEntityPacket(Pos, 1001);
            }
        }

        public void IgniteWithFuel(IItemStack stack) {
            CombustibleProperties fuelCopts = stack.Collectible.CombustibleProps;

            maxFuelBurnTime = fuelBurnTime = fuelCopts.BurnDuration;
            maxTemperature = (int)(fuelCopts.BurnTemperature);
            SetBlockState("lit");
            MarkDirty(true);
        }

        public void SetBlockState(string state) {
            var curType = Block.Variant["type"];
            if (curType != null && ShouldSlamDoor(curType, state) && Api.Side.IsServer()) {
                Api.World.PlaySoundAt(new AssetLocation("game:sounds/block/metaldoor.ogg"), Pos.X, Pos.Y, Pos.Z, null, true, 32f, 1f);
            }

            AssetLocation loc = Block.CodeWithVariant("type", state);
            Block block = Api.World.GetBlock(loc);
            if (block == null) return;

            Api.World.BlockAccessor.ExchangeBlock(block.Id, Pos);
            this.Block = block;
        }

        private bool ShouldSlamDoor(string curType, string setType) {
            if (curType == "open" && (setType == "closed" || setType == "lit")) {
                return true;
            }

            if (curType == "closed" && setType == "open") {
                return true;
            }

            return false;
        }

        public override void OnBlockBroken(IPlayer byPlayer = null) {
            base.OnBlockBroken(byPlayer);

            if (Inventory != null && !Inventory.FuelSlot.Empty) {
                Inventory.DropAll(Pos.ToVec3d());
            }

            if (Api.Side.IsClient()) {
                ToggleBurningSound(false);
            }
        }

        public override void OnReceivedServerPacket(int packetid, byte[] data) {
            base.OnReceivedServerPacket(packetid, data);

            if (packetid == 1001) {
                ToggleBurningSound(true);
            }

            if (packetid == 1002) {
                ToggleBurningSound(false);
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve) {
            base.FromTreeAttributes(tree, worldAccessForResolve);

            _inventory = new InventoryGassifier(worldAccessForResolve.Api, Pos);
            _inventory.FromTreeAttributes(tree);
            furnaceTemperature = tree.GetFloat("furnaceTemperature");
            maxTemperature = tree.GetInt("maxTemperature");
            fuelBurnTime = tree.GetFloat("fuelBurnTime");
            maxFuelBurnTime = tree.GetFloat("maxFuelBurnTime");
            canIgniteFuel = tree.GetBool("canIgniteFuel", true);
            cachedFuel = tree.GetFloat("cachedFuel", 0);
        }

        public override void ToTreeAttributes(ITreeAttribute tree) {
            base.ToTreeAttributes(tree);

            _inventory.ToTreeAttributes(tree);
            tree.SetFloat("furnaceTemperature", furnaceTemperature);
            tree.SetInt("maxTemperature", maxTemperature);
            tree.SetFloat("fuelBurnTime", fuelBurnTime);
            tree.SetFloat("maxFuelBurnTime", maxFuelBurnTime);
            tree.SetBool("canIgniteFuel", canIgniteFuel);
            tree.SetFloat("cachedFuel", cachedFuel);
        }
    }
}
