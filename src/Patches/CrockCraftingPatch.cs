using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using GloomeClasses.src.Chef;

namespace GloomeClasses.src.Patches {

    // allows vanilla crocks to be used in recipes that output refurbished crocks
    // patching CollectibleObject because BlockCrock no longer overrides MatchesForCrafting in VS 1.22
    [HarmonyPatch(typeof(CollectibleObject))]
    [HarmonyPatchCategory(GloomeClassesModSystem.CrockCraftingPatchCategory)]
    public class CrockCraftingPatch {

        [HarmonyPrefix]
        [HarmonyPatch(nameof(CollectibleObject.MatchesForCrafting))]
        private static bool MatchesForCraftingPrefix(CollectibleObject __instance, ItemStack inputStack, IRecipeBase recipe, IRecipeIngredient ingredient, ref bool __result) {
            if (__instance is not BlockCrock) return true;
            if (recipe is not GridRecipe gr) return true;
            if (gr.Output.ResolvedItemStack?.Collectible is BlockRefurbishedCrock) {
                bool hasSealingIngredient = false;
                for (int i = 0; i < gr.ResolvedIngredients.Length; i++) {
                    var stack = gr.ResolvedIngredients[i]?.ResolvedItemStack;
                    if (stack?.ItemAttributes?["canSealCrock"]?.AsBool(false) == true) {
                        hasSealingIngredient = true;
                        break;
                    }
                }
                // if no sealing ingredient, this is a conversion recipe - allow vanilla matching
                if (!hasSealingIngredient) {
                    __result = true;
                    return false; // skip original method
                }
            }
            return true; // run original method
        }
    }
}
