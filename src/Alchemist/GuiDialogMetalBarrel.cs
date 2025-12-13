using Cairo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GloomeClasses.src.Alchemist {

    public class GuiDialogMetalBarrel : GuiDialogBlockEntity {

        private const double MaxInteractionDistance = 15.0;
        private long distanceCheckTimerId;
        private EnumPosFlag screenPos;
        private ElementBounds inputSlotBounds;
        protected override double FloatyDialogPosition => 0.6;
        protected override double FloatyDialogAlign => 0.8;
        public override double DrawOrder => 0.2;

        public GuiDialogMetalBarrel(string dialogTitle, InventoryBase inventory, BlockPos blockEntityPos, ICoreClientAPI capi) : base(dialogTitle, inventory, blockEntityPos, capi) {
            _ = base.IsDuplicate;
        }

        private void SetupDialog() {
            ElementBounds elementBounds = ElementBounds.Fixed(0.0, 30.0, 150.0, 200.0);
            ElementBounds elementBounds2 = ElementBounds.Fixed(170.0, 30.0, 150.0, 200.0);
            inputSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 30.0, 1, 1);
            inputSlotBounds.fixedHeight += 10.0;
            _ = inputSlotBounds.fixedHeight;
            _ = inputSlotBounds.fixedY;
            ElementBounds elementBounds3 = ElementBounds.Fixed(100.0, 30.0, 40.0, 200.0);
            ElementBounds elementBounds4 = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            elementBounds4.BothSizing = ElementSizing.FitToChildren;
            elementBounds4.WithChildren(elementBounds, elementBounds2);
            ElementBounds bounds = ElementStdBounds.AutosizedMainDialog.WithFixedAlignmentOffset(IsRight(screenPos) ? (0.0 - GuiStyle.DialogToScreenPadding) : GuiStyle.DialogToScreenPadding, 0.0).WithAlignment(IsRight(screenPos) ? EnumDialogArea.RightMiddle : EnumDialogArea.LeftMiddle);
            base.SingleComposer = capi.Gui.CreateCompo("blockentitymetalbarrel" + base.BlockEntityPosition, bounds).AddShadedDialogBG(elementBounds4).AddDialogTitleBar(DialogTitle, OnTitleBarClose)
                .BeginChildElements(elementBounds4)
                .AddItemSlotGrid(base.Inventory, SendInvPacket, 1, new int[1], inputSlotBounds, "inputSlot")
                .AddSmallButton(Lang.Get("barrel-seal"), OnSealClick, ElementBounds.Fixed(0.0, 100.0, 80.0, 25.0))
                .AddInset(elementBounds3.ForkBoundingParent(2.0, 2.0, 2.0, 2.0), 2)
                .AddDynamicCustomDraw(elementBounds3, FullnessMeterDraw, "liquidBar")
                .AddDynamicText(GetContentsText(), CairoFont.WhiteDetailText(), elementBounds2, "contentText")
                .EndChildElements()
                .Compose();
        }

        private string GetContentsText() {
            string text = Lang.Get("Contents:");
            if (base.Inventory[0].Empty && base.Inventory[1].Empty) {
                text = text + "\n" + Lang.Get("nobarrelcontents");
            } else {
                if (!base.Inventory[1].Empty) {
                    ItemStack itemstack = base.Inventory[1].Itemstack;
                    WaterTightContainableProps containableProps = BlockLiquidContainerBase.GetContainableProps(itemstack);
                    if (containableProps != null) {
                        string text2 = Lang.Get(itemstack.Collectible.Code.Domain + ":incontainer-" + itemstack.Class.ToString().ToLowerInvariant() + "-" + itemstack.Collectible.Code.Path);
                        text = text + "\n" + Lang.Get((containableProps.MaxStackSize > 0) ? "barrelcontents-items" : "barrelcontents-liquid", (float)itemstack.StackSize / containableProps.ItemsPerLitre, text2);
                    } else {
                        text = text + "\n" + Lang.Get("barrelcontents-items", itemstack.StackSize, itemstack.GetName());
                    }
                }

                if (!base.Inventory[0].Empty) {
                    ItemStack itemstack2 = base.Inventory[0].Itemstack;
                    text = text + "\n" + Lang.Get("barrelcontents-items", itemstack2.StackSize, itemstack2.GetName());
                }

                if (capi.World.BlockAccessor.GetBlockEntity(base.BlockEntityPosition) is BlockEntityMetalBarrel blockEntityBarrel && blockEntityBarrel.CurrentRecipe != null) {
                    ItemStack resolvedItemstack = blockEntityBarrel.CurrentRecipe.Output.ResolvedItemstack;
                    WaterTightContainableProps containableProps2 = BlockLiquidContainerBase.GetContainableProps(resolvedItemstack);
                    string text3 = ((blockEntityBarrel.CurrentRecipe.SealHours > 24.0) ? Lang.Get("{0} days", Math.Round(blockEntityBarrel.CurrentRecipe.SealHours / (double)capi.World.Calendar.HoursPerDay, 1)) : Lang.Get("{0} hours", blockEntityBarrel.CurrentRecipe.SealHours));
                    if (containableProps2 != null) {
                        string text4 = Lang.Get(resolvedItemstack.Collectible.Code.Domain + ":incontainer-" + resolvedItemstack.Class.ToString().ToLowerInvariant() + "-" + resolvedItemstack.Collectible.Code.Path);
                        float num = (float)blockEntityBarrel.CurrentOutSize / containableProps2.ItemsPerLitre;
                        text = text + "\n\n" + Lang.Get("Will turn into {0} litres of {1} after {2} of sealing.", num, text4, text3);
                    } else {
                        text = text + "\n\n" + Lang.Get("Will turn into {0}x {1} after {2} of sealing.", blockEntityBarrel.CurrentOutSize, resolvedItemstack.GetName(), text3);
                    }
                }
            }

            return text;
        }

        public void UpdateContents() {
            base.SingleComposer.GetCustomDraw("liquidBar").Redraw();
            base.SingleComposer.GetDynamicText("contentText").SetNewText(GetContentsText());
        }

        private void FullnessMeterDraw(Context ctx, ImageSurface surface, ElementBounds currentBounds) {
            ItemSlot itemSlot = base.Inventory[1];
            if (!itemSlot.Empty) {
                if (capi.World.BlockAccessor.GetBlockEntity(base.BlockEntityPosition) is not BlockEntityMetalBarrel obj) return; // Block entity no longer exists (chunk unloaded)

                float num = 1f;
                int num2 = obj.CapacityLitres;
                WaterTightContainableProps containableProps = BlockLiquidContainerBase.GetContainableProps(itemSlot.Itemstack);
                if (containableProps != null) {
                    num = containableProps.ItemsPerLitre;
                    num2 = Math.Max(num2, containableProps.MaxStackSize);
                }

                float num3 = (float)itemSlot.StackSize / num / (float)num2;
                double num4 = (double)(1f - num3) * currentBounds.InnerHeight;
                ctx.Rectangle(0.0, num4, currentBounds.InnerWidth, currentBounds.InnerHeight - num4);
                CompositeTexture compositeTexture = containableProps?.Texture ?? itemSlot.Itemstack.Collectible.Attributes?["inContainerTexture"].AsObject<CompositeTexture>(null, itemSlot.Itemstack.Collectible.Code.Domain);
                if (compositeTexture != null) {
                    ctx.Save();
                    Matrix matrix = ctx.Matrix;
                    matrix.Scale(GuiElement.scaled(3.0), GuiElement.scaled(3.0));
                    ctx.Matrix = matrix;
                    AssetLocation textureLoc = compositeTexture.Base.Clone().WithPathAppendixOnce(".png");
                    GuiElement.fillWithPattern(capi, ctx, textureLoc, nearestScalingFiler: true, preserve: false, compositeTexture.Alpha);
                    ctx.Restore();
                }
            }
        }

        private bool OnSealClick() {
            if (!(capi.World.BlockAccessor.GetBlockEntity(base.BlockEntityPosition) is BlockEntityMetalBarrel { Sealed: false } blockEntityBarrel)) {
                return true;
            }

            if (!blockEntityBarrel.CanSeal) {
                return true;
            }

            blockEntityBarrel.SealBarrel();
            capi.Network.SendBlockEntityPacket(base.BlockEntityPosition, 1337);
            capi.World.PlaySoundAt(new AssetLocation("sounds/player/seal"), base.BlockEntityPosition, 0.4);
            TryClose();
            return true;
        }

        private void SendInvPacket(object packet) {
            capi.Network.SendBlockEntityPacket(base.BlockEntityPosition.X, base.BlockEntityPosition.Y, base.BlockEntityPosition.Z, packet);
        }

        private void OnTitleBarClose() {
            TryClose();
        }

        private void CheckDistance(float dt) {
            if (capi?.World?.Player?.Entity == null) {
                TryClose();
                return;
            }

            Vec3d playerPos = capi.World.Player.Entity.Pos.XYZ;
            Vec3d blockPos = new(BlockEntityPosition.X + 0.5, BlockEntityPosition.Y + 0.5, BlockEntityPosition.Z + 0.5);
            double distance = playerPos.DistanceTo(blockPos);

            if (distance > MaxInteractionDistance) {
                TryClose();
            }
        }

        public override void OnGuiOpened() {
            base.OnGuiOpened();
            screenPos = GetFreePos("smallblockgui");
            OccupyPos("smallblockgui", screenPos);
            SetupDialog();

            // register distance checker (runs every 500ms)
            distanceCheckTimerId = capi.Event.RegisterGameTickListener(CheckDistance, 500);
        }

        public override void OnGuiClosed() {
            base.SingleComposer.GetSlotGrid("inputSlot").OnGuiClosed(capi);
            base.OnGuiClosed();
            FreePos("smallblockgui", screenPos);

            // unregister distance checker
            capi.Event.UnregisterGameTickListener(distanceCheckTimerId);
        }
    }
}
