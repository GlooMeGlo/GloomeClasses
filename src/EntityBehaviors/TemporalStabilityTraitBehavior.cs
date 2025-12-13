using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace GloomeClasses.src.EntityBehaviors {
    public class TemporalStabilityTraitBehavior(Entity entity) : EntityBehavior(entity) {

        protected bool hasLocatedClass = false;
        protected float timeSinceLastUpdate = 0.0f;
        public bool hasClaustrophobia = false;
        public bool hasAgoraphobia = false;
        public bool hasShelteredStone = false;
        public bool hasNone = true;
        public bool enabled;

        protected const double ShelteredByStoneGainVelocity = 0.002;
        protected const int SunLightLevelForInCave = 5;

        public const string ClaustrophobicCode = "claustrophobicgloo";
        public const string AgoraphobiaCode = "agoraphobia";
        public const string ShelteredByStoneCode = "shelteredstone";

        protected EntityBehaviorTemporalStabilityAffected TemporalAffected => entity.GetBehavior<EntityBehaviorTemporalStabilityAffected>();

        public override string PropertyName() => "gcTemporalStabilityTraitBehavior";

        public override void Initialize(EntityProperties properties, JsonObject attributes) {
            base.Initialize(properties, attributes);

            enabled = entity.Api.World.Config.GetBool("temporalStability", true);
        }

        public override void OnGameTick(float deltaTime) {
            if (!enabled || entity == null || entity is not EntityPlayer) {
                return;
            }

            if (entity.World.PlayerByUid(((EntityPlayer)entity).PlayerUID) is IServerPlayer serverPlayer && serverPlayer.ConnectionState != EnumClientState.Playing) {
                return;
            }

            if (!hasNone) {
                HandleTraits(deltaTime);
            }

            if (hasLocatedClass) {
                return;
            }

            timeSinceLastUpdate += deltaTime;

            if (timeSinceLastUpdate > 1.0f) { //Only tick once a second or so, this doesn't need to run EVERY tick, that would be incredibly excessive.
                timeSinceLastUpdate = 0.0f;

                if (!hasLocatedClass) {
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
        }

        public void HandleTraits(float deltaTime) {
            BlockPos pos = entity.Pos.AsBlockPos;
            var tempStabVelocity = TemporalAffected.TempStabChangeVelocity;

            if (hasShelteredStone) {
                if (entity.World.BlockAccessor.GetLightLevel(pos, EnumLightLevelType.OnlySunLight) < SunLightLevelForInCave) {
                    if (tempStabVelocity > ShelteredByStoneGainVelocity) {
                        return;
                    } else {
                        TemporalAffected.TempStabChangeVelocity = ShelteredByStoneGainVelocity;
                        return;
                    }
                }
            }

            if (hasAgoraphobia) {
                var room = entity.Api.ModLoader.GetModSystem<RoomRegistry>().GetRoomForPosition(pos);
                
                if (room == null || !(room.ExitCount == 0 || room.SkylightCount < room.NonSkylightCount)) {
                    var surfaceLoss = (double)entity.Stats.GetBlended("surfaceStabilityLoss") - 1; //The -1 should return the raw value.
                    if (tempStabVelocity < surfaceLoss) {
                        surfaceLoss = tempStabVelocity;
                    }

                    TemporalAffected.TempStabChangeVelocity = surfaceLoss;
                    return;
                }
            }

            if (hasClaustrophobia) {
                if (entity.World.BlockAccessor.GetLightLevel(pos, EnumLightLevelType.OnlySunLight) < SunLightLevelForInCave && tempStabVelocity < 0) {
                    var caveLoss = entity.Stats.GetBlended("caveStabilityLoss");
                    TemporalAffected.TempStabChangeVelocity = (tempStabVelocity * caveLoss);
                    return;
                }
            }
        }
    }
}
