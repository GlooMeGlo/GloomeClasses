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
