using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace GloomeClasses.src.Alchemist {

    public class InventoryGassifier : InventoryGeneric {

        public ItemSlot FuelSlot => slots[0];

        public InventoryGassifier(ICoreAPI api, BlockPos pos = null) : base(1, "AlchemistGassifier", pos.ToString(), api, OnNewSlot) {
            Pos = pos;
        }

        private static ItemSlot OnNewSlot(int id, InventoryGeneric self) {
            return new ItemSlot((InventoryGassifier)self) {
                MaxSlotStackSize = 8
            };
        }

        public bool AddFuel(ItemSlot fromSlot) {
            if (FuelSlot == null || fromSlot.Empty) {
                return false;
            }

            var count = fromSlot.TryPutInto(Api.World, FuelSlot, quantity: 1);
            fromSlot.MarkDirty();
            if (count > 0) {
                return true;
            }
            return false;
        }

        public override void FromTreeAttributes(ITreeAttribute treeAttribute) {
            base.FromTreeAttributes(treeAttribute);
        }

        public override void ToTreeAttributes(ITreeAttribute invtree) {
            base.ToTreeAttributes(invtree);
        }
    }
}
