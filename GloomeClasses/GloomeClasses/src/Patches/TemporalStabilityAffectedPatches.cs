using GloomeClasses.src.EntityBehaviors;
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
using Vintagestory.GameContent;

namespace GloomeClasses.src.Patches {

    [HarmonyPatch(typeof(EntityBehaviorTemporalStabilityAffected))]
    [HarmonyPatchCategory(GloomeClassesModSystem.TemporalStabilityAffectedPatchesCategory)]
    public class TemporalStabilityAffectedPatches {

        [HarmonyTranspiler]
        [HarmonyPatch(nameof(EntityBehaviorTemporalStabilityAffected.OnGameTick))]
        private static IEnumerable<CodeInstruction> OnGameTickTemporalTraitPatch(IEnumerable<CodeInstruction> instructions) {
            var codes = new List<CodeInstruction>(instructions);

            var colorOverlayMethodInfo = AccessTools.Method(typeof(ColorUtil), "ColorOverlay", new Type[] { typeof(int), typeof(int), typeof(float) });
            int indexOfColorOverlayCall = -1;
            int countFields = 0;
            int indexOfLoadFieldEntity = -1;

            for (int i = 0; i < codes.Count(); i++) {
                if (countFields < 3 && codes[i].opcode == OpCodes.Ldfld) {
                    countFields++;
                    if (countFields == 3) {
                        indexOfLoadFieldEntity = i;
                    }
                    continue;
                }

                if (countFields == 3 && codes[i].opcode == OpCodes.Call && (MethodInfo)codes[i].operand == colorOverlayMethodInfo) {
                    indexOfColorOverlayCall = i;
                    break;
                }
            }

            if (indexOfColorOverlayCall >= 0 && indexOfLoadFieldEntity >= 0) {
                var addCallToTraitHandle = new List<CodeInstruction>() {
                    CodeInstruction.LoadArgument(0),
                    codes[indexOfLoadFieldEntity].Clone(),
                    CodeInstruction.LoadLocal(1),
                    CodeInstruction.Call(typeof(TemporalStabilityAffectedPatches), "CheckForAndCallTemporalStabilityBehavior", new Type[] { typeof(EntityAgent), typeof(double) }),
                    CodeInstruction.StoreLocal(1)
                };

                codes.InsertRange(indexOfColorOverlayCall + 2, addCallToTraitHandle);
            } else {
                GloomeClassesModSystem.Logger.Error("Something went wrong patching OnGameTick in EntityBehaviorTemporalStabilityAffected. Will not patch anything. More details to follow:");
                if (indexOfColorOverlayCall < 0) {
                    GloomeClassesModSystem.Logger.Error("Could not find the ColorOverlay call.");
                }
                if (indexOfLoadFieldEntity < 0) {
                    GloomeClassesModSystem.Logger.Error("Could not find the LoadField Entity call.");
                }
            }

            return codes.AsEnumerable();
        }

        public static double CheckForAndCallTemporalStabilityBehavior(EntityAgent entity, double hereStability) {
            if (entity is EntityPlayer) {
                if (entity.HasBehavior<TemporalStabilityTraitBehavior>()) {
                    var tempStabTraits = entity.GetBehavior<TemporalStabilityTraitBehavior>();
                    return tempStabTraits.HandleTraits(hereStability);
                }
            }

            return hereStability;
        }
    }
}
