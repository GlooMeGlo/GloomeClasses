using System;
using System.Collections.Generic;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GloomeClasses
{
    public abstract class GenericDropBonusBehavior : BlockBehavior
    {
        public abstract string traitStat { get; }

        public GenericDropBonusBehavior(Block block) : base(block) { }

        public virtual List<ItemStack> GetDropsList(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropChanceMultiplier, ref EnumHandling handling)
        {

            world.Api.Logger.Debug("GC, GenericDrop, Start");
            if (byPlayer == null) return null;
            List<ItemStack> drops = new List<ItemStack>();
            if (this.traitStat.Length <= 0) { world.Api.Logger.Debug("GC, GenericDrop, RETURN, no trait"); return drops; }
            if (block.Drops.Length == 0) { world.Api.Logger.Debug("GC, GenericDrop, RETURN, no drops"); return drops; }

            world.Api.Logger.Debug("GC, GenericDrop, STAT: {0}", traitStat);
            float statMult = byPlayer.Entity.Stats.GetBlended(traitStat);
            if (statMult <= 0) { world.Api.Logger.Debug("GC, GenericDrop, No stat mult"); return drops; }

            handling = EnumHandling.PreventDefault;
            float dropMult = dropChanceMultiplier * statMult;
            //if (dropMult <= 1) { world.Api.Logger.Debug("GC, GenericDrop, Dropmult = 1, no additional drops"); return drops; }
            world.Api.Logger.Debug("GC, GenericDrop, dropMult: {0}, statMult: {1}, dropChanceMultiplier {2}",dropMult, statMult, dropChanceMultiplier);
            for (int index = 0; index < block.Drops.Length; index++)
            { 
                ItemStack drop = block.Drops[index].GetNextItemStack(dropMult);
                if (drop != null) drops.Add(drop);
            }
            world.Api.Logger.Debug("GC, GenericDrop, Successful mult");
            return drops;
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropChanceMultiplier, ref EnumHandling handling)
        {
            if (byPlayer == null) return null;
            List<ItemStack> PotentialDrops = this.GetDropsList(world, pos, byPlayer, dropChanceMultiplier, ref handling);

            if (PotentialDrops == null) { handling = EnumHandling.PassThrough; return null; }
            return PotentialDrops.ToArray();
        }
        
    }

    /*
    public abstract class SpecialDropBonus : BlockBehavior
    {
        public abstract string traitStatItem { get; }
        public abstract string specialDrop { get; }

        public SpecialDropBonus(Block block) : base(block) { }

        public virtual List<ItemStack> GetDropsList(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropChanceMultiplier, ref EnumHandling handling)
        {
            List<ItemStack> drops = new List<ItemStack>();
            if (this.traitStatItem.Length <= 0) return drops;
            if (block.Drops.Length == 0) return drops;

            float statMult = byPlayer.Entity.Stats.GetBlended(traitStatItem);
            if (statMult <= 0) return drops;

            handling = EnumHandling.PreventDefault;
            float dropMult = dropChanceMultiplier + statMult;

            for (int index = 0; index < block.Drops.Length; index++)
            {
                ItemStack drop = block.Drops[index].GetNextItemStack
.
                if (drop == null) drops.Add(drop);
            }
           
            return drops;
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropChanceMultiplier, ref EnumHandling handling)
        {
            return this.GetDropsList(world, pos, byPlayer, dropChanceMultiplier, ref handling).ToArray();
        }

    }*/
}
