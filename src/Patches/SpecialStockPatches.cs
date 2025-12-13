using GloomeClasses.src.Merchant;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace GloomeClasses.src.Patches {

    [HarmonyPatch(typeof(EntityTradingHumanoid))]
    [HarmonyPatchCategory(GloomeClassesModSystem.SpecialStockPatchesCategory)]
    public class SpecialStockPatches {

        [HarmonyPrefix]
        [HarmonyPatch("Dialog_DialogTriggers")]
        public static bool HandleSpecialStockOpenPrefix(EntityAgent triggeringEntity, ref string value, JsonObject data, EntityTradingHumanoid __instance) {
            if (value == "opensilvertonguetrade") {
                SpecialStockHandling.LoadAndOpenSpecialStock(ref value, __instance);
            }

            return true;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(nameof(EntityTradingHumanoid.OnGameTick))]
        public static IEnumerable<CodeInstruction> CloseSpecialStockTranspiler(IEnumerable<CodeInstruction> instructions) {
            var codes = new List<CodeInstruction>(instructions);

            bool foundFiveFloat = false;
            int indexOfAttemptCloseSpecialStock = -1;

            for (int i = 0; i < codes.Count - 8; i++) {

                if (!foundFiveFloat && codes[i].opcode == OpCodes.Ldc_R4 && (float)(codes[i].operand) == 5f) {
                    foundFiveFloat = true;
                }

                if (foundFiveFloat && codes[i].opcode == OpCodes.Stloc_S) {
                    indexOfAttemptCloseSpecialStock = i + 8;
                    break;
                }
            }

            var putAwaySpecialStockMethod = AccessTools.Method(typeof(SpecialStockHandling), "PutAwaySpecialStock", [typeof(EntityTradingHumanoid)]);

            var injectPutAwaySpecialStock = new List<CodeInstruction> {
                CodeInstruction.LoadArgument(0),
                new(OpCodes.Call, putAwaySpecialStockMethod)
            };

            if (indexOfAttemptCloseSpecialStock > -1) {
                injectPutAwaySpecialStock[0].MoveLabelsFrom(codes[indexOfAttemptCloseSpecialStock]);
                codes.InsertRange(indexOfAttemptCloseSpecialStock, injectPutAwaySpecialStock);
            } else {
                GloomeClassesModSystem.Logger.Error("Could not patch EntityTradingHumanoid's OnGameTick method. Special Stock Trait will not work. More info to follow.");
                if (!foundFiveFloat) {
                    GloomeClassesModSystem.Logger.Error("Unable to locate the creation of the 5f to compare against the squareDist");
                } else if (indexOfAttemptCloseSpecialStock == -1) {
                    GloomeClassesModSystem.Logger.Error("Could not locate where to inject the call to PutAwaySpecialStock.");
                }
            }

            return codes.AsEnumerable();
        }

        [HarmonyPrefix]
        [HarmonyPatch("RefreshBuyingSellingInventory")]
        public static bool ClearSpecialStockAttributePrefix(EntityTradingHumanoid __instance, float refreshChance = 1.1f) {
            SpecialStockHandling.ClearSpecialStockAttribute(__instance);

            return true;
        }
    }
}
