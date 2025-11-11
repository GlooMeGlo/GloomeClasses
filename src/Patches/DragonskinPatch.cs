using GloomeClasses.src.EntityBehaviors;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace GloomeClasses.src.Patches {

    [HarmonyPatch(typeof(EntityAgent))]
    [HarmonyPatchCategory(GloomeClassesModSystem.DragonskinPatchCategory)]
    public class DragonskinPatch {

        [HarmonyPrefix]
        [HarmonyPatch(nameof(EntityAgent.ReceiveDamage))]
        public static void PlayerRecieveDamagePrefix(DamageSource damageSource, ref float damage, EntityAgent __instance) {
            if (__instance.HasBehavior<DragonskinTraitBehavior>()) {
                var dragonskinBehavior = __instance.GetBehavior<DragonskinTraitBehavior>();
                dragonskinBehavior.HandleFireDamage(damageSource, ref damage);
            }
        }
    }
}
