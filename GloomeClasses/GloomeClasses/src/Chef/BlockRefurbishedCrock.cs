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

        public override float GetContainingTransitionModifierContained(IWorldAccessor world, ItemSlot inSlot, EnumTransitionType transType) {
            float num = 1f;
            if (transType == EnumTransitionType.Perish) {
                num = ((!inSlot.Itemstack.Attributes.GetBool("sealed")) ? (num * 0.85f) : ((inSlot.Itemstack.Attributes.GetString("recipeCode") == null) ? (num * 0.125f) : (num * 0.05f)));
            }

            return num;
        }

        public override float GetContainingTransitionModifierPlaced(IWorldAccessor world, BlockPos pos, EnumTransitionType transType) {
            float num = 1f;
            if (!(world.BlockAccessor.GetBlockEntity(pos) is BlockEntityCrock blockEntityCrock)) {
                return num;
            }

            if (transType == EnumTransitionType.Perish) {
                num = ((!blockEntityCrock.Sealed) ? (num * 0.85f) : ((blockEntityCrock.RecipeCode == null) ? (num * 0.125f) : (num * 0.05f)));
            }

            return num;
        }
    }
}
