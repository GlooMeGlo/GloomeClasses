using System;
using System.Collections.Generic;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GloomeClasses
{
    public class CropDropBonus : GenericDropBonusBehavior
    {
        public override string traitStat => "cropDropRate";

        public CropDropBonus(Block block) : base(block) { }
    }
}
