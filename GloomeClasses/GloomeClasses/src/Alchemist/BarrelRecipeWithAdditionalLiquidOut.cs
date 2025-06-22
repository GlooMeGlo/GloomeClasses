using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace GloomeClasses.src.Alchemist {

    [DocumentAsJson]
    public class BarrelRecipeWithAdditionalLiquidOut : BarrelRecipe {

        [DocumentAsJson]
        public BarrelOutputStack LiquidOutput;

        public new bool Matches(ItemSlot[] inputSlots, out int outputStackSize) {
            var baseRet = base.Matches(inputSlots, out outputStackSize);

            if (LiquidOutput != null && baseRet) {
                foreach (var ingredient in Ingredients) {
                    if (ingredient.ConsumeLitres.HasValue) {
                        foreach (var slot in inputSlots) {
                            if (ingredient.SatisfiesAsIngredient(slot.Itemstack)) {
                                return slot.Itemstack.StackSize % outputStackSize == 0;
                            }
                        }
                        return false;
                    }
                }
            }

            return baseRet;
        }

        public new bool TryCraftNow(ICoreAPI api, double nowSealedHours, ItemSlot[] inputslots) {
            var baseRet = base.TryCraftNow(api, nowSealedHours, inputslots);

            if (LiquidOutput != null && baseRet) {
                inputslots[1].Itemstack = LiquidOutput.ResolvedItemstack;
                inputslots[1].Itemstack.StackSize = LiquidOutput.StackSize * (inputslots[0].Itemstack.StackSize / Output.StackSize);
            }

            return baseRet;
        }

        public new bool Resolve(IWorldAccessor world, string sourceForErrorLogging) {
            var baseRet = base.Resolve(world, sourceForErrorLogging);

            if (LiquidOutput != null && baseRet) {
                baseRet &= LiquidOutput.Resolve(world, sourceForErrorLogging);
                if (baseRet) {
                    WaterTightContainableProps containableProps = BlockLiquidContainerBase.GetContainableProps(LiquidOutput.ResolvedItemstack);
                    if (containableProps != null) {
                        if (LiquidOutput.Litres < 0f) {
                            if (LiquidOutput.Quantity > 0) {
                                world.Logger.Warning("Barrel recipe {0}, output {1} does not define a litres attribute but a stacksize, will assume stacksize=litres for backwards compatibility.", sourceForErrorLogging, Output.Code);
                                LiquidOutput.Litres = LiquidOutput.Quantity;
                            } else {
                                LiquidOutput.Litres = 1f;
                            }
                        }

                        LiquidOutput.Quantity = (int)(containableProps.ItemsPerLitre * LiquidOutput.Litres);
                    }
                }
            }

            return baseRet;
        }

        public new BarrelRecipeWithAdditionalLiquidOut Clone() {
            BarrelRecipeIngredient[] array = new BarrelRecipeIngredient[Ingredients.Length];
            for (int i = 0; i < Ingredients.Length; i++) {
                array[i] = Ingredients[i].Clone();
            }

            return new BarrelRecipeWithAdditionalLiquidOut {
                SealHours = SealHours,
                Output = Output.Clone(),
                LiquidOutput = LiquidOutput.Clone(),
                Code = Code,
                Enabled = Enabled,
                Name = Name,
                RecipeId = RecipeId,
                Ingredients = array
            };
        }
    }
}
