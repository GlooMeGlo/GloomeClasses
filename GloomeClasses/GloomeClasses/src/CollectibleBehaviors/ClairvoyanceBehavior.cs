using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace GloomeClasses.src.CollectibleBehaviors {

    public class ClairvoyanceBehavior : CollectibleBehavior {

        private bool divining = false;
        private const float timeToDivine = 2.0f;
        private const float maxRange = 150.0f;
        private const float midRange = 90.0f;
        private const float closeRange = 50.0f;
        private const float nearby = 20.0f;
        private const int softCapCharges = 30;
        private const int chargesPerGear = 10;
        private const string chargesAttribute = "skullCharges";

        public ClairvoyanceBehavior(CollectibleObject collObj) : base(collObj) {

        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling) {
            var player = (byEntity as EntityPlayer);
            TestSkullChargesAndInit(slot);

            if (player != null && firstEvent) {
                if (!player.LeftHandItemSlot.Empty && player.LeftHandItemSlot.Itemstack.Collectible.Code.Path == "gear-temporal") {
                    handHandling = EnumHandHandling.PreventDefault;
                    handling = EnumHandling.PreventSubsequent;
                    return;
                }
                string classcode = player.WatchedAttributes.GetString("characterClass");
                CharacterClass charclass = player.Api.ModLoader.GetModSystem<CharacterSystem>().characterClasses.FirstOrDefault(c => c.Code == classcode);
                if (charclass.Traits.Contains("clairvoyance")) {
                    handHandling = EnumHandHandling.PreventDefault;
                    handling = EnumHandling.PreventSubsequent;
                    if (player.World.Side == EnumAppSide.Server) {
                        player.World.PlaySoundAt(new AssetLocation("gloomeclasses:sounds/clairvoyance/breathe.ogg"), byEntity.Pos.X, byEntity.Pos.Y, byEntity.Pos.Z, null, true, 32f, 1f);
                    }
                    divining = true;
                    return;
                }
            }

            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling) {
            if (divining) {
                handling = EnumHandling.PreventSubsequent;
                return divining && secondsUsed < timeToDivine;
            } else if (!divining && !byEntity.LeftHandItemSlot.Empty && byEntity.LeftHandItemSlot.Itemstack.Collectible.Code.Path == "gear-temporal") {
                handling = EnumHandling.PreventSubsequent;
                return secondsUsed < timeToDivine;
            }

            return base.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling) {
            if (divining && secondsUsed >= timeToDivine - 0.1) { //If they were crafting, verify that the countdown is up, and if so, craft it (if there still is a valid offhand handle!)
                handling = EnumHandling.PreventDefault;
                //if (byEntity.World.Side.IsServer()) {
                    DivineTranslocator(slot, byEntity);
                //}
                divining = false;
                return;
            } else if (!divining && secondsUsed >= timeToDivine - 0.1 && !byEntity.LeftHandItemSlot.Empty && byEntity.LeftHandItemSlot.Itemstack.Collectible.Code.Path == "gear-temporal") {
                handling = EnumHandling.PreventDefault;
                if (byEntity.World.Side.IsServer()) {
                    RechargeSkullWithGear(slot, byEntity.LeftHandItemSlot, byEntity);
                }
                divining = false;
                return;
            }

            divining = false;
            base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
        }

        public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason, ref EnumHandling handled) {
            if (divining && secondsUsed >= timeToDivine - 0.1) {
                handled = EnumHandling.PreventSubsequent;
                //if (byEntity.World.Side.IsServer()) {
                    DivineTranslocator(slot, byEntity);
                //}
            } else if (!divining && secondsUsed >= timeToDivine - 0.1 && !byEntity.LeftHandItemSlot.Empty && byEntity.LeftHandItemSlot.Itemstack.Collectible.Code.Path == "gear-temporal") {
                handled = EnumHandling.PreventDefault;
                if (byEntity.World.Side.IsServer()) {
                    RechargeSkullWithGear(slot, byEntity.LeftHandItemSlot, byEntity);
                }
            }

            divining = false;
            return base.OnHeldInteractCancel(secondsUsed, slot, byEntity, blockSel, entitySel, cancelReason, ref handled);
        }

        public void DivineTranslocator(ItemSlot slot, EntityAgent byEntity) {
            if (slot != null && !slot.Empty) {
                var poireg = byEntity.Api.ModLoader.GetModSystem<POIRegistry>();
                var curCharge = slot.Itemstack.Attributes.GetInt(chargesAttribute);
                if (poireg != null && curCharge > 0) {
                    curCharge--;
                    slot.Itemstack.Attributes.SetInt(chargesAttribute, curCharge);
                    var nearPoi = poireg.GetNearestPoi(byEntity.Pos.XYZFloat.ToVec3d(), maxRange, (IPointOfInterest poi) => (poi.Type == "translocator"));
                    if (nearPoi != null) {
                        var distanceTo = byEntity.Pos.DistanceTo(nearPoi.Position);
                        if (distanceTo > midRange) {
                            if (byEntity.Api.Side.IsClient()) {
                                (byEntity.Api as ICoreClientAPI).ShowChatMessage(Lang.Get("divinationlongresult"));
                            }
                        } else if (distanceTo > closeRange) {
                            if (byEntity.Api.Side.IsClient()) {
                                (byEntity.Api as ICoreClientAPI).ShowChatMessage(Lang.Get("divinationmidresult"));
                            }
                        } else if (distanceTo > nearby) {
                            if (byEntity.Api.Side.IsClient()) {
                                (byEntity.Api as ICoreClientAPI).ShowChatMessage(Lang.Get("divinationcloseresult"));
                            }
                        } else {
                            if (byEntity.Api.Side.IsClient()) {
                                (byEntity.Api as ICoreClientAPI).ShowChatMessage(Lang.Get("divinationnearresult"));
                            }
                        }
                    } else {
                        if (byEntity.Api.Side.IsClient()) {
                            (byEntity.Api as ICoreClientAPI).ShowChatMessage(Lang.Get("divinationnoresult"));
                        }
                    }
                    slot.MarkDirty();
                } else {
                    if (curCharge <= 0) {
                        if (byEntity.Api.Side.IsClient()) {
                            (byEntity.Api as ICoreClientAPI).ShowChatMessage(Lang.Get("divinationnocharge"));
                        }
                    }
                }
            }
        }

        public void RechargeSkullWithGear(ItemSlot skullSlot, ItemSlot gearSlot, EntityAgent byEntity) {
            if (skullSlot != null && !skullSlot.Empty && gearSlot != null && !gearSlot.Empty) {
                var curCharge = skullSlot.Itemstack.Attributes.GetInt(chargesAttribute);
                if (curCharge < softCapCharges) {
                    skullSlot.Itemstack.Attributes.SetInt(chargesAttribute, curCharge + chargesPerGear);
                    gearSlot.TakeOut(1);
                    skullSlot.MarkDirty();
                    gearSlot.MarkDirty();
                }
            }
        }

        public void TestSkullChargesAndInit(ItemSlot skullSlot) {
            if (skullSlot != null && !skullSlot.Empty) {
                if (!skullSlot.Itemstack.Attributes.HasAttribute(chargesAttribute)) {
                    skullSlot.Itemstack.Attributes.SetInt(chargesAttribute, chargesPerGear);
                }
            }
        }
    }
}
