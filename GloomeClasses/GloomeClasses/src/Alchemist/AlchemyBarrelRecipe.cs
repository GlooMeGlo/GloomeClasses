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
    public class AlchemyBarrelRecipe : BarrelRecipe {

        [DocumentAsJson]
        public double TempRequired = -1;

        [DocumentAsJson]
        public BarrelOutputStack LiquidOutput;

        public new bool Matches(ItemSlot[] inputSlots, out int outputStackSize) {
            var baseRet = base.Matches(inputSlots, out outputStackSize);

            if (LiquidOutput == null && TempRequired > 0 && !baseRet) {
                var hasNoSolidIngredient = true;
                BarrelRecipeIngredient liquidIngredient = null;
                foreach (var ingredient in Ingredients) {
                    if (!ingredient.ConsumeLitres.HasValue) {
                        hasNoSolidIngredient = false;
                    } else {
                        liquidIngredient = ingredient;
                    }
                }

                if (hasNoSolidIngredient && liquidIngredient != null) {
                    foreach (var slot in inputSlots) {
                        if (liquidIngredient.SatisfiesAsIngredient(slot.Itemstack)) {
                            outputStackSize = Output.StackSize;
                            return slot.Itemstack.StackSize >= liquidIngredient.ConsumeLitres;
                        }
                    }
                    return false;
                }
            }

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

        public bool TryCraftNow(ICoreAPI api, double nowSealedHours, float heatedTemp, ItemSlot[] inputslots) {
            var baseRet = base.TryCraftNow(api, nowSealedHours, inputslots);

            if (LiquidOutput == null && TempRequired > 0 && heatedTemp > TempRequired && !baseRet) {
                if (!inputslots[0].Empty && inputslots[0].Itemstack.Collectible.Code != Output.ResolvedItemstack.Collectible.Code) {
                    return false;
                }

                ItemStack outputStack = Output.ResolvedItemstack.Clone();
                outputStack.StackSize = Output.StackSize;

                foreach (var ingredient in Ingredients) {
                    if (ingredient.SatisfiesAsIngredient(inputslots[1].Itemstack)) {
                        inputslots[1].Itemstack.StackSize -= (int)ingredient.ConsumeQuantity;
                        if (inputslots[1].Itemstack.StackSize <= 0) {
                            inputslots[1].Itemstack = null;
                        }
                    }
                }

                if (inputslots[0].Empty) {
                    inputslots[0].Itemstack = outputStack;
                } else {
                    inputslots[0].Itemstack.StackSize += Output.StackSize;
                }

                inputslots[0].MarkDirty();
                inputslots[1].MarkDirty();
                return true;
            }

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

        public new AlchemyBarrelRecipe Clone() {
            BarrelRecipeIngredient[] array = new BarrelRecipeIngredient[Ingredients.Length];
            for (int i = 0; i < Ingredients.Length; i++) {
                array[i] = Ingredients[i].Clone();
            }

            return new AlchemyBarrelRecipe {
                SealHours = SealHours,
                TempRequired = TempRequired,
                Output = Output.Clone(),
                LiquidOutput = LiquidOutput?.Clone(),
                Code = Code,
                Enabled = Enabled,
                Name = Name,
                RecipeId = RecipeId,
                Ingredients = array
            };
        }
    }
}
