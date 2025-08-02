using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GloomeClasses.src.Patches
{
    [HarmonyPatch(typeof(Block))]
    [HarmonyPatchCategory(GloomeClassesModSystem.StaticTranslocatorPatchesCategory)]
    public class StaticTranslocatorPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Block.ShouldReceiveServerGameTicks))]
        public static void CheckForPoiEntityPostfix(Block __instance, IWorldAccessor world, BlockPos pos)
        {
            if (__instance is not BlockStaticTranslocator) return;

            if (world.BlockAccessor.GetBlockEntity(pos) == null)
            {
                world.BlockAccessor.SpawnBlockEntity("POITrackerDummyBlockEntity", pos);
            }
        }
    }
}
