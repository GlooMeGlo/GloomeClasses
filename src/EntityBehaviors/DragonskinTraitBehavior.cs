using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace GloomeClasses.src.EntityBehaviors {
    public class DragonskinTraitBehavior(Entity entity) : EntityBehavior(entity) {

        public override string PropertyName() => "gcDragonskinTraitBehavior";

        public void HandleFireDamage(DamageSource damageSource, ref float damage) {
            var dragonskinBlended = entity.Stats.GetBlended("fireDamage");
            if (dragonskinBlended < 1) {
                entity.OnFireBeginTotalMs = 0;
                entity.IsOnFire = false;
                if (damageSource.Type == EnumDamageType.Fire) {
                    damage *= dragonskinBlended;
                }
            }
        }
    }
}
