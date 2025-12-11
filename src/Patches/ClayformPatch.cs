using HarmonyLib;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace GloomeClasses.src.Patches {

    [HarmonyPatch(typeof(ItemClay))]
    [HarmonyPatchCategory(GloomeClassesModSystem.ClayformingPatchesCategory)]
    public class ClayformPatch {

        [HarmonyPrefix]
        [HarmonyPatch(nameof(ItemClay.OnHeldInteractStop))]
        private static bool AdditionalClayVoxelsStatPrefix(ItemClay __instance, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel) {
            if (blockSel == null || byEntity.World.BlockAccessor.GetBlock(blockSel.Position) is not BlockClayForm || byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) is not BlockEntityClayForm blockEntityClayForm) {
                return true;
            }

            IPlayer player = null;
            if (byEntity is EntityPlayer player1) {
                player = byEntity.World.PlayerByUid(player1.PlayerUID);
            }

            var bonusPointsStats = player.Entity.Stats.Where(stats => stats.Key == GloomeClassesModSystem.BonusClayVoxelsStat);
            if (!bonusPointsStats.Any()) {
                return true;
            }

            var bonusPoints = bonusPointsStats.First().Value;
            if (player != null && byEntity.World.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.Use)) {
                if (blockEntityClayForm.AvailableVoxels <= 0) {
                    slot.TakeOut(1);
                    slot.MarkDirty();
                    blockEntityClayForm.AvailableVoxels += 25 + (int)(bonusPoints.GetBlended() - 1);
                }
            }

            return true;
        }
    }
}
