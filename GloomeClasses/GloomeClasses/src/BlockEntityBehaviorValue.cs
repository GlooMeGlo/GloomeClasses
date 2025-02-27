using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace GloomeClasses
{
    public class BlockEntityBehaviorValue : BlockEntityBehavior
    {
        public float Value { get; set; }

        public BlockEntityBehaviorValue(BlockEntity blockentity) : base(blockentity)
        {
            Value = 0.0f;
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            Value = tree.GetFloat("value");
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetFloat("value", Value);
        }

    }
}
