using System;
using System.Collections;
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
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace GloomeClasses.src.Alchemist {
    public class BlockMetalBarrel : BlockLiquidContainerBase {
        public override bool AllowHeldLiquidTransfer => false;
        public AssetLocation emptyShape { get; protected set; } = AssetLocation.Create("block/wood/barrel/empty");
        public AssetLocation sealedShape { get; protected set; } = AssetLocation.Create("block/wood/barrel/closed");
        public AssetLocation contentsShape { get; protected set; } = AssetLocation.Create("block/wood/barrel/contents");
        public AssetLocation opaqueLiquidContentsShape { get; protected set; } = AssetLocation.Create("block/wood/barrel/opaqueliquidcontents");
        public AssetLocation liquidContentsShape { get; protected set; } = AssetLocation.Create("block/wood/barrel/liquidcontents");

        public override int GetContainerSlotId(BlockPos pos) {
            return 1;
        }

        public override int GetContainerSlotId(ItemStack containerStack) {
            return 1;
        }

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo) {
            object value;
            Dictionary<string, MultiTextureMeshRef> dictionary2 = (Dictionary<string, MultiTextureMeshRef>)(capi.ObjectCache.TryGetValue("barrelMeshRefs" + Code, out value) ? (value as Dictionary<string, MultiTextureMeshRef>) : (capi.ObjectCache["barrelMeshRefs" + Code] = new Dictionary<string, MultiTextureMeshRef>()));
            ItemStack[] contents = GetContents(capi.World, itemstack);
            if (contents != null && contents.Length != 0) {
                bool @bool = itemstack.Attributes.GetBool("sealed");
                string barrelMeshkey = GetBarrelMeshkey(contents[0], (contents.Length > 1) ? contents[1] : null);
                if (!dictionary2.TryGetValue(barrelMeshkey, out var value2)) {
                    MeshData data = GenMesh(contents[0], (contents.Length > 1) ? contents[1] : null, @bool);
                    value2 = (dictionary2[barrelMeshkey] = capi.Render.UploadMultiTextureMesh(data));
                }

                renderinfo.ModelRef = value2;
            }
        }

        public string GetBarrelMeshkey(ItemStack contentStack, ItemStack liquidStack) {
            return string.Concat(contentStack?.StackSize + "x" + contentStack?.GetHashCode(), (liquidStack?.StackSize).ToString(), "x", (liquidStack?.GetHashCode()).ToString());
        }

        public override void OnUnloaded(ICoreAPI api) {
            if (!(api is ICoreClientAPI coreClientAPI) || !coreClientAPI.ObjectCache.TryGetValue("barrelMeshRefs", out var value)) {
                return;
            }

            foreach (KeyValuePair<int, MultiTextureMeshRef> item in value as Dictionary<int, MultiTextureMeshRef>) {
                item.Value.Dispose();
            }

            coreClientAPI.ObjectCache.Remove("barrelMeshRefs");
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f) {
            bool flag = false;
            BlockBehavior[] blockBehaviors = BlockBehaviors;
            foreach (BlockBehavior obj in blockBehaviors) {
                EnumHandling handling = EnumHandling.PassThrough;
                obj.OnBlockBroken(world, pos, byPlayer, ref handling);
                if (handling == EnumHandling.PreventDefault) {
                    flag = true;
                }

                if (handling == EnumHandling.PreventSubsequent) {
                    return;
                }
            }

            if (flag) {
                return;
            }

            if (world.Side == EnumAppSide.Server && (byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)) {
                ItemStack[] array = new ItemStack[1]
                {
                new ItemStack(this)
                };
                for (int j = 0; j < array.Length; j++) {
                    world.SpawnItemEntity(array[j], pos);
                }

                world.PlaySoundAt(Sounds.GetBreakSound(byPlayer), pos, 0.0, byPlayer);
            }

            if (EntityClass != null) {
                world.BlockAccessor.GetBlockEntity(pos)?.OnBlockBroken(byPlayer);
            }

            world.BlockAccessor.SetBlock(0, pos);
        }

        public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling) {
        }

        public override int TryPutLiquid(BlockPos pos, ItemStack liquidStack, float desiredLitres) {
            return base.TryPutLiquid(pos, liquidStack, desiredLitres);
        }

        public override int TryPutLiquid(ItemStack containerStack, ItemStack liquidStack, float desiredLitres) {
            return base.TryPutLiquid(containerStack, liquidStack, desiredLitres);
        }

        public MeshData GenMesh(ItemStack contentStack, ItemStack liquidContentStack, bool issealed, BlockPos forBlockPos = null) {
            ICoreClientAPI obj = api as ICoreClientAPI;
            Shape shape = Vintagestory.API.Common.Shape.TryGet(obj, issealed ? sealedShape : emptyShape);
            obj.Tesselator.TesselateShape(this, shape, out var modeldata);
            if (!issealed) {
                JsonObject containerProps = liquidContentStack?.ItemAttributes?["waterTightContainerProps"];
                MeshData meshData = getContentMeshFromAttributes(contentStack, liquidContentStack, forBlockPos) ?? getContentMeshLiquids(contentStack, liquidContentStack, forBlockPos, containerProps) ?? getContentMesh(contentStack, forBlockPos, contentsShape);
                if (meshData != null) {
                    modeldata.AddMeshData(meshData);
                }

                if (forBlockPos != null) {
                    modeldata.CustomInts = new CustomMeshDataPartInt(modeldata.FlagsCount);
                    modeldata.CustomInts.Values.Fill(67108864);
                    modeldata.CustomInts.Count = modeldata.FlagsCount;
                    modeldata.CustomFloats = new CustomMeshDataPartFloat(modeldata.FlagsCount * 2);
                    modeldata.CustomFloats.Count = modeldata.FlagsCount * 2;
                }
            }

            return modeldata;
        }

        private MeshData getContentMeshLiquids(ItemStack contentStack, ItemStack liquidContentStack, BlockPos forBlockPos, JsonObject containerProps) {
            bool flag = containerProps?["isopaque"].AsBool() ?? false;
            bool flag2 = containerProps?.Exists ?? false;
            if (liquidContentStack != null && (flag2 || contentStack == null)) {
                AssetLocation shapefilepath = contentsShape;
                if (flag2) {
                    shapefilepath = (flag ? opaqueLiquidContentsShape : liquidContentsShape);
                }

                return getContentMesh(liquidContentStack, forBlockPos, shapefilepath);
            }

            return null;
        }

        private MeshData getContentMeshFromAttributes(ItemStack contentStack, ItemStack liquidContentStack, BlockPos forBlockPos) {
            if (liquidContentStack != null && (liquidContentStack.ItemAttributes?["inBarrelShape"].Exists).GetValueOrDefault()) {
                AssetLocation shapefilepath = AssetLocation.Create(liquidContentStack.ItemAttributes?["inBarrelShape"].AsString(), contentStack.Collectible.Code.Domain).WithPathPrefixOnce("shapes").WithPathAppendixOnce(".json");
                return getContentMesh(contentStack, forBlockPos, shapefilepath);
            }

            return null;
        }

        protected MeshData getContentMesh(ItemStack stack, BlockPos forBlockPos, AssetLocation shapefilepath) {
            ICoreClientAPI coreClientAPI = api as ICoreClientAPI;
            WaterTightContainableProps containableProps = BlockLiquidContainerBase.GetContainableProps(stack);
            ITexPositionSource texPositionSource;
            float fillHeight;
            if (containableProps != null) {
                if (containableProps.Texture == null) {
                    return null;
                }

                texPositionSource = new ContainerTextureSource(coreClientAPI, stack, containableProps.Texture);
                fillHeight = GameMath.Min(1f, (float)stack.StackSize / containableProps.ItemsPerLitre / (float)Math.Max(50, containableProps.MaxStackSize)) * 10f / 16f;
            } else {
                texPositionSource = getContentTexture(coreClientAPI, stack, out fillHeight);
            }

            if (stack != null && texPositionSource != null) {
                Shape shape = Vintagestory.API.Common.Shape.TryGet(coreClientAPI, shapefilepath);
                if (shape == null) {
                    api.Logger.Warning($"Barrel block '{Code}': Content shape {shapefilepath} not found. Will try to default to another one.");
                    return null;
                }

                coreClientAPI.Tesselator.TesselateShape("barrel", shape, out var modeldata, texPositionSource, new Vec3f(Shape.rotateX, Shape.rotateY, Shape.rotateZ), containableProps?.GlowLevel ?? 0, 0, 0);
                modeldata.Translate(0f, fillHeight, 0f);
                if (containableProps != null && containableProps.ClimateColorMap != null) {
                    int color = coreClientAPI.World.ApplyColorMapOnRgba(containableProps.ClimateColorMap, null, -1, 196, 128, flipRb: false);
                    if (forBlockPos != null) {
                        color = coreClientAPI.World.ApplyColorMapOnRgba(containableProps.ClimateColorMap, null, -1, forBlockPos.X, forBlockPos.Y, forBlockPos.Z, flipRb: false);
                    }

                    byte[] array = ColorUtil.ToBGRABytes(color);
                    for (int i = 0; i < modeldata.Rgba.Length; i++) {
                        modeldata.Rgba[i] = (byte)(modeldata.Rgba[i] * array[i % 4] / 255);
                    }
                }

                return modeldata;
            }

            return null;
        }

        public static ITexPositionSource getContentTexture(ICoreClientAPI capi, ItemStack stack, out float fillHeight) {
            ITexPositionSource result = null;
            fillHeight = 0f;
            JsonObject jsonObject = stack?.ItemAttributes?["inContainerTexture"];
            if (jsonObject != null && jsonObject.Exists) {
                result = new ContainerTextureSource(capi, stack, jsonObject.AsObject<CompositeTexture>());
                fillHeight = GameMath.Min(0.75f, 0.7f * (float)stack.StackSize / (float)stack.Collectible.MaxStackSize);
            } else if (stack?.Block != null && (stack.Block.DrawType == EnumDrawType.Cube || stack.Block.Shape.Base.Path.Contains("basic/cube")) && capi.BlockTextureAtlas.GetPosition(stack.Block, "up", returnNullWhenMissing: true) != null) {
                result = new BlockTopTextureSource(capi, stack.Block);
                fillHeight = GameMath.Min(0.75f, 0.7f * (float)stack.StackSize / (float)stack.Collectible.MaxStackSize);
            } else if (stack != null) {
                if (stack.Class == EnumItemClass.Block) {
                    if (stack.Block.Textures.Count > 1) {
                        return null;
                    }

                    result = new ContainerTextureSource(capi, stack, stack.Block.Textures.FirstOrDefault().Value);
                } else {
                    if (stack.Item.Textures.Count > 1) {
                        return null;
                    }

                    result = new ContainerTextureSource(capi, stack, stack.Item.FirstTexture);
                }

                fillHeight = GameMath.Min(0.75f, 0.7f * (float)stack.StackSize / (float)stack.Collectible.MaxStackSize);
            }

            return result;
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot) {
            return new WorldInteraction[1]
            {
            new WorldInteraction
            {
                ActionLangCode = "heldhelp-place",
                HotKeyCode = "shift",
                MouseButton = EnumMouseButton.Right,
                ShouldApply = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => true
            }
            };
        }

        public override void OnLoaded(ICoreAPI api) {
            base.OnLoaded(api);
            if (Attributes != null) {
                capacityLitresFromAttributes = Attributes["capacityLitres"].AsInt(50);
                emptyShape = AssetLocation.Create(Attributes["emptyShape"].AsString(emptyShape), Code.Domain);
                sealedShape = AssetLocation.Create(Attributes["sealedShape"].AsString(sealedShape), Code.Domain);
                contentsShape = AssetLocation.Create(Attributes["contentsShape"].AsString(contentsShape), Code.Domain);
                opaqueLiquidContentsShape = AssetLocation.Create(Attributes["opaqueLiquidContentsShape"].AsString(opaqueLiquidContentsShape), Code.Domain);
                liquidContentsShape = AssetLocation.Create(Attributes["liquidContentsShape"].AsString(liquidContentsShape), Code.Domain);
            }

            emptyShape.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
            sealedShape.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
            contentsShape.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
            opaqueLiquidContentsShape.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
            liquidContentsShape.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
            if (api.Side != EnumAppSide.Client) {
                return;
            }

            ICoreClientAPI capi = api as ICoreClientAPI;
            interactions = ObjectCacheUtil.GetOrCreate(api, "liquidContainerBase", delegate {
                List<ItemStack> list = new List<ItemStack>();
                foreach (CollectibleObject collectible in api.World.Collectibles) {
                    if (collectible is ILiquidSource || collectible is ILiquidSink || collectible is BlockWateringCan) {
                        List<ItemStack> handBookStacks = collectible.GetHandBookStacks(capi);
                        if (handBookStacks != null) {
                            list.AddRange(handBookStacks);
                        }
                    }
                }

                ItemStack[] lstacks = list.ToArray();
                ItemStack[] linenStack = new ItemStack[1]
                {
                new ItemStack(api.World.GetBlock(new AssetLocation("linen-normal-down")))
                };
                return new WorldInteraction[2]
                {
                new WorldInteraction
                {
                    ActionLangCode = "blockhelp-bucket-rightclick",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = lstacks,
                    GetMatchingStacks = delegate(WorldInteraction wi, BlockSelection bs, EntitySelection ws)
                    {
                        BlockEntityMetalBarrel obj = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityMetalBarrel;
                        return (obj == null || obj.Sealed) ? null : lstacks;
                    }
                },
                new WorldInteraction
                {
                    ActionLangCode = "blockhelp-barrel-takecottagecheese",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "shift",
                    Itemstacks = linenStack,
                    GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection ws) => ((api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityMetalBarrel)?.Inventory[1].Itemstack?.Item?.Code?.Path == "cottagecheeseportion") ? linenStack : null
                }
                };
            });
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection blockSel, IPlayer forPlayer) {
            BlockEntityMetalBarrel blockEntityBarrel = null;
            if (blockSel.Position != null) {
                blockEntityBarrel = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityMetalBarrel;
            }

            if (blockEntityBarrel != null && blockEntityBarrel.Sealed) {
                return new WorldInteraction[0];
            }

            return base.GetPlacedBlockInteractionHelp(world, blockSel, forPlayer);
        }

        public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling) {
            base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
            if (blockSel != null && !world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use)) {
                return false;
            }

            BlockEntityMetalBarrel blockEntityBarrel = null;
            if (blockSel.Position != null) {
                blockEntityBarrel = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityMetalBarrel;
            }

            if (blockEntityBarrel != null && blockEntityBarrel.Sealed) {
                return true;
            }

            bool flag = base.OnBlockInteractStart(world, byPlayer, blockSel);
            if (!flag && !byPlayer.WorldData.EntityControls.ShiftKey && blockSel.Position != null) {
                blockEntityBarrel?.OnPlayerRightClick(byPlayer);
                return true;
            }

            return flag;
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            ItemStack[] contents = GetContents(world, inSlot.Itemstack);
            if (contents != null && contents.Length != 0) {
                ItemStack itemStack = ((contents[0] == null) ? contents[1] : contents[0]);
                if (itemStack != null) {
                    dsc.Append(", " + Lang.Get("{0}x {1}", itemStack.StackSize, itemStack.GetName()));
                }
            }
        }

        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer) {
            string text = base.GetPlacedBlockInfo(world, pos, forPlayer);
            string text2 = "";
            int num = text.IndexOfOrdinal(Environment.NewLine + Environment.NewLine);
            if (num > 0) {
                text2 = text.Substring(num);
                text = text.Substring(0, num);
            }

            if (GetCurrentLitres(pos) <= 0f) {
                text = "";
            }

            if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityMetalBarrel blockEntityBarrel) {
                ItemSlot itemSlot = blockEntityBarrel.Inventory[0];
                if (!itemSlot.Empty) {
                    text = ((text.Length <= 0) ? (text + Lang.Get("Contents:") + "\n ") : (text + " "));
                    text += Lang.Get("{0}x {1}", itemSlot.Itemstack.StackSize, itemSlot.Itemstack.GetName());
                    text += BlockLiquidContainerBase.PerishableInfoCompact(api, itemSlot, 0f, withStackName: false);
                }

                if (blockEntityBarrel.Sealed && blockEntityBarrel.CurrentRecipe != null) {
                    double num2 = world.Calendar.TotalHours - blockEntityBarrel.SealedSinceTotalHours;
                    if (num2 < 3.0) {
                        num2 = Math.Max(0.0, num2 + 0.2);
                    }

                    string text3 = ((num2 > 24.0) ? Lang.Get("{0} days", Math.Floor(num2 / (double)api.World.Calendar.HoursPerDay * 10.0) / 10.0) : Lang.Get("{0} hours", Math.Floor(num2)));
                    string text4 = ((blockEntityBarrel.CurrentRecipe.SealHours > 24.0) ? Lang.Get("{0} days", Math.Round(blockEntityBarrel.CurrentRecipe.SealHours / (double)api.World.Calendar.HoursPerDay, 1)) : Lang.Get("{0} hours", Math.Round(blockEntityBarrel.CurrentRecipe.SealHours)));
                    text = text + "\n" + Lang.Get("Sealed for {0} / {1}", text3, text4);
                }
            }

            return text + text2;
        }

        public override void TryFillFromBlock(EntityItem byEntityItem, BlockPos pos) {
        }
    }
}
