using HarmonyLib;
using System.Reflection;
using Vintagestory;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using GloomeClasses.src.EntityBehaviors;
using System;
using Vintagestory.GameContent;
using Vintagestory.API.MathTools;
using System.Linq;
using Vintagestory.API.Util;
using Vintagestory.API.Common.Entities;
using System.Numerics;
using GloomeClasses.src.CollectibleBehaviors;
using GloomeClasses.src.BlockBehaviors;


namespace GloomeClasses.src {

    public class GloomeClassesModSystem : ModSystem {

        public static Harmony harmony;

        public const string ClayformingPatchesCategory = "gloomeClassesClayformingPatchesCatagory";
        public const string WearableLightsPatchesCategory = "gloomeClassesWearableLightsPatchesCategory";
        public const string ToolkitPatchesCategory = "gloomeClassesToolkitFunctionalityCategory";
        public const string SilverTonguePatchesCategory = "gloomeClassesSilverTonguePatchCategory";

        public static ICoreAPI Api;
        public static ICoreClientAPI CApi;
        public static ICoreServerAPI SApi;
        public static ILogger Logger;
        public static string ModID;

        public const string FlaxRateStat = "flaxFiberChance";
        public const string BonusClayVoxelsStat = "clayformingPoints";

        public const string ToolkitRepairedAttribute = "toolkitRepairedLoss";

        public const float lossPerToolkitRepair = 0.1f;

        public override void StartPre(ICoreAPI api) {
            Api = api;
            Logger = Mod.Logger;
            ModID = Mod.Info.ModID;
        }

        public override void Start(ICoreAPI api) {
            api.RegisterCollectibleBehaviorClass("HealHackedBehavior", typeof(HealHackedLocustsBehavior));
            api.RegisterBlockBehaviorClass("UnlikelyHarvestBehavior", typeof(UnlikelyHarvestBlockBehavior));
            api.RegisterEntityBehaviorClass("EntityBehaviorDread", typeof(DreadBehavior));
            api.RegisterEntityBehaviorClass("EntityBehaviorTemporalTraits", typeof(TemporalStabilityTraitBehavior));
            api.RegisterEntityBehaviorClass("EntityBehaviorDragonskin", typeof(DragonskinTraitBehavior));

            ApplyPatches();
        }

        public override void StartServerSide(ICoreServerAPI api) {
            SApi = api;
        }

        public override void StartClientSide(ICoreClientAPI api) {
            CApi = api;
        }

        private static void ApplyPatches() {
            if (harmony != null) {
                return;
            }

            harmony = new Harmony(ModID);
            Logger.VerboseDebug("Harmony is starting Patches!");
            harmony.PatchCategory(ClayformingPatchesCategory);
            harmony.PatchCategory(WearableLightsPatchesCategory);
            harmony.PatchCategory(ToolkitPatchesCategory);
            harmony.PatchCategory(SilverTonguePatchesCategory);
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
