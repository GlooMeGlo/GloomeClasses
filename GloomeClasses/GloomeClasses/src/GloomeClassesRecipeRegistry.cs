using GloomeClasses.src.Alchemist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

namespace GloomeClasses.src {

    public class GloomeClassesRecipeRegistry : ModSystem {

        public List<BarrelRecipeWithAdditionalLiquidOut> AlchemistSteelBarrelRecipes = new List<BarrelRecipeWithAdditionalLiquidOut>();
        public List<BarrelRecipeWithAdditionalLiquidOut> AlchemistTinBarrelRecipes = new List<BarrelRecipeWithAdditionalLiquidOut>();
        public List<BarrelRecipeWithAdditionalLiquidOut> AlchemistBarrelRecipes = new List<BarrelRecipeWithAdditionalLiquidOut>();

        public override double ExecuteOrder() {
            return 0.6;
        }

        public override void Start(ICoreAPI api) {
            AlchemistSteelBarrelRecipes = api.RegisterRecipeRegistry<RecipeRegistryGeneric<BarrelRecipeWithAdditionalLiquidOut>>("alcsteelbarrelrecipes").Recipes;
            AlchemistTinBarrelRecipes = api.RegisterRecipeRegistry<RecipeRegistryGeneric<BarrelRecipeWithAdditionalLiquidOut>>("alctinbarrelrecipes").Recipes;
            AlchemistBarrelRecipes = api.RegisterRecipeRegistry<RecipeRegistryGeneric<BarrelRecipeWithAdditionalLiquidOut>>("alcbarrelrecipes").Recipes;
        }

        public override void AssetsLoaded(ICoreAPI api) {
            if (!(api is ICoreServerAPI coreServerAPI)) {
                return;
            }

            ICoreServerAPI sapi = api as ICoreServerAPI;
            if (sapi != null) {
                Dictionary<AssetLocation, BarrelRecipeWithAdditionalLiquidOut> many = sapi.Assets.GetMany<BarrelRecipeWithAdditionalLiquidOut>(sapi.Server.Logger, "recipes/alchemybarrel/stainlesssteel");
                foreach (KeyValuePair<AssetLocation, BarrelRecipeWithAdditionalLiquidOut> item in many) {
                    if (item.Value.Enabled) {
                        item.Value.Resolve(api.World, "stainlesssteel barrel recipe " + item.Key);
                        RegisterStainlessSteelBarrelRecipe(item.Value);
                    }
                }

                many.Clear();
                many = sapi.Assets.GetMany<BarrelRecipeWithAdditionalLiquidOut>(sapi.Server.Logger, "recipes/alchemybarrel/tin");
                foreach (KeyValuePair<AssetLocation, BarrelRecipeWithAdditionalLiquidOut> item in many) {
                    if (item.Value.Enabled) {
                        item.Value.Resolve(api.World, "tin barrel recipe " + item.Key);
                        RegisterTinBarrelRecipe(item.Value);
                    }
                }

                many.Clear();
                many = sapi.Assets.GetMany<BarrelRecipeWithAdditionalLiquidOut>(sapi.Server.Logger, "recipes/alchemybarrel/any");
                foreach (KeyValuePair<AssetLocation, BarrelRecipeWithAdditionalLiquidOut> item in many) {
                    if (item.Value.Enabled) {
                        item.Value.Resolve(api.World, "any alchemist barrel recipe " + item.Key);
                        RegisterAlchemistBarrelRecipe(item.Value);
                    }
                }
            }
        }

        protected void RegisterStainlessSteelBarrelRecipe(BarrelRecipeWithAdditionalLiquidOut recipe) {
            if (recipe.Code == null) {
                throw new ArgumentException("Barrel recipes must have a non-null code! (choose freely)");
            }

            BarrelRecipeIngredient[] ingredients = recipe.Ingredients;
            foreach (BarrelRecipeIngredient barrelRecipeIngredient in ingredients) {
                if (barrelRecipeIngredient.ConsumeQuantity.HasValue && barrelRecipeIngredient.ConsumeQuantity > barrelRecipeIngredient.Quantity) {
                    throw new ArgumentException("Barrel recipe with code {0} has an ingredient with ConsumeQuantity > Quantity. Not a valid recipe!");
                }
            }

            AlchemistSteelBarrelRecipes.Add(recipe);
        }

        protected void RegisterTinBarrelRecipe(BarrelRecipeWithAdditionalLiquidOut recipe) {
            if (recipe.Code == null) {
                throw new ArgumentException("Barrel recipes must have a non-null code! (choose freely)");
            }

            BarrelRecipeIngredient[] ingredients = recipe.Ingredients;
            foreach (BarrelRecipeIngredient barrelRecipeIngredient in ingredients) {
                if (barrelRecipeIngredient.ConsumeQuantity.HasValue && barrelRecipeIngredient.ConsumeQuantity > barrelRecipeIngredient.Quantity) {
                    throw new ArgumentException("Barrel recipe with code {0} has an ingredient with ConsumeQuantity > Quantity. Not a valid recipe!");
                }
            }

            AlchemistTinBarrelRecipes.Add(recipe);
        }

        protected void RegisterAlchemistBarrelRecipe(BarrelRecipeWithAdditionalLiquidOut recipe) {
            if (recipe.Code == null) {
                throw new ArgumentException("Barrel recipes must have a non-null code! (choose freely)");
            }

            BarrelRecipeIngredient[] ingredients = recipe.Ingredients;
            foreach (BarrelRecipeIngredient barrelRecipeIngredient in ingredients) {
                if (barrelRecipeIngredient.ConsumeQuantity.HasValue && barrelRecipeIngredient.ConsumeQuantity > barrelRecipeIngredient.Quantity) {
                    throw new ArgumentException("Barrel recipe with code {0} has an ingredient with ConsumeQuantity > Quantity. Not a valid recipe!");
                }
            }

            AlchemistBarrelRecipes.Add(recipe);
        }

        public List<BarrelRecipeWithAdditionalLiquidOut> GetAlchemistBarrelRecipes(string type) {
            var retList = new List<BarrelRecipeWithAdditionalLiquidOut>();
            retList.AddRange(AlchemistBarrelRecipes);

            if (type == "stainlesssteel") {
                retList.AddRange(AlchemistSteelBarrelRecipes);
            } else if (type == "tin") {
                retList.AddRange(AlchemistTinBarrelRecipes);
            }

            return retList;
        }
    }
}
