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
        private float agphobStabLoss = 0.005f;
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
            if (TemporalAffected==null) return;

            double stability = TemporalAffected.OwnStability;

            bool sunlightAffected = (this.entity.World.BlockAccessor.GetLightLevel(this.entity.Pos.AsBlockPos, Vintagestory.API.Common.EnumLightLevelType.OnlySunLight)) > 9;
            double caveStabilityStat = this.entity.Stats.GetBlended("caveStabilityLoss");
            double stabilityModifier = 0;

            double change = oldStability - stability;

            if (caveStabilityStat == -1.0)
            {

                // Stability gain case
                if (change < 0.0) {

                    // Undo any increase occuring in sunlight for caveStabilityStat
                    stabilityModifier += (change + agphobStabLoss) *(sunlightAffected ? -1.0d : 1.0d);

                }
                // Stability loss case
                else if (change > 0.0)
                {
                    // No loss if not in sunlight for caveStabilityStat 
                    stabilityModifier += (sunlightAffected ? -1.0d : 1.0d)*(agphobStabLoss) + (sunlightAffected ? 0.0d : 1.0d)*change;
                }
                else if (change == 0)
                {
                    stabilityModifier += (sunlightAffected? -1.0d : 1.0d) * agphobStabLoss;
                }
            } else
            {

                // Stability gain case
                if (change < 0.0)
                {

                    // Undo any increase occuring in sunlight for caveStabilityStat
                    stabilityModifier += change * (1-caveStabilityStat) * (sunlightAffected ? 1.0d : -1.0d);

                }
                // Stability loss case
                else if (change > 0.0)
                {
                    // No loss if not in sunlight for caveStabilityStat 
                    stabilityModifier += change * (1-caveStabilityStat) * (sunlightAffected ? 1.0d : -1.0d);
                }
                else if (change == 0.0)
                {
                    stabilityModifier += (sunlightAffected ? 1.0d : -1.0d) * (1-caveStabilityStat) * agphobStabLoss;
                }

            }



            TemporalAffected.OwnStability += stabilityModifier;
            oldStability = stability;
            entity.Api.Logger.Debug("GC, Player, Temporal, stability: {0}, old stability: {1}, mod: {2}, caveStabilityStat: {3}, sunlight: {4}", stability, oldStability, stabilityModifier, caveStabilityStat, sunlightAffected);

        }
    }
}
