using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace GloomeClasses.src.CollectibleBehaviors {

    public class HealsHackedProps {
        public int healthRestored = 1;
        public bool corruptedHealer = false;
    }

    public class HealHackedLocustsBehavior(CollectibleObject collObj) : CollectibleBehavior(collObj) {

        private HealsHackedProps properties = new();

        public const string LocustLoverCode = "locustlover";

        public override void Initialize(JsonObject properties) {
            base.Initialize(properties);
            this.properties = properties.AsObject<HealsHackedProps>();
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling) {
            if (byEntity is EntityPlayer entPlayer && entitySel != null && entitySel.Entity != null && entitySel.Entity is EntityLocust && entitySel.Entity.Properties.Variant.ContainsKey("state") && slot.StackSize > 0)
            { //The only Locusts that have a 'State' variant are the hacked ones!
                string classcode = entPlayer.WatchedAttributes.GetString("characterClass");
                CharacterClass charclass = entPlayer.Api.ModLoader.GetModSystem<CharacterSystem>().characterClasses.FirstOrDefault(c => c.Code == classcode);
                var hasLocustLover = charclass != null && charclass.Traits.Contains(LocustLoverCode);

                if (hasLocustLover && entitySel.Entity.Properties.Variant.TryGetValue("type", out string hackedType))
                {
                    if (properties.corruptedHealer == false && hackedType == "bronze")
                    {
                        var locustHealth = entitySel.Entity.GetBehavior<EntityBehaviorHealth>();

                        if (entPlayer.Api.Side.IsServer() && (locustHealth == null || locustHealth.Health >= locustHealth.MaxHealth))
                        {
                            return;
                        }

                        handHandling = EnumHandHandling.PreventDefault;
                        handling = EnumHandling.PreventSubsequent;

                        if (byEntity.World.Side == EnumAppSide.Server)
                        {
                            byEntity.World.PlaySoundAt(new AssetLocation("sounds/block/anvil2.ogg"), entitySel.Entity.Pos.X, entitySel.Entity.Pos.Y, entitySel.Entity.Pos.Z, null, true, 32f, 1f);
                        }
                        else
                        {
                            return; //If it's the client, need to get the interaction back on the serverside before any of the healing and handling can happen!
                        }

                        entitySel.Entity.ReceiveDamage(new DamageSource()
                        {
                            Source = EnumDamageSource.Internal,
                            Type = EnumDamageType.Heal
                        }, properties.healthRestored);

                        slot.TakeOut(1);
                        slot.MarkDirty();

                        return;
                    }
                    else if (properties.corruptedHealer == true && hackedType != "bronze")
                    {
                        var locustHealth = entitySel.Entity.GetBehavior<EntityBehaviorHealth>();

                        if (entPlayer.Api.Side.IsServer() && (locustHealth == null || locustHealth.Health >= locustHealth.MaxHealth))
                        {
                            return;
                        }

                        handHandling = EnumHandHandling.PreventDefault;
                        handling = EnumHandling.PreventSubsequent;

                        if (byEntity.World.Side == EnumAppSide.Server)
                        {
                            byEntity.World.PlaySoundAt(new AssetLocation("sounds/block/anvil2.ogg"), entitySel.Entity.Pos.X, entitySel.Entity.Pos.Y, entitySel.Entity.Pos.Z, null, true, 32f, 1f);
                        }
                        else
                        {
                            return; //If it's the client, need to get the interaction back on the serverside before any of the healing and handling can happen!
                        }

                        entitySel.Entity.ReceiveDamage(new DamageSource()
                        {
                            Source = EnumDamageSource.Internal,
                            Type = EnumDamageType.Heal
                        }, properties.healthRestored);

                        slot.TakeOut(1);
                        slot.MarkDirty();

                        return;
                    }
                }
            }

            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);
        }
    }
}
