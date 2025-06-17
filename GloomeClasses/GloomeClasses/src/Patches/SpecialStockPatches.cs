using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.GameContent;

namespace GloomeClasses.src.Patches {

    [HarmonyPatch(typeof(EntityTradingHumanoid))]
    [HarmonyPatchCategory(GloomeClassesModSystem.SpecialStockPatchesCategory)]
    public class SpecialStockPatches {



    }
}
