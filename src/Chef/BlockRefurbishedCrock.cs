using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GloomeClasses.src.Chef {

    public class BlockRefurbishedCrock : BlockCrock {

        // refurbished crocks provide better preservation than vanilla crocks
        // rosin sealing provides even better preservation than fat/beeswax sealing

        public override void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, IRecipeBase byRecipe) {
            if (outputSlot.Itemstack == null) return;

            // check if this is a sealing recipe (has a sealing ingredient)
            bool hasSealingIngredient = false;
            bool hasRosin = false;
            ItemSlot crockSlot = null;

            for (int i = 0; i < allInputslots.Length; i++) {
                var slot = allInputslots[i];
                if (slot.Itemstack == null) continue;

                if (slot.Itemstack.Collectible is BlockCrock) {
                    crockSlot = slot;
                } else if (slot.Itemstack.ItemAttributes?["canSealCrock"]?.AsBool(false) == true) {
                    hasSealingIngredient = true;
                    // check if it's rosin specifically
                    if (slot.Itemstack.Collectible.Code.Path == "rosin") {
                        hasRosin = true;
                    }
                }
            }

            // copy attributes from input crock if present
            if (crockSlot != null) {
                outputSlot.Itemstack.Attributes = crockSlot.Itemstack.Attributes.Clone();
            }

            // only set sealed if this is a sealing recipe
            if (hasSealingIngredient) {
                outputSlot.Itemstack.Attributes.SetBool("sealed", true);
                if (hasRosin) {
                    outputSlot.Itemstack.Attributes.SetBool("rosinSealed", true);
                }
            }
        }

        public override float GetContainingTransitionModifierContained(IWorldAccessor world, ItemSlot inSlot, EnumTransitionType transType) {
            float mul = 1f;

            if (transType == EnumTransitionType.Perish) {
                bool isSealed = inSlot.Itemstack.Attributes.GetBool("sealed");
                bool isRosinSealed = inSlot.Itemstack.Attributes.GetBool("rosinSealed");
                string recipeCode = inSlot.Itemstack.Attributes.GetString("recipeCode");

                if (!isSealed) {
                    mul *= 0.85f;  // unsealed: same as vanilla
                } else if (isRosinSealed) {
                    // rosin sealed: better than vanilla
                    mul *= (recipeCode == null) ? 0.0625f : 0.025f;
                } else {
                    // fat/beeswax sealed: same as vanilla (0.25 or 0.1)
                    mul *= (recipeCode == null) ? 0.125f : 0.05f;
                }
            }

            return mul;
        }

        public override float GetContainingTransitionModifierPlaced(IWorldAccessor world, BlockPos pos, EnumTransitionType transType) {
            float mul = 1f;

            if (world.BlockAccessor.GetBlockEntity(pos) is not BlockEntityCrock blockEntityCrock) {
                return mul;
            }

            if (transType == EnumTransitionType.Perish) {
                // check if any item in the crock has the rosinSealed attribute
                bool isRosinSealed = false;
                for (int i = 0; i < blockEntityCrock.Inventory.Count; i++) {
                    var slot = blockEntityCrock.Inventory[i];
                    if (!slot.Empty && slot.Itemstack.Attributes.GetBool("rosinSealed")) {
                        isRosinSealed = true;
                        break;
                    }
                }

                if (!blockEntityCrock.Sealed) {
                    mul *= 0.85f;  // unsealed: same as vanilla
                } else if (isRosinSealed) {
                    // rosin sealed: better than vanilla
                    mul *= (blockEntityCrock.RecipeCode == null) ? 0.0625f : 0.025f;
                } else {
                    // fat/beeswax sealed: same as vanilla (0.25 or 0.1)
                    mul *= (blockEntityCrock.RecipeCode == null) ? 0.125f : 0.05f;
                }
            }

            return mul;
        }
    }
}
