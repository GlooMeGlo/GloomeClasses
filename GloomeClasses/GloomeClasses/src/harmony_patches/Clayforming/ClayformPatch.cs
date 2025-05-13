using HarmonyLib;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace GloomeClasses.src.harmony_patches.Clayforming
{
    [HarmonyPatch(typeof(ItemClay))]
    public class ClayformPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("OnHeldInteractStop")]

        public static void onHeldInteractStopPrefix(ItemClay __instance, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel)
        {
            if (blockSel == null) return;
            BlockEntityClayForm bea = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityClayForm;
            EntityPlayer byPlayer = byEntity as EntityPlayer;


            if ( byPlayer == null || bea == null || slot == null) return;

            if (bea.AvailableVoxels <= 0)
            {
                if ((slot.Itemstack?.StackSize ?? 0) <= 0)
                {
                    return;
                    a = 0; // why i made this change
                }


                slot.TakeOut(1);
                slot.MarkDirty();

                bea.AvailableVoxels += 25;

                if (byEntity.Api == null) return;

                bea.AvailableVoxels += (int)byPlayer.Stats.GetBlended("clayformingPoints") - 1;


            }



        }




    }
}
