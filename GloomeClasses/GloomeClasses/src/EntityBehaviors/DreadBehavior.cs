using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace GloomeClasses.src.EntityBehaviors {
    public class DreadBehavior : EntityBehavior {

        public override string PropertyName() => "gcDreadTraitBehavior";
        
        public DreadBehavior(Entity entity) : base(entity) {
          
        }

        public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage) {
            if (damageSource.GetCauseEntity() == null) {
                return;
            }
            EntityPlayer byPlayer = damageSource.GetCauseEntity() as EntityPlayer;
            if (byPlayer != null) {
                damage *= byPlayer.Stats.GetBlended("rustedDamage");
            }
        }
    }
}