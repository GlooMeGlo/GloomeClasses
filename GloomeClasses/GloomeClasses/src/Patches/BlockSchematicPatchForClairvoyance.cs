using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

namespace GloomeClasses.src.Patches {

    [HarmonyPatch(typeof(BlockSchematic))]
    [HarmonyPatchCategory(GloomeClassesModSystem.BlockSchematicPatchCategory)]
    public class BlockSchematicPatchForClairvoyance {

        public static MethodBase TargetMethod() {
            var method = AccessTools.Method(typeof(BlockSchematic), "Place", new Type[5] { typeof(IBlockAccessor), typeof(IWorldAccessor), typeof(BlockPos), typeof(EnumReplaceMode), typeof(bool) });
            return method;
        }

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> BlockSchematicTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator) {
            var codes = new List<CodeInstruction>(instructions);

            int indexOfPlaceIncrement = -1;
            int stloc1Count = 0;

            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].opcode == OpCodes.Stloc_1) {
                    if (stloc1Count < 1) {
                        stloc1Count++;
                    } else if (stloc1Count >= 1) {
                        indexOfPlaceIncrement = i + 1;
                        break;
                    }
                }
            }

            var injectCallToTestForAndInitTranslocatorBE = new List<CodeInstruction> {
                CodeInstruction.LoadArgument(1),
                CodeInstruction.LoadArgument(2),
                CodeInstruction.LoadLocal(0),
                CodeInstruction.LoadLocal(11),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BlockSchematicPatchForClairvoyance), "TestAndInitTranslocatorBE", new Type[4] { typeof(IBlockAccessor), typeof(IWorldAccessor), typeof(BlockPos), typeof(AssetLocation) }))
            };

            if (indexOfPlaceIncrement > -1) {
                codes.InsertRange(indexOfPlaceIncrement, injectCallToTestForAndInitTranslocatorBE);
            } else {
                GloomeClassesModSystem.Logger.Error("Could not locate the incrementing of 'placed' in BlockSchematic.Place to inject after. Some Translocators placed by Schematics will not have BEs, and Clairvoyant will not fully function.");
            }

            return codes.AsEnumerable();
        }

        public static void TestAndInitTranslocatorBE(IBlockAccessor blockAccess, IWorldAccessor world, BlockPos curPos, AssetLocation blockCode) {
            GloomeClassesModSystem.Logger.Warning("Spawning a " + blockCode);
            if (blockAccess is IWorldGenBlockAccessor && blockCode.Path.Contains("statictranslocator-broken-")) {
                GloomeClassesModSystem.Logger.Warning("Found a broken translocator being placed in a Schematic!");
                var block = blockAccess.GetBlock(curPos);
                if (block != null && block.EntityClass != null) {
                    GloomeClassesModSystem.Logger.Warning("It has the entityClass of " + block.EntityClass);
                    blockAccess.SpawnBlockEntity(block.EntityClass, curPos);
                    var be = blockAccess.GetBlockEntity(curPos);
                    be.Initialize(world.Api);
                }
            }
        }
    }

    [HarmonyPatch(typeof(BlockSchematicStructure))]
    [HarmonyPatchCategory(GloomeClassesModSystem.BlockSchematicPatchCategory)]
    public class BlockSchematicStructurePatchesForClairvoyance {

        [HarmonyTranspiler]
        [HarmonyPatch(nameof(BlockSchematicStructure.PlaceReplacingBlocks))]
        public static IEnumerable<CodeInstruction> PlaceReplacingBlocksTranspiler(IEnumerable<CodeInstruction> instructions) {
            var codes = new List<CodeInstruction>(instructions);

            int indexOfPlaceIncrement = -1;
            int stloc1Count = 0;

            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].opcode == OpCodes.Stloc_1) {
                    if (stloc1Count < 1) {
                        stloc1Count++;
                    } else if (stloc1Count >= 1) {
                        indexOfPlaceIncrement = i + 1;
                        break;
                    }
                }
            }

            var injectCallToTestForAndInitTranslocatorBE = new List<CodeInstruction> {
                CodeInstruction.LoadArgument(1),
                CodeInstruction.LoadArgument(2),
                CodeInstruction.LoadLocal(0),
                CodeInstruction.LoadLocal(13),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BlockSchematicPatchForClairvoyance), "TestAndInitTranslocatorBE", new Type[4] { typeof(IBlockAccessor), typeof(IWorldAccessor), typeof(BlockPos), typeof(AssetLocation) }))
            };

            if (indexOfPlaceIncrement > -1) {
                codes.InsertRange(indexOfPlaceIncrement, injectCallToTestForAndInitTranslocatorBE);
            } else {
                GloomeClassesModSystem.Logger.Error("Could not locate the incrementing of 'placed' in BlockSchematicStructure.PlaceReplacingBlocks to inject after. Some Translocators placed by Schematics will not have BEs, and Clairvoyant will not fully function.");
            }

            return codes.AsEnumerable();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(nameof(BlockSchematicStructure.PlaceRespectingBlockLayers))]
        public static IEnumerable<CodeInstruction> PlaceRespectingBlockLayersTranspiler(IEnumerable<CodeInstruction> instructions) {
            var codes = new List<CodeInstruction>(instructions);



            return codes.AsEnumerable();
        }
    }
}
