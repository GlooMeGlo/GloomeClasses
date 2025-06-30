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

        public List<AlchemyBarrelRecipe> AlchemistSteelBarrelRecipes = new List<AlchemyBarrelRecipe>();
        public List<AlchemyBarrelRecipe> AlchemistTinBarrelRecipes = new List<AlchemyBarrelRecipe>();
        public List<AlchemyBarrelRecipe> AlchemistBarrelRecipes = new List<AlchemyBarrelRecipe>();

        public override double ExecuteOrder() {
            return 0.6;
        }

        public override void Start(ICoreAPI api) {
            AlchemistSteelBarrelRecipes = api.RegisterRecipeRegistry<RecipeRegistryGeneric<AlchemyBarrelRecipe>>("alcsteelbarrelrecipes").Recipes;
            AlchemistTinBarrelRecipes = api.RegisterRecipeRegistry<RecipeRegistryGeneric<AlchemyBarrelRecipe>>("alctinbarrelrecipes").Recipes;
            AlchemistBarrelRecipes = api.RegisterRecipeRegistry<RecipeRegistryGeneric<AlchemyBarrelRecipe>>("alcbarrelrecipes").Recipes;
        }

        public override void AssetsLoaded(ICoreAPI api) {
            if (!(api is ICoreServerAPI coreServerAPI)) {
                return;
            }

            ICoreServerAPI sapi = api as ICoreServerAPI;
            if (sapi != null) {
                Dictionary<AssetLocation, AlchemyBarrelRecipe> many = sapi.Assets.GetMany<AlchemyBarrelRecipe>(sapi.Server.Logger, "recipes/alchemybarrel/stainlesssteel");
                foreach (KeyValuePair<AssetLocation, AlchemyBarrelRecipe> item in many) {
                    if (item.Value.Enabled) {
                        item.Value.Resolve(api.World, "stainlesssteel barrel recipe " + item.Key);
                        RegisterStainlessSteelBarrelRecipe(item.Value);
                    }
                }

                many.Clear();
                many = sapi.Assets.GetMany<AlchemyBarrelRecipe>(sapi.Server.Logger, "recipes/alchemybarrel/tin");
                foreach (KeyValuePair<AssetLocation, AlchemyBarrelRecipe> item in many) {
                    if (item.Value.Enabled) {
                        item.Value.Resolve(api.World, "tin barrel recipe " + item.Key);
                        RegisterTinBarrelRecipe(item.Value);
                    }
                }

                many.Clear();
                many = sapi.Assets.GetMany<AlchemyBarrelRecipe>(sapi.Server.Logger, "recipes/alchemybarrel/any");
                foreach (KeyValuePair<AssetLocation, AlchemyBarrelRecipe> item in many) {
                    if (item.Value.Enabled) {
                        item.Value.Resolve(api.World, "any alchemist barrel recipe " + item.Key);
                        RegisterAlchemistBarrelRecipe(item.Value);
                    }
                }
            }
        }

        protected void RegisterStainlessSteelBarrelRecipe(AlchemyBarrelRecipe recipe) {
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

        protected void RegisterTinBarrelRecipe(AlchemyBarrelRecipe recipe) {
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

        protected void RegisterAlchemistBarrelRecipe(AlchemyBarrelRecipe recipe) {
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

        public List<AlchemyBarrelRecipe> GetAlchemistBarrelRecipes(string type) {
            var retList = new List<AlchemyBarrelRecipe>();
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
