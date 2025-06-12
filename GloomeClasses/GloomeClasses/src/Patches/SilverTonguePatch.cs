using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.GameContent;

namespace GloomeClasses.src.Patches {

    [HarmonyPatch(typeof(InventoryTrader))]
    [HarmonyPatchCategory(GloomeClassesModSystem.SilverTonguePatchesCategory)]
    public class SilverTonguePatch {

        [HarmonyTranspiler]
        [HarmonyPatch("HandleMoneyTransaction")]
        private static IEnumerable<CodeInstruction> SilverTongueHandleMoneyTransactionTranspiler(IEnumerable<CodeInstruction> instructions) {
            var codes = new List<CodeInstruction>(instructions);



            return codes.AsEnumerable();
        }
    }
}
