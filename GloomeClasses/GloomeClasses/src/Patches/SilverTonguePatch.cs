using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace GloomeClasses.src.Patches {

    [HarmonyPatch(typeof(InventoryTrader))]
    [HarmonyPatchCategory(GloomeClassesModSystem.SilverTonguePatchesCategory)]
    public class SilverTonguePatch {

        [HarmonyTranspiler]
        [HarmonyPatch("HandleMoneyTransaction")]
        private static IEnumerable<CodeInstruction> SilverTongueHandleMoneyTransactionTranspiler(IEnumerable<CodeInstruction> instructions) {
            var codes = new List<CodeInstruction>(instructions);

            int indexOfGetTotalGainCall = -1;

            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].opcode == OpCodes.Stloc_2 && codes[i+1].opcode == OpCodes.Ldloc_1) {
                    indexOfGetTotalGainCall = i + 1;
                    break;
                }
            }

            var applySilverTonguedCostCall = AccessTools.Method(typeof(SilverTonguePatch), "ApplySilverTonguedTraitCost", new Type[2] { typeof(IPlayer), typeof(int) });
            var applySilverTonguedGainCall = AccessTools.Method(typeof(SilverTonguePatch), "ApplySilverTonguedTraitGain", new Type[2] { typeof(IPlayer), typeof(int) });

            var applySilverTonguedTrait = new List<CodeInstruction> {
                CodeInstruction.LoadArgument(1),
                CodeInstruction.LoadLocal(1),
                new CodeInstruction(OpCodes.Call, applySilverTonguedCostCall),
                CodeInstruction.StoreLocal(1),
                CodeInstruction.LoadArgument(1),
                CodeInstruction.LoadLocal(2),
                new CodeInstruction(OpCodes.Call, applySilverTonguedGainCall),
                CodeInstruction.StoreLocal(2)
            };

            if (indexOfGetTotalGainCall > -1) {
                codes.InsertRange(indexOfGetTotalGainCall, applySilverTonguedTrait);
            } else {
                GloomeClassesModSystem.Logger.Error("Could not locate the GetTotalGain call in HandleMoneyTransaction in InventoryTrader. Silver Tongue Trait will not function.");
            }

            return codes.AsEnumerable();
        }

        private static int ApplySilverTonguedTraitCost(IPlayer player, int totalCost) {
            string classcode = player.Entity.WatchedAttributes.GetString("characterClass");
            CharacterClass charclass = player.Entity.Api.ModLoader.GetModSystem<CharacterSystem>().characterClasses.FirstOrDefault(c => c.Code == classcode);
            if (charclass != null && charclass.Traits.Contains("silvertongue")) {
                if (totalCost > 0) {
                    totalCost -= (int)MathF.Round(totalCost * 0.25f);
                }
            }

            return totalCost;
        }

        private static int ApplySilverTonguedTraitGain(IPlayer player, int totalGain) {
            string classcode = player.Entity.WatchedAttributes.GetString("characterClass");
            CharacterClass charclass = player.Entity.Api.ModLoader.GetModSystem<CharacterSystem>().characterClasses.FirstOrDefault(c => c.Code == classcode);
            if (charclass != null && charclass.Traits.Contains("silvertongue")) {
                if (totalGain > 0) {
                    totalGain += (int)MathF.Round(totalGain * 0.25f);
                }
            }

            return totalGain;
        }
    }

    [HarmonyPatch(typeof(GuiDialogTrader))]
    [HarmonyPatchCategory(GloomeClassesModSystem.SilverTonguePatchesCategory)]
    public class SilverTongueGuiPatch {

        [HarmonyTranspiler]
        [HarmonyPatch("TraderInventory_SlotModified")]
        public static IEnumerable<CodeInstruction> SilverTongueGuiTraderTranspiler(IEnumerable<CodeInstruction> instructions) {
            var codes = new List<CodeInstruction>(instructions);

            var adjustCostMethod = AccessTools.Method(typeof(SilverTongueGuiPatch), "AdjustSilverTongueCost", new Type[1] { typeof(int) });
            var adjustGainMethod = AccessTools.Method(typeof(SilverTongueGuiPatch), "AdjustSilverTongueGain", new Type[1] { typeof(int) });
            var handleSilverTongueVisuals = new List<CodeInstruction> {
                CodeInstruction.LoadLocal(0),
                new CodeInstruction(OpCodes.Call, adjustCostMethod),
                CodeInstruction.StoreLocal(0),
                CodeInstruction.LoadLocal(1),
                new CodeInstruction(OpCodes.Call, adjustGainMethod),
                CodeInstruction.StoreLocal(1)
            };

            codes.InsertRange(8, handleSilverTongueVisuals);

            return codes.AsEnumerable();
        }

        private static int AdjustSilverTongueCost(int totalCost) {
            if (GloomeClassesModSystem.CApi != null && GloomeClassesModSystem.CApi.Side.IsClient()) {
                var player = GloomeClassesModSystem.CApi.World.PlayerByUid(GloomeClassesModSystem.CApi.World.Player.PlayerUID);
                string classcode = player.Entity.WatchedAttributes.GetString("characterClass");
                CharacterClass charclass = player.Entity.Api.ModLoader.GetModSystem<CharacterSystem>().characterClasses.FirstOrDefault(c => c.Code == classcode);
                if (charclass != null && charclass.Traits.Contains("silvertongue")) {
                    if (totalCost > 0) {
                        totalCost -= (int)MathF.Round(totalCost * 0.25f);
                    }
                }
            }

            return totalCost;
        }

        private static int AdjustSilverTongueGain(int totalGain) {
            if (GloomeClassesModSystem.CApi != null && GloomeClassesModSystem.CApi.Side.IsClient()) {
                var player = GloomeClassesModSystem.CApi.World.PlayerByUid(GloomeClassesModSystem.CApi.World.Player.PlayerUID);
                string classcode = player.Entity.WatchedAttributes.GetString("characterClass");
                CharacterClass charclass = player.Entity.Api.ModLoader.GetModSystem<CharacterSystem>().characterClasses.FirstOrDefault(c => c.Code == classcode);
                if (charclass != null && charclass.Traits.Contains("silvertongue")) {
                    if (totalGain > 0) {
                        totalGain += (int)MathF.Round(totalGain * 0.25f);
                    }
                }
            }

            return totalGain;
        }
    }
}
