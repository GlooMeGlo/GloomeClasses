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
        public static bool ToolkitCreatedByCraftingPrefix(ItemSlot[] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe) {
            if (allInputslots == null || allInputslots.Length < 2 || outputSlot == null || outputSlot.Inventory == null || outputSlot.Inventory.GetType() == typeof(DummyInventory) || outputSlot.Inventory.GetType() == typeof(CreativeInventoryTab)) {
                return true; //Needs to have a length of 2 or more, since the inventory grid always returns the full 9 slots. If it doesn't have at least 2 slots, it can't possibly be a Toolkit Repair.
            }
            
            ItemSlot toolkitSlot = null;
            ItemSlot repairedToolSlot = null;

            for (int i = 0; i < allInputslots.Length; i++) {
                if (allInputslots[i] == null || allInputslots[i].Empty) {
                    continue;
                }

                if (allInputslots[i].Itemstack.Collectible.Code.FirstCodePart() == "toolkit") {
                    toolkitSlot = allInputslots[i];
                } else if (repairedToolSlot == null && allInputslots[i].Itemstack.Collectible.GetMaxDurability(allInputslots[i].Itemstack) > 1) {
                    repairedToolSlot = allInputslots[i];
                } else {
                    return true; //If it checks 2 items and neither are a Toolkit, then this isn't a recipe to care about, let it continue on.
                }

                if (toolkitSlot != null && repairedToolSlot != null) {
                    break;
                }
            }

            float toolMaxDurPenalty = 1f;
            if (repairedToolSlot.Itemstack.Attributes.HasAttribute(GloomeClassesModSystem.ToolkitRepairedAttribute)) {
                toolMaxDurPenalty = repairedToolSlot.Itemstack.Attributes.GetFloat(GloomeClassesModSystem.ToolkitRepairedAttribute);
            }

            var toolkitType = toolkitSlot.Itemstack.Collectible.Variant["type"];
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
                GloomeClassesModSystem.Logger.Error("Somehow a Toolkit lacks a 'type' variant? Could not retreive one, so defaulting to the largest durability penalty.");
                toolMaxDurPenalty -= GloomeClassesModSystem.lossPerBasicTkRepair;
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