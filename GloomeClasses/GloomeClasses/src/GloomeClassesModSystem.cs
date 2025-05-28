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
using GloomeClasses.src.EntityBehaviors;
using System;
using Vintagestory.GameContent;
using Vintagestory.API.MathTools;
using System.Linq;
using Vintagestory.API.Util;
using Vintagestory.API.Common.Entities;
using System.Numerics;


namespace GloomeClasses {

    public class GloomeClassesModSystem : ModSystem {

        public static Harmony harmony;

        public const string ClayformingPatchesCategory = "GloomeClassesClayformingPatchesCatagory";
        public const string TemporalStabilityAffectedPatchesCategory = "GloomeClassesTemporalStabilityAffectedPatchesCategory";

        public static ICoreAPI Api;
        public static ICoreClientAPI CApi;
        public static ICoreServerAPI SApi;
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
            api.RegisterEntityBehaviorClass("EntityBehaviorTemporalTraits", typeof(TemporalStabilityTraitBehavior));

            ApplyPatches();
        }

        public override void StartServerSide(ICoreServerAPI api) {
            SApi = api;

            api.Event.PlayerNowPlaying += OnPlayerNowPlayingAddTemporalTraitBehaviors;
            api.Event.PlayerDisconnect += OnPlayerDisconnectRemoveTemporalTraitBehaviors;
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
            harmony.PatchCategory(TemporalStabilityAffectedPatchesCategory);
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

        private static void OnPlayerNowPlayingAddTemporalTraitBehaviors(IServerPlayer player) {
            if (!player.Entity.HasBehavior<TemporalStabilityTraitBehavior>()) {
                var behavior = (TemporalStabilityTraitBehavior)Activator.CreateInstance(typeof(TemporalStabilityTraitBehavior), player.Entity);
                player.Entity.AddBehavior(behavior);
            }
        }

        private static void OnPlayerDisconnectRemoveTemporalTraitBehaviors(IServerPlayer player) {
            if (player.Entity.HasBehavior<TemporalStabilityTraitBehavior>()) {
                var behavior = player.Entity.GetBehavior<TemporalStabilityTraitBehavior>();
                player.Entity.RemoveBehavior(behavior);
            }
        }
    }
}
