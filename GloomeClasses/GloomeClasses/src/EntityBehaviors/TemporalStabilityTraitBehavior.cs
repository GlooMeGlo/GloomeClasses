using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace GloomeClasses.src.EntityBehaviors {
    public class TemporalStabilityTraitBehavior : EntityBehavior {

        protected bool hasLocatedClass = false;
        protected float timeSinceLastUpdate = 0.0f;
        public bool hasClaustrophobia = false;
        public bool hasAgoraphobia = false;
        public bool hasShelteredStone = false;
        public bool hasNone = true;

        public const string ClaustrophobicCode = "claustrophobic";
        public const string AgoraphobiaCode = "agoraphobia";
        public const string ShelteredByStoneCode = "shelteredstone";

        protected EntityBehaviorTemporalStabilityAffected TemporalAffected => entity.GetBehavior<EntityBehaviorTemporalStabilityAffected>();

        public override string PropertyName() => "gcTemporalStabilityTraitBehavior";

        public TemporalStabilityTraitBehavior(Entity entity) : base(entity) {

        }

        public override void OnGameTick(float deltaTime) {
            if (hasLocatedClass || entity == null || entity is not EntityPlayer) {
                return;
            }

            timeSinceLastUpdate += deltaTime;

            if (timeSinceLastUpdate > 1.0f) { //Only tick once a second or so, this doesn't need to run EVERY tick, that would be incredibly excessive.
                timeSinceLastUpdate = 0.0f;

                string classcode = entity.WatchedAttributes.GetString("characterClass");
                CharacterClass charclass = entity.Api.ModLoader.GetModSystem<CharacterSystem>().characterClasses.FirstOrDefault(c => c.Code == classcode);
                if (charclass != null) {
                    if (charclass.Traits.Contains(ClaustrophobicCode)) {
                        hasClaustrophobia = true;
                    }
                    if (charclass.Traits.Contains(AgoraphobiaCode)) {
                        hasAgoraphobia = true;
                    }
                    if (charclass.Traits.Contains(ShelteredByStoneCode)) {
                        hasShelteredStone = true;
                    }

                    if (hasClaustrophobia || hasAgoraphobia || hasShelteredStone) {
                        hasNone = false; //This just might make the check a TINY bit quicker if it's only comparing a single bool for every 1s tick after this.
                    }
                    hasLocatedClass = true;
                }
            }
        }

        public double HandleTraits(double hereStability) {
            if (hasNone) {
                return hereStability;
            }

            BlockPos pos = entity.Pos.AsBlockPos;
            if (hasShelteredStone) {
                if (entity.World.BlockAccessor.GetLightLevel(pos, EnumLightLevelType.OnlySunLight) < 5) {
                    if (hereStability > 1.05f) {
                        return hereStability;
                    } else {
                        GloomeClassesModSystem.Logger.Warning("Ticking Sheltered Stone! Returning 1.05f.");
                        return 1.05f;
                    }
                }
            }

            if (hasAgoraphobia) {
                var room = entity.Api.ModLoader.GetModSystem<RoomRegistry>().GetRoomForPosition(pos);
                if (room != null) {
                    var surfaceLoss = entity.Stats.GetBlended("surfaceStabilityLoss");
                    var ret = (hereStability * surfaceLoss);
                    if (ret > surfaceLoss) {
                        ret = surfaceLoss;
                    }
                    GloomeClassesModSystem.Logger.Warning("Ticking Claustrophobia! Surfaceloss is " + surfaceLoss + ". Stability is " + hereStability + ". Ret is " + ret);
                    return ret;
                }
            }

            if (hasClaustrophobia) {
                if (entity.World.BlockAccessor.GetLightLevel(pos, EnumLightLevelType.OnlySunLight) < 5) {
                    var caveLoss = entity.Stats.GetBlended("caveStabilityLoss");
                    var ret = (hereStability * caveLoss);
                    if (ret > caveLoss) {
                        ret = caveLoss;
                    }
                    GloomeClassesModSystem.Logger.Warning("Ticking Claustrophobia! Caveloss is " + caveLoss + ". Stability is " + hereStability + ". Ret is " + ret);
                    return ret;
                }
            }

            return hereStability;
        }
    }
}
