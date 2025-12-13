using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace GloomeClasses.src.EntityBehaviors {
    public class DreadBehavior(Entity entity) : EntityBehavior(entity) {

        public override string PropertyName() => "gcDreadTraitBehavior";

        public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage) {
            if (damageSource.GetCauseEntity() == null) {
                return;
            }
            if (damageSource.GetCauseEntity() is EntityPlayer byPlayer)
            {
                damage *= byPlayer.Stats.GetBlended("rustedDamage");
            }
        }
    }
}