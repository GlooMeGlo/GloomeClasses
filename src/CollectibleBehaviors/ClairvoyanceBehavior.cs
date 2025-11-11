using Cairo;
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
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GloomeClasses.src.CollectibleBehaviors {

    public class ClairvoyanceBehavior : CollectibleBehavior {

        private bool divining = false;
        private const float timeToDivine = 2.0f;
        private const float maxRange = 80.0f;
        private const float midRange = 60.0f;
        private const float closeRange = 30.0f;
        private const float nearby = 15.0f;
        private const int softCapCharges = 30;
        private const int chargesPerGear = 10;
        private const string chargesAttribute = "skullCharges";
        
        public ClairvoyanceBehavior(CollectibleObject collObj) : base(collObj) {

        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            var chargesInSkull = inSlot.Itemstack.Attributes.GetInt(chargesAttribute);
            if (chargesInSkull > 25) {
                dsc.AppendLine(Lang.Get("fullSkullCharge"));
            } else if (chargesInSkull > 15) {
                dsc.AppendLine(Lang.Get("midSkullCharge"));
            } else if (chargesInSkull > 5) {
                dsc.AppendLine(Lang.Get("lowSkullCharge"));
            } else if (chargesInSkull > 0) {
                dsc.AppendLine(Lang.Get("minimalSkullCharge"));
            } else {
                dsc.AppendLine(Lang.Get("noSkullCharge"));
            }
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling) {
            var player = (byEntity as EntityPlayer);
            TestSkullChargesAndInit(slot);

            if (player != null && firstEvent) {
                if (!player.LeftHandItemSlot.Empty && CheckForRechargeMaterial(player.LeftHandItemSlot.Itemstack.Collectible.Code.Path)) {
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
            } else if (!divining && !byEntity.LeftHandItemSlot.Empty && CheckForRechargeMaterial(byEntity.LeftHandItemSlot.Itemstack.Collectible.Code.Path)) {
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
            } else if (!divining && secondsUsed >= timeToDivine - 0.1 && !byEntity.LeftHandItemSlot.Empty && CheckForRechargeMaterial(byEntity.LeftHandItemSlot.Itemstack.Collectible.Code.Path)) {
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
            } else if (!divining && secondsUsed >= timeToDivine - 0.1 && !byEntity.LeftHandItemSlot.Empty && CheckForRechargeMaterial(byEntity.LeftHandItemSlot.Itemstack.Collectible.Code.Path)) {
                handled = EnumHandling.PreventDefault;
                if (byEntity.World.Side.IsServer()) {
                    RechargeSkullWithGear(slot, byEntity.LeftHandItemSlot, byEntity);
                }
            }

            divining = false;
            return base.OnHeldInteractCancel(secondsUsed, slot, byEntity, blockSel, entitySel, cancelReason, ref handled);
        }

        public void DivineTranslocator(ItemSlot slot, EntityAgent byEntity) {
            if (slot == null || slot.Empty) return;

            var poireg = byEntity.Api.ModLoader.GetModSystem<POIRegistry>();
            var curCharge = slot.Itemstack.Attributes.GetInt(chargesAttribute);

            if (poireg != null && curCharge > 0)
            {
                curCharge--;
                slot.Itemstack.Attributes.SetInt(chargesAttribute, curCharge);
                slot.MarkDirty();

                if (byEntity.Api.Side.IsClient())
                {
                    var nearPoi = poireg.GetNearestPoi(byEntity.Pos.XYZFloat.ToVec3d(), maxRange, (IPointOfInterest poi) => (poi.Type == "translocator"));
                    ShowDivinationResultChatMessage(byEntity.Api as ICoreClientAPI, nearPoi, byEntity.Pos.XYZ);
                }
            }
            else if (curCharge <= 0 && byEntity.Api.Side.IsClient())
            {
                (byEntity.Api as ICoreClientAPI).ShowChatMessage(Lang.Get("divinationnocharge"));
            }
        }

        public void ShowDivinationResultChatMessage(ICoreClientAPI capi, IPointOfInterest poi, Vec3d fromPosition)
        {
            if (poi == null)
            {
                capi.ShowChatMessage(Lang.Get("divinationnoresult"));
                return;
            }

            var translocator = capi.World.BlockAccessor.GetBlockEntity<BlockEntityStaticTranslocator>(poi.Position.AsBlockPos);
            if (translocator != null && translocator.FullyRepaired)
            {
                capi.ShowChatMessage(Lang.Get("divinationnoresult"));
                return;
            }

            double distanceTo = fromPosition.DistanceTo(poi.Position);
            string divinationMessage = distanceTo switch
            {
                > midRange => Lang.Get("divinationlongresult"),
                > closeRange => Lang.Get("divinationmidresult"),
                > nearby => Lang.Get("divinationcloseresult"),
                _ => Lang.Get("divinationnearresult")
            };

            capi.ShowChatMessage(divinationMessage);
        }

        public static bool CheckForRechargeMaterial(string itemPath) {
            if (itemPath == "gear-temporal" || itemPath == "primamateria" || itemPath == "gem-temporal") {
                return true;
            }

            return false;
        }

        public void RechargeSkullWithGear(ItemSlot skullSlot, ItemSlot gearSlot, EntityAgent byEntity) {
            if (skullSlot != null && !skullSlot.Empty && gearSlot != null && !gearSlot.Empty) {
                var curCharge = skullSlot.Itemstack.Attributes.GetInt(chargesAttribute);
                if (curCharge < softCapCharges) {
                    var toChargeAmount = chargesPerGear;
                    if (gearSlot.Itemstack.Collectible.Code.Path == "primamateria") {
                        toChargeAmount = (int)MathF.Round((float)toChargeAmount * 0.25f);
                    } else if (gearSlot.Itemstack.Collectible.Code.Path == "gem-temporal") {
                        toChargeAmount = (int)MathF.Round((float)toChargeAmount * 1.5f);
                    }

                    skullSlot.Itemstack.Attributes.SetInt(chargesAttribute, curCharge + toChargeAmount);
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
