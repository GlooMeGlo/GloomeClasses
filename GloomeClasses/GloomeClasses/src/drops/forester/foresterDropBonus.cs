using System;
using System.Collections.Generic;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GloomeClasses
{
    public class TreelogBonusBehavior : GenericDropBonusBehavior
    {
        public override string traitStat => "treelogDropRate";

        public TreelogBonusBehavior(Block block) : base(block) { }
    }


    public class LeavesBonusBehavior : GenericDropBonusBehavior
    {
        public override string traitStat => "saplingDropRate";
        private static string sticktraitStat = "stickDropRate";

        public LeavesBonusBehavior(Block block) : base(block) { }

        public override List<ItemStack> GetDropsList(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropChanceMultiplier, ref EnumHandling handling)
        {

            world.Api.Logger.Debug("GC, leavesDrop, Start");

            List<ItemStack> drops = new List<ItemStack>();
            if (byPlayer == null) return drops;
            if (this.traitStat.Length <= 0) { world.Api.Logger.Debug("GC, leavesdrop, RETURN, no trait"); return drops; }
            if (block.Drops.Length == 0) { world.Api.Logger.Debug("GC, leavesdrop, RETURN, no drops"); return drops; }

            float sapStatMult = byPlayer.Entity.Stats.GetBlended(traitStat);
            float stickStatMult = byPlayer.Entity.Stats.GetBlended(sticktraitStat);
            if (sapStatMult <= 0) { world.Api.Logger.Debug("GC, leavesdrop, No stat mult"); return drops; }
            if (stickStatMult <= 0) { world.Api.Logger.Debug("GC, leavesdrop, No stat mult"); return drops; }
            handling = EnumHandling.PreventDefault;
            float sapDropMult = dropChanceMultiplier * sapStatMult;
            world.Api.Logger.Debug("GC, GenericDrop, dropMult: {0}, statMult{1}, dropChanceMultiplier {2}", sapDropMult, sapStatMult, dropChanceMultiplier);

            ItemStack drop = block.Drops[0].GetNextItemStack(sapDropMult);


            if (drop != null) drops.Add(drop);


            if (block.Drops.Length > 1)
            {
                drop = block.Drops[1].GetNextItemStack(stickStatMult * dropChanceMultiplier);
                if (drop != null) drops.Add(drop);
            }

            for (int index = 2; index < block.Drops.Length; index++)
            {
                drop = block.Drops[index].GetNextItemStack(dropChanceMultiplier);
                if (drop != null) drops.Add(drop);
            }

            world.Api.Logger.Debug("GC, GenericDrop, Successful mult");
            return drops;
        }
    }

}
