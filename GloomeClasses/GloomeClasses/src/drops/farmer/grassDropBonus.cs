using System;
using System.Collections.Generic;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GloomeClasses
{
    public class GrassBonusBehavior : GenericDropBonusBehavior
    {
        public override string traitStat => "grassDropRate";

        public GrassBonusBehavior(Block block) : base(block) { }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropChanceMultiplier, ref EnumHandling handling)
        {
            if (byPlayer == null) return null;
            ItemStack[] grassdrops = new ItemStack[0];

            world.Api.Logger.Debug("GC, Grassdrop, dropChangeMultipler: {0}, statMult: {1}, product: {2}", dropChanceMultiplier, byPlayer.Entity.Stats.GetBlended(traitStat), byPlayer.Entity.Stats.GetBlended(traitStat) * dropChanceMultiplier);
            dropChanceMultiplier *= byPlayer.Entity.Stats.GetBlended(traitStat);
            return grassdrops;
        }
    }
}
