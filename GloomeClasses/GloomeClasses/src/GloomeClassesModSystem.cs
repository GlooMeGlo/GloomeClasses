using HarmonyLib;
using GloomeClasses.BlockBehaviors;
using GloomeClasses.EntityBehaviors;
using System.Reflection;
using Vintagestory;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;


namespace GloomeClasses {

    public class GloomeClassesModSystem : ModSystem {

        public static Harmony harmony;

        public const string ClayformingPatchesCatagory = "GloomeClassesClayformingPatchesCatagory";

        public static ICoreAPI Api;
        public static ILogger Logger;
        public static string ModID;

        public const string FlaxRateStat = "flaxSeedChance";
        public const string BonusClayVoxelsStat = "clayformingPoints";

        public override void StartPre(ICoreAPI api) {
            Api = api;
            Logger = Mod.Logger;
            ModID = Mod.Info.ModID;
        }

        public override void Start(ICoreAPI api) {
            api.RegisterBlockBehaviorClass("UnlikelyHarvestBehavior", typeof(UnlikelyHarvestBlockBehavior));
            api.RegisterEntityBehaviorClass("EntityBehaviorDread", typeof(DreadBehavior));

            ApplyPatches();
        }

        public override void StartServerSide(ICoreServerAPI api) {
            
        }

        public override void StartClientSide(ICoreClientAPI api) {
            
        }

        private static void ApplyPatches() {
            if (harmony != null) {
                return;
            }

            harmony = new Harmony(ModID);
            Logger.VerboseDebug("Harmony is starting Patches!");
            harmony.PatchCategory(ClayformingPatchesCatagory);
            Logger.VerboseDebug("Finished patching for Trait purposes.");
        }

        private static void HarmonyUnpatch() {
            Logger?.VerboseDebug("Unpatching Harmony Patches.");
            harmony?.UnpatchAll(ModID);
            harmony = null;
        }

        public override void Dispose() {
            HarmonyUnpatch();
            Logger = null;
            ModID = null;
            Api = null;
            base.Dispose();
        }
    }
}
