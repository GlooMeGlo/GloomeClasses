using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace GloomeClasses
{
    public class ClassesPlayerBehavior : EntityBehavior
    {
        private double oldStability;
        private float timeSinceUpdate;
        public override string PropertyName() => "GC_ClassesPlayerBehavior";

        protected EntityBehaviorTemporalStabilityAffected TemporalAffected => this.entity.GetBehavior<EntityBehaviorTemporalStabilityAffected>();

        public ClassesPlayerBehavior(Entity entity) : base(entity)
        {

            this.oldStability = TemporalAffected?.OwnStability ?? 1.0;

        }

        public override void OnGameTick(float deltaTime)
        {

            if (this.entity == null) return;

            timeSinceUpdate += deltaTime;

            if (timeSinceUpdate >= 1.0f)
            {
                TemporalEffects();

                this.timeSinceUpdate = 0.0f;
            }


        }


        protected void TemporalEffects()
        {

            double stability = TemporalAffected.OwnStability;

            bool sunlightAffected = (this.entity.World.BlockAccessor.GetLightLevel(this.entity.Pos.AsBlockPos, Vintagestory.API.Common.EnumLightLevelType.OnlySunLight)) > 9;
            double agoraphobia = this.entity.Stats.GetBlended("caveStabilityLoss");
            double stabilityModifier = 0;

            double change = oldStability - stability;

            if (change != 0.0f || agoraphobia == 0)
            {

                // Stability gain case
                if (change < 0.0) {

                    // Undo any increase occuring in sunlight for agoraphobia
                    stabilityModifier -= change * (1.0 - agoraphobia) * (sunlightAffected ? 1.0d : 0.0d);



                }
                // Stability loss case
                else if (change > 0.0)
                {
                    // No loss if not in sunlight for agoraphobia
                    stabilityModifier += change * (1.0 - agoraphobia) * (sunlightAffected?0.0d:1.0d);
                    
                


                }
                if (agoraphobia == 0)
                {
                    stabilityModifier += sunlightAffected?-0.002:0.002;
                }

               

            }
            TemporalAffected.OwnStability += stabilityModifier;
            oldStability = stability;
            entity.Api.Logger.Debug("GC, Player, Temporal, stability: {0}, old stability: {1}, mod: {2}, agoraphobia: {3}, sunlight: {4}", stability, oldStability, stabilityModifier, agoraphobia, sunlightAffected);

        }
    }
}
