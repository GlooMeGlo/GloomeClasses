using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;
using static System.Net.Mime.MediaTypeNames;

namespace GloomeClasses
{
    public class DreadBehavior : EntityBehavior
    {
        public override string PropertyName() => "GC_ClassesPlayerBehavior";
        public DreadBehavior(Entity entity) : base(entity)
        {

        }

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);
            EntityBehaviorHealth behaviorHealth = (this.entity.GetBehavior("health")) as EntityBehaviorHealth;
            if ( behaviorHealth != null)
            {
                behaviorHealth.onDamaged += onHorrorDamage;
                base.entity.Api.Logger.Debug("DREADBEHAVIOR: SUCCESSFUL ADD");
            }


        }

        public float onHorrorDamage(float damage, DamageSource dmgSource)
        {
            if (dmgSource.CauseEntity == null)
            {
                return damage;
            }
            EntityPlayer byPlayer = dmgSource.SourceEntity as EntityPlayer ??
                dmgSource.CauseEntity as EntityPlayer;
            if (byPlayer != null)
            {
                damage *= byPlayer.Stats.GetBlended("horrorsDamage");

                byPlayer.Api.Logger.Debug("HorrorOnDamagePrint, Damage: {0}", damage);
            }


            return damage;
        }

        public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
        {
            //base.OnEntityReceiveDamage(damageSource, ref damage);

            if (damageSource.CauseEntity == null) {
            }
            EntityPlayer byPlayer = damageSource.SourceEntity as EntityPlayer ??
                damageSource.CauseEntity as EntityPlayer;
            if (byPlayer != null)
            {
                damage = byPlayer.Stats.GetBlended("horrorsDamage") * damage;

                byPlayer.Api.Logger.Debug("Horror_OnEntityReceivedDamagePrint, Damage: {0}", damage);
                
            }
            return;
        }


    }
}
