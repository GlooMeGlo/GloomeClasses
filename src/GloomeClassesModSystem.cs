using GloomeClasses.src.Diagnostics;
using GloomeClasses.src.Alchemist;
using GloomeClasses.src.BlockBehaviors;
using GloomeClasses.src.Chef;
using GloomeClasses.src.CollectibleBehaviors;
using GloomeClasses.src.EntityBehaviors;
using GloomeClasses.src.Smith;
using GloomeClasses.src.Utils;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Vintagestory;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;
using GloomeClasses.src.Diagnostics.Patches;
using GloomeClasses.src.Merchant;


namespace GloomeClasses.src {

    public class GloomeClassesModSystem : ModSystem {

        public static Harmony harmony;

        public const string ClayformingPatchesCategory = "gloomeClassesClayformingPatchesCatagory";
        public const string WearableLightsPatchesCategory = "gloomeClassesWearableLightsPatchesCategory";
        public const string ToolkitPatchesCategory = "gloomeClassesToolkitFunctionalityCategory";
        public const string SilverTonguePatchesCategory = "gloomeClassesSilverTonguePatchCategory";
        public const string SpecialStockPatchesCategory = "gloomeClassesSpecialStockPatchesCategory";
        //public const string ChefRosinPatchCategory = "gloomeClassesChefRosinPatchCategory";
        public const string BlockSchematicPatchCategory = "gloomeClassesBlockSchematicPatchCategory";
        public const string StaticTranslocatorPatchesCategory = "gloomeClassesStaticTranslocatorBlockPatchCategory";
        public const string DragonskinPatchCategory = "gloomeClassesDragonskinPatchCategory";
        public const string DiagnosticPatchCategory = "gloomeClassesDiagnosticsPatchCategory";
        public const string CrockCraftingPatchCategory = "gloomeClassesCrockCraftingPatchCategory";
        public const string ForesterPatchCategory = "gloomeClassesForesterPatchCategory";

        public static ICoreAPI Api;
        public static ICoreClientAPI CApi;
        public static ICoreServerAPI SApi;
        public static ILogger Logger;
        public static string ModID;

        public const string FlaxRateStat = "flaxFiberChance";
        public const string BonusClayVoxelsStat = "clayformingPoints";

        public const string ToolkitRepairedAttribute = "toolkitRepairedLoss";

        public const float lossPerBasicTkRepair = 0.2f;
        public const float lossPerSimpleTkRepair = 0.15f;
        public const float lossPerStandardTkRepair = 0.1f;
        public const float lossPerAdvancedTkRepair = 0.05f;

        public override void StartPre(ICoreAPI api) {
            Api = api;
            Logger = Mod.Logger;
            ModID = Mod.Info.ModID;

            // Initialize diagnostic systems
            DiagnosticLogger.Initialize(api, Logger);
            MeshDiagnostics.Initialize(api);
        }

        public override void Start(ICoreAPI api) {
            api.RegisterCollectibleBehaviorClass("HealHackedBehavior", typeof(HealHackedLocustsBehavior));
            api.RegisterCollectibleBehaviorClass("ClairvoyanceBehavior", typeof(ClairvoyanceBehavior));
            api.RegisterBlockBehaviorClass("UnlikelyHarvestBehavior", typeof(UnlikelyHarvestBlockBehavior));
            api.RegisterBlockEntityBehaviorClass("TranslocatorPOIBehavior", typeof(TranslocatorTrackerBlockEntityBehavior));

            api.RegisterEntityBehaviorClass("EntityBehaviorDread", typeof(DreadBehavior));
            api.RegisterEntityBehaviorClass("EntityBehaviorFanatic", typeof(FanaticBehavior));
            api.RegisterEntityBehaviorClass("EntityBehaviorTemporalTraits", typeof(TemporalStabilityTraitBehavior));
            api.RegisterEntityBehaviorClass("EntityBehaviorDragonskin", typeof(DragonskinTraitBehavior));

            api.RegisterBlockClass("BlockAdvBloomery", typeof(BlockAdvancedBloomery));
            api.RegisterBlockEntityClass("BlockEntityAdvBloomery", typeof(BlockEntityAdvancedBloomery));
            api.RegisterBlockClass("BlockMetalBarrel", typeof(BlockMetalBarrel));
            api.RegisterBlockEntityClass("BlockEntityMetalBarrel", typeof(BlockEntityMetalBarrel));
            api.RegisterBlockEntityClass("POITrackerDummyBlockEntity", typeof(POITrackerDummyBlockEntity));
            api.RegisterBlockClass("BlockRefurbishedCrock", typeof(BlockRefurbishedCrock));
            api.RegisterBlockClass("BlockGassifier", typeof(BlockGassifier));
            api.RegisterBlockEntityClass("BlockEntityGassifier", typeof(BlockEntityGassifier));

            ApplyPatches();

            // log startup diagnostics
            Logger.Notification("═══════════════════════════════════════════");
            Logger.Notification("GloomeClasses v{0}", Mod.Info.Version);
            Logger.Notification("═══════════════════════════════════════════");
            Logger.Notification("");
            Logger.Notification("Diagnostic Features:");
            Logger.Notification("  • Mod compatibility detection");
            Logger.Notification("  • Character/trait system monitoring");
            Logger.Notification("  • Environment detection logging");
            Logger.Notification("");

            // run diagnostic checks
            DiagnosticLogger.LogModLoadOrder(api);
            DiagnosticLogger.LogThirdPartyModPresence(api);
            DiagnosticLogger.LogCharselCommandNote();
        }

        public override void StartServerSide(ICoreServerAPI api) {
            SApi = api;

            // register player join event for class migration
            api.Event.PlayerJoin += OnPlayerJoin;

            // debug command to force refresh special stock on nearby traders
            api.ChatCommands.Create("refreshspecialstock")
                .WithDescription("Force refresh special stock on nearby traders (debug)")
                .RequiresPrivilege(Privilege.controlserver)
                .HandleWith(args => {
                    var player = args.Caller.Player;
                    if (player?.Entity == null) {
                        return TextCommandResult.Error("Must be called by a player");
                    }

                    int count = 0;
                    var nearbyTraders = api.World.GetEntitiesAround(
                        player.Entity.Pos.XYZ,
                        50, 50,
                        e => e is EntityTradingHumanoid
                    );

                    foreach (var entity in nearbyTraders) {
                        if (entity is not EntityTradingHumanoid trader) continue;

                        // clear special stock so it regenerates fresh next time
                        SpecialStockHandling.ClearSpecialStockAttribute(trader);

                        // clear temp main if stuck in special stock mode
                        if (trader.WatchedAttributes.HasAttribute(SpecialStockHandling.TempMainAttribute)) {
                            trader.WatchedAttributes.RemoveAttribute(SpecialStockHandling.TempMainAttribute);
                            trader.WatchedAttributes.MarkAllDirty();
                        }

                        count++;
                    }

                    return TextCommandResult.Success($"Cleared special stock on {count} trader(s) within 50 blocks");
                });
        }

        /// <summary>
        /// mapping of vanilla class codes to their GloomeClasses equivalents.
        /// used to auto-migrate players who had vanilla classes before installing this mod.
        /// </summary>
        private static readonly Dictionary<string, string> VanillaToGloomeClassMap = new() {
            { "commoner", "commonergloo" },
            { "hunter", "huntergloo" },
            { "malefactor", "malefactorgloo" },
            { "clockmaker", "clockmakergloo" },
            { "blackguard", "blackguardgloo" },
            { "tailor", "tailorgloo" }
        };

        private void OnPlayerJoin(IServerPlayer player) {
            try {
                if (player?.Entity == null) return;

                string currentClass = player.Entity.WatchedAttributes.GetString("characterClass");
                if (string.IsNullOrEmpty(currentClass)) return;

                // check if the player has a vanilla class that needs migration
                if (VanillaToGloomeClassMap.TryGetValue(currentClass, out string newClass)) {
                    // migrate to GloomeClasses equivalent
                    player.Entity.WatchedAttributes.SetString("characterClass", newClass);
                    player.Entity.WatchedAttributes.MarkPathDirty("characterClass");

                    DiagnosticLogger.LogClassMigration(player.PlayerName, currentClass, newClass);

                    // notify the player
                    player.SendMessage(
                        GlobalConstants.GeneralChatGroup,
                        $"[GloomeClasses] Your character class has been migrated from '{currentClass}' to '{newClass}'.",
                        EnumChatType.Notification
                    );
                }
            } catch (Exception ex) {
                Logger?.Error("[GloomeClasses] REPORT THIS! Error during player class migration: {0}", ex.Message);
            }
        }

        public override void StartClientSide(ICoreClientAPI api) {
            CApi = api;

            // register mesh diagnostics command
            api.ChatCommands.Create("meshdiag")
                .WithDescription("Display mesh generation diagnostics")
                .RequiresPrivilege(Privilege.controlserver)
                .HandleWith(args => {
                    MeshDiagnostics.GenerateReport();
                    return TextCommandResult.Success("Mesh diagnostics report generated. Check logs.");
                });
        }

        public override void AssetsLoaded(ICoreAPI api) {
            // verify character system state after assets load
            DiagnosticLogger.LogCharacterSystemState(api);
            DiagnosticLogger.LogAvailableCharacterClasses(api);
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
            harmony.PatchCategory(SpecialStockPatchesCategory);
            //harmony.PatchCategory(ChefRosinPatchCategory);
            harmony.PatchCategory(BlockSchematicPatchCategory);
            harmony.PatchCategory(StaticTranslocatorPatchesCategory);
            harmony.PatchCategory(DragonskinPatchCategory);
            harmony.PatchCategory(CrockCraftingPatchCategory);
            harmony.PatchCategory(ForesterPatchCategory);

            // apply diagnostic patches
            TraitSystemDiagnostics.ApplyTraitSystemPatches(harmony);

            // apply CharacterSystem diagnostic patches (uses Harmony attributes)
            try
            {
                harmony.CreateClassProcessor(typeof(CharacterSystemDiagnosticPatches)).Patch();
                Logger.VerboseDebug("[GloomeClasses] CharacterSystem diagnostic patches applied");
            }
            catch (Exception ex)
            {
                Logger.Error("[GloomeClasses] Failed to apply CharacterSystem diagnostic patches: {0}", ex.Message);
            }

            Logger.VerboseDebug("[GloomeClasses] Diagnostic patches applied");

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
