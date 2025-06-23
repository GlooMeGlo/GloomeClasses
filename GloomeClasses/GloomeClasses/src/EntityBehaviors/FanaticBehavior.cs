using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace GloomeClasses.src.EntityBehaviors {
    public class FanaticBehavior : EntityBehavior {

        public override string PropertyName() => "gcFanaticTraitBehavior";
        
        public FanaticBehavior(Entity entity) : base(entity) {
          
        }

        public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage) {
            if (damageSource.GetCauseEntity() == null || entity as EntityPlayer == null) {
                return;
            }

            Entity damageCause = damageSource.GetCauseEntity();
            EntityPlayer player = entity as EntityPlayer;
            if (damageCause.Attributes.HasAttribute("isMechanical") && damageCause.Attributes.GetAsBool("isMechanical")) {
                damage *= player.Stats.GetBlended("damageFromMechanicals");
            }
        }
    }
}