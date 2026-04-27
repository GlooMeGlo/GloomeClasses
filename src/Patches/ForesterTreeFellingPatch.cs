using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GloomeClasses.src.Patches {

    // applies the forester logDropRate bonus only when felling actual trees
    // this prevents the exploit where placing logs and breaking them would give bonus drops
    // the bonus is applied in the tree felling loop (where FindTree found connected tree blocks)
    // single/isolated logs don't trigger tree felling, so they won't get the bonus
    [HarmonyPatch(typeof(ItemAxe))]
    [HarmonyPatchCategory(GloomeClassesModSystem.ForesterPatchCategory)]
    public class ForesterTreeFellingPatch {

        // helper method called by transpiler to adjust drop multiplier for logs
        public static float GetLogDropMultiplier(float baseMul, IPlayer player, bool isLog) {
            if (!isLog || player == null) {
                return baseMul;
            }

            // get the forester bonus from player stats
            float logDropRate = player.Entity.Stats.GetBlended("logDropRate");
            return baseMul * logDropRate;
        }

        [HarmonyTranspiler]
        [HarmonyPatch("OnBlockBrokenWith")]
        public static IEnumerable<CodeInstruction> TreeFellingDropBonusTranspiler(IEnumerable<CodeInstruction> instructions) {
            var codes = new List<CodeInstruction>(instructions);

            // find the BreakBlock call in the tree felling while loop
            // pattern: callvirt IBlockAccessor.BreakBlock(BlockPos, IPlayer, float)
            var breakBlockMethod = AccessTools.Method(
                typeof(IBlockAccessor),
                nameof(IBlockAccessor.BreakBlock),
                [typeof(BlockPos), typeof(IPlayer), typeof(float)]
            );

            var getLogDropMultiplierMethod = AccessTools.Method(
                typeof(ForesterTreeFellingPatch),
                nameof(GetLogDropMultiplier),
                [typeof(float), typeof(IPlayer), typeof(bool)]
            );

            // find the index of BreakBlock call
            int breakBlockIndex = -1;
            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].Calls(breakBlockMethod)) {
                    breakBlockIndex = i;
                    break;
                }
            }

            if (breakBlockIndex == -1) {
                GloomeClassesModSystem.Logger.Error("ForesterTreeFellingPatch: Could not find BreakBlock call in ItemAxe.OnBlockBrokenWith");
                return codes.AsEnumerable();
            }

            // find where isLog is stored (stloc for bool after BlockMaterial == Wood check)
            // and where byPlayer is stored
            // we need these local variable indices to load them later

            // look backwards from BreakBlock to find the pattern
            // the stack before BreakBlock should be: [IBlockAccessor, BlockPos, IPlayer, float]
            // the float comes from the ternary: isLeaves ? leavesMul : (isBranchy ? leavesBranchyMul : 1)

            // find isLog local - look for "BlockMaterial" property access followed by comparison to Wood (4)
            int isLogLocalIndex = -1;
            int byPlayerLocalIndex = -1;

            for (int i = 0; i < breakBlockIndex; i++) {
                // look for: ldfld EnumBlockMaterial followed by ldc.i4.4 (Wood) and ceq, then stloc
                if (codes[i].opcode == OpCodes.Ldc_I4_4) {
                    // check if next is ceq and then stloc
                    if (i + 2 < codes.Count && codes[i + 1].opcode == OpCodes.Ceq) {
                        var stlocInstr = codes[i + 2];
                        if (stlocInstr.IsStloc()) {
                            isLogLocalIndex = GetLocalIndex(stlocInstr);
                            break;
                        }
                    }
                }
            }

            // find byPlayer - look for PlayerByUid call result being stored
            for (int i = 0; i < breakBlockIndex; i++) {
                if (codes[i].opcode == OpCodes.Callvirt) {
                    var method = codes[i].operand as MethodInfo;
                    if (method != null && method.Name == "PlayerByUid") {
                        // next should be stloc for byPlayer
                        if (i + 1 < codes.Count && codes[i + 1].IsStloc()) {
                            byPlayerLocalIndex = GetLocalIndex(codes[i + 1]);
                            break;
                        }
                    }
                }
            }

            if (isLogLocalIndex == -1) {
                GloomeClassesModSystem.Logger.Error("ForesterTreeFellingPatch: Could not find isLog local variable");
                return codes.AsEnumerable();
            }

            if (byPlayerLocalIndex == -1) {
                GloomeClassesModSystem.Logger.Error("ForesterTreeFellingPatch: Could not find byPlayer local variable");
                return codes.AsEnumerable();
            }

            // insert our helper call right before BreakBlock
            // stack at this point: [IBlockAccessor, BlockPos, IPlayer, float]
            // we need to replace the float with GetLogDropMultiplier(float, IPlayer, bool)
            // so we: load byPlayer, load isLog, call helper

            var insertInstructions = new List<CodeInstruction> {
                new(OpCodes.Ldloc, byPlayerLocalIndex),
                new(OpCodes.Ldloc, isLogLocalIndex),
                new(OpCodes.Call, getLogDropMultiplierMethod)
            };

            codes.InsertRange(breakBlockIndex, insertInstructions);

            GloomeClassesModSystem.Logger.VerboseDebug("ForesterTreeFellingPatch: Successfully patched ItemAxe.OnBlockBrokenWith");
            return codes.AsEnumerable();
        }

        private static int GetLocalIndex(CodeInstruction instruction) {
            if (instruction.opcode == OpCodes.Stloc_0 || instruction.opcode == OpCodes.Ldloc_0) return 0;
            if (instruction.opcode == OpCodes.Stloc_1 || instruction.opcode == OpCodes.Ldloc_1) return 1;
            if (instruction.opcode == OpCodes.Stloc_2 || instruction.opcode == OpCodes.Ldloc_2) return 2;
            if (instruction.opcode == OpCodes.Stloc_3 || instruction.opcode == OpCodes.Ldloc_3) return 3;
            if (instruction.opcode == OpCodes.Stloc_S || instruction.opcode == OpCodes.Ldloc_S) {
                return ((LocalBuilder)instruction.operand).LocalIndex;
            }
            if (instruction.opcode == OpCodes.Stloc || instruction.opcode == OpCodes.Ldloc) {
                return ((LocalBuilder)instruction.operand).LocalIndex;
            }
            return -1;
        }
    }
}
