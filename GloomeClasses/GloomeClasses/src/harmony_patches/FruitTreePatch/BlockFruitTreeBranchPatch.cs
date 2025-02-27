using HarmonyLib;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;


namespace GloomeClasses.src.harmony_patches.FruitTreePatch
{

    [HarmonyPatch(typeof(BlockFruitTreeBranch))]
    public class BlockFruitTreeBranchPatch
    {

        public static bool Prepare(MethodBase original)
        {
            //return false;
            return true;
        }
         

        [HarmonyPostfix]
        [HarmonyPatch("TryPlaceBlock")]
        public static void TryPlaceBlockPostfix(bool __result, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (!__result) return;
            if (byPlayer == null) return;

            BlockEntityFruitTreeBranch be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityFruitTreeBranch;
            BlockEntityBehaviorValue beh = new BlockEntityBehaviorValue(be);

            float statMult = byPlayer.Entity.Stats.GetBlended("fruittreeSurvival");

            beh.Value = statMult;
            be.Behaviors.Add(beh);
        }
    }


    public struct FruitTreeGrowingState
    {
        public float CuttingGraftChance;
        public float CuttingRootingChance;
        public FruitTreeTypeProperties props;

    }

    [HarmonyPatch(typeof(BlockEntityFruitTreeBranch))]
    public class BlockEntityFruitTreeBranchPatch
    {
        /* public static bool Prepare(MethodBase original)
         {
             return BlockFruitTreeBranchPatch.Prepare(original);
         }*/

        [HarmonyPostfix]
        [HarmonyPatch("FromTreeAttributes")]
        public static void FromTreeAttributesPostfix(BlockEntityFruitTreeBranch __instance, ITreeAttribute tree)
        {
            if (__instance.GetBehavior<FruitTreeGrowingBranchBH> == null) return;
            float value = tree.GetFloat("value", -1.0f);
            if (value <= 0.0f) return;

            BlockEntityBehaviorValue beh = __instance.Behaviors.Find(x => x is BlockEntityBehaviorValue) as BlockEntityBehaviorValue;
            if (beh == null)
            {
                beh = new BlockEntityBehaviorValue(__instance);
                __instance.Behaviors.Add(beh);
            }
            beh.Value = value;
        }


    }


    [HarmonyPatch(typeof(FruitTreeGrowingBranchBH))]
    public class FruitTreeGrowingBranchBHPatch
    {
        public static void CommonPrefix(FruitTreeGrowingBranchBH instance, out FruitTreeGrowingState state, BlockFruitTreeBranch branchBlock)
        {
            state = new FruitTreeGrowingState();


            state = new FruitTreeGrowingState();
            BlockEntityBehaviorValue behaviorValue = instance.Blockentity.GetBehavior<BlockEntityBehaviorValue>();
            if (behaviorValue == null) return;

            BlockEntityFruitTreeBranch ownBe = instance.Blockentity as BlockEntityFruitTreeBranch;
            branchBlock.TypeProps.TryGetValue(ownBe.TreeType, out var typeprops);
            if (typeprops == null) return;

            state.props = typeprops;
            state.CuttingGraftChance = typeprops.CuttingGraftChance;
            state.CuttingRootingChance = typeprops.CuttingRootingChance;

            typeprops.CuttingGraftChance *= behaviorValue.Value;
            typeprops.CuttingRootingChance *= behaviorValue.Value;

        }

        public static void CommonPostfix(FruitTreeGrowingState state)
        {
            if (state.props == null) return;
            state.props.CuttingGraftChance = state.CuttingGraftChance;
            state.props.CuttingRootingChance = state.CuttingRootingChance;
        }



        [HarmonyPrefix]
        [HarmonyPatch("GetBlockInfo")]
        public static void GetBlockInfoPrefix(FruitTreeGrowingBranchBH __instance, out FruitTreeGrowingState __state, BlockFruitTreeBranch ___branchBlock)
        {
            CommonPrefix(__instance, out __state, ___branchBlock);
        }

        [HarmonyPostfix]
        [HarmonyPatch("GetBlockInfo")]
        public static void GetBlockInfoPostfix(FruitTreeGrowingState __state)
        {
            CommonPostfix(__state);
        }

        [HarmonyPrefix]
        [HarmonyPatch("TryGrow")]
        public static void TryGrowPrefix(FruitTreeGrowingBranchBH __instance, out FruitTreeGrowingState __state, BlockFruitTreeBranch ___branchBlock)
        {
            CommonPrefix(__instance, out __state, ___branchBlock);
        }

        [HarmonyPostfix]
        [HarmonyPatch("TryGrow")]
        public static void TryGrowPostfix(FruitTreeGrowingBranchBH __instance, FruitTreeGrowingState __state)
        {
            CommonPostfix(__state);
        }

    }

}
