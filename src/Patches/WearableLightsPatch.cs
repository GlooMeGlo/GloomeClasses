using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace GloomeClasses.src.Patches {

    [HarmonyPatch(typeof(EntityPlayer))]
    [HarmonyPatchCategory(GloomeClassesModSystem.WearableLightsPatchesCategory)]
    public class WearableLightsPatch { //This patch is modeled after the patch that Wearable Lights mod has! Thank you!

        [HarmonyPostfix]
        [HarmonyPatch("LightHsv", MethodType.Getter)]
        public static void PlayerLightHsvPostfixPatch(EntityPlayer __instance, ref byte[] __result) {
            try {
                if (__instance == null || !__instance.Alive || __instance.Player == null || __instance.Player.WorldData.CurrentGameMode == EnumGameMode.Spectator) {
                    return;
                }
                ApplyWearableLights(__instance, ref __result);
            } catch {

            }
        }

        public static void ApplyWearableLights(EntityPlayer player, ref byte[] lightBytes) {
            IInventory backpack = player.Player.InventoryManager.GetInventory(GlobalConstants.backpackInvClassName + "-" + player.PlayerUID);
            if (backpack != null) {
                for (int i = 0; i < backpack.Count; i++) {
                    var backpackSlot = backpack[i];

                    if (backpackSlot is not ItemSlotBackpack || backpackSlot.Empty) {
                        continue;
                    }

                    byte[] backpackHsv = backpackSlot.Itemstack.Collectible.LightHsv;

                    if (backpackHsv == null) {
                        continue;
                    }

                    if (lightBytes == null) {
                        lightBytes = backpackHsv;
                        continue;
                    }

                    float totalVal = lightBytes[2] + backpackHsv[2];
                    float number = backpackHsv[2] / totalVal;

                    // Calculate weighted average of light values
                    lightBytes = [
                        (byte)(backpackHsv[0] * number + lightBytes[0] * (1 - number)),
                        (byte)(backpackHsv[1] * number + lightBytes[1] * (1 - number)),
                        Math.Max(backpackHsv[2], lightBytes[2])
                    ];
                }
            }
        }
    }
}
