using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GloomeClasses.src.BlockBehaviors {
    public class UnlikelyHarvestBlockBehavior(Block block) : BlockBehavior(block) {
        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropChanceMultiplier, ref EnumHandling handling) {
            if (byPlayer != null) {
                var flaxDropRateStats = byPlayer.Entity.Stats.Where(stats => stats.Key == GloomeClassesModSystem.FlaxRateStat);
                if (flaxDropRateStats.Count() > 0) {
                    var firstRate = flaxDropRateStats.First().Value;
                    if (world.Rand.NextDouble() <= firstRate.GetBlended() - 1) {
                        handling = EnumHandling.Handled;
                        return [new ItemStack(world.GetItem(new AssetLocation("game:flaxfibers")))];
                    }
                }
            }

            return base.GetDrops(world, pos, byPlayer, ref dropChanceMultiplier, ref handling);
        }
    }
}