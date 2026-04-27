using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace GloomeClasses.src.Patches {

    [HarmonyPatch(typeof(CollectibleObject))]
    [HarmonyPatchCategory(GloomeClassesModSystem.ToolkitPatchesCategory)]
    public class ToolkitPatches {

        [HarmonyPrefix]
        [HarmonyPatch(nameof(CollectibleObject.OnCreatedByCrafting))]
        public static bool ToolkitCreatedByCraftingPrefix(ItemSlot[] allInputSlots, ItemSlot outputSlot, IRecipeBase byRecipe) {
            if (allInputSlots == null || allInputSlots.Length < 2 || outputSlot == null || outputSlot.Inventory == null || outputSlot.Inventory.GetType() == typeof(DummyInventory) || outputSlot.Inventory.GetType() == typeof(CreativeInventoryTab)) {
                return true; //Needs to have a length of 2 or more, since the inventory grid always returns the full 9 slots. If it doesn't have at least 2 slots, it can't possibly be a Toolkit Repair.
            }

            ItemSlot toolkitSlot = null;
            ItemSlot repairedToolSlot = null;

            for (int i = 0; i < allInputSlots.Length; i++) {
                if (allInputSlots[i] == null || allInputSlots[i].Empty) {
                    continue;
                }

                if (allInputSlots[i].Itemstack.Collectible.Code.FirstCodePart() == "toolkit") {
                    toolkitSlot = allInputSlots[i];
                } else if (repairedToolSlot == null && allInputSlots[i].Itemstack.Collectible.GetMaxDurability(allInputSlots[i].Itemstack) > 1) {
                    repairedToolSlot = allInputSlots[i];
                } else {
                    return true; //If it checks 2 items and neither are a Toolkit, then this isn't a recipe to care about, let it continue on.
                }

                if (toolkitSlot != null && repairedToolSlot != null) {
                    break;
                }
            }

            if (toolkitSlot == null || toolkitSlot.Empty || repairedToolSlot == null || repairedToolSlot.Empty) {
                return true;
            }

            float toolMaxDurPenalty = 1f;
            if (repairedToolSlot.Itemstack.Attributes.HasAttribute(GloomeClassesModSystem.ToolkitRepairedAttribute)) {
                toolMaxDurPenalty = repairedToolSlot.Itemstack.Attributes.GetFloat(GloomeClassesModSystem.ToolkitRepairedAttribute);
            }

            var toolkitType = toolkitSlot.Itemstack?.Collectible?.Variant["type"];
            if (toolkitType != null) {
                switch (toolkitType) {
                    case "basic":
                        toolMaxDurPenalty -= GloomeClassesModSystem.lossPerBasicTkRepair;
                        break;
                    case "simple":
                        toolMaxDurPenalty -= GloomeClassesModSystem.lossPerSimpleTkRepair;
                        break;
                    case "standard":
                        toolMaxDurPenalty -= GloomeClassesModSystem.lossPerStandardTkRepair;
                        break;
                    case "advanced":
                        toolMaxDurPenalty -= GloomeClassesModSystem.lossPerAdvancedTkRepair;
                        break;
                    default:
                        GloomeClassesModSystem.Logger.Warning("Toolkit has a Type variant, but not a known one. Will default to the largest penalty.");
                        toolMaxDurPenalty -= GloomeClassesModSystem.lossPerBasicTkRepair;
                        break;
                }
            } else {
                GloomeClassesModSystem.Logger.Error("Somehow a Toolkit lacks a 'type' variant? Or somehow improperly detecting a Toolkit. Not applying penalty and just reverting to vanilla.");
                //toolMaxDurPenalty -= GloomeClassesModSystem.lossPerBasicTkRepair;
                return true;
            }

            outputSlot.Itemstack.Attributes.SetFloat(GloomeClassesModSystem.ToolkitRepairedAttribute, toolMaxDurPenalty);

            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(CollectibleObject.GetMaxDurability))]
        private static void ToolkitMaxDurabilityPenaltyPostfix(ref int __result, ItemStack itemstack) {
            if (itemstack.Attributes.HasAttribute(GloomeClassesModSystem.ToolkitRepairedAttribute)) {
                var toolkitLoss = itemstack.Attributes.GetFloat(GloomeClassesModSystem.ToolkitRepairedAttribute, 1);
                __result = (int)MathF.Round((float)__result * toolkitLoss);
            }
        }
    }
}