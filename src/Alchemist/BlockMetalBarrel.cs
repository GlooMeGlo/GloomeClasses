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
using GloomeClasses.src.Utils;

namespace GloomeClasses.src.Alchemist {
    public class BlockMetalBarrel : BlockLiquidContainerBase {
        public override bool AllowHeldLiquidTransfer => false;
        public AssetLocation EmptyShape { get; protected set; } = AssetLocation.Create("block/wood/barrel/empty");
        public AssetLocation SealedShape { get; protected set; } = AssetLocation.Create("block/wood/barrel/closed");
        public AssetLocation ContentsShape { get; protected set; } = AssetLocation.Create("block/wood/barrel/contents");
        public AssetLocation OpaqueLiquidContentsShape { get; protected set; } = AssetLocation.Create("block/wood/barrel/opaqueliquidcontents");
        public AssetLocation LiquidContentsShape { get; protected set; } = AssetLocation.Create("block/wood/barrel/liquidcontents");

        public override int GetContainerSlotId(BlockPos pos) {
            return 1;
        }

        public override int GetContainerSlotId(ItemStack containerStack) {
            return 1;
        }

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo) {
            string cacheKey = "barrelMeshRefs" + Code;
            Dictionary<string, MultiTextureMeshRef> meshCache = (Dictionary<string, MultiTextureMeshRef>)(capi.ObjectCache.TryGetValue(cacheKey, out object value) ? (value as Dictionary<string, MultiTextureMeshRef>) : (capi.ObjectCache[cacheKey] = new Dictionary<string, MultiTextureMeshRef>()));

            ItemStack[] contents = GetContents(capi.World, itemstack);
            if (contents != null && contents.Length != 0) {
                bool isSealed = itemstack.Attributes.GetBool("sealed");
                string meshKey = GetBarrelMeshkey(contents[0], (contents.Length > 1) ? contents[1] : null, isSealed);

                if (!meshCache.TryGetValue(meshKey, out var meshRef)) {
                    // cache miss - generate new mesh
                    MeshDiagnostics.RecordCacheMiss("BlockMetalBarrel");
                    MeshData data = GenMesh(contents[0], (contents.Length > 1) ? contents[1] : null, isSealed);
                    MeshDiagnostics.RecordMeshGeneration("BlockMetalBarrel", data?.VerticesCount ?? 0);
                    meshRef = meshCache[meshKey] = capi.Render.UploadMultiTextureMesh(data);
                } else {
                    // cache hit - reusing existing mesh
                    MeshDiagnostics.RecordCacheHit("BlockMetalBarrel");
                }

                renderinfo.ModelRef = meshRef;
            }
        }

        public string GetBarrelMeshkey(ItemStack contentStack, ItemStack liquidStack, bool isSealed) {
            // use stable identifiers instead of GetHashCode() which can vary
            string contentKey = contentStack != null
                ? $"{contentStack.Collectible.Code}:{contentStack.StackSize}"
                : "empty";
            string liquidKey = liquidStack != null
                ? $"{liquidStack.Collectible.Code}:{liquidStack.StackSize}"
                : "empty";

            return $"{contentKey}|{liquidKey}|{(isSealed ? "sealed" : "open")}";
        }

        public override void OnUnloaded(ICoreAPI api) {
            if (api is not ICoreClientAPI coreClientAPI) {
                return;
            }

            string cacheKey = "barrelMeshRefs" + Code;
            if (!coreClientAPI.ObjectCache.TryGetValue(cacheKey, out var value)) {
                return;
            }

            // properly dispose of all cached meshes
            foreach (KeyValuePair<string, MultiTextureMeshRef> item in value as Dictionary<string, MultiTextureMeshRef>) {
                item.Value?.Dispose();
            }

            coreClientAPI.ObjectCache.Remove(cacheKey);
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
                ItemStack[] array =
                [
                new ItemStack(this)
                ];
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
            Shape shape = Vintagestory.API.Common.Shape.TryGet(obj, issealed ? SealedShape : EmptyShape);
            obj.Tesselator.TesselateShape(this, shape, out var modeldata);
            if (!issealed) {
                JsonObject containerProps = liquidContentStack?.ItemAttributes?["waterTightContainerProps"];
                MeshData meshData = GetContentMeshFromAttributes(contentStack, liquidContentStack, forBlockPos) ?? GetContentMeshLiquids(contentStack, liquidContentStack, forBlockPos, containerProps) ?? GetContentMesh(contentStack, forBlockPos, ContentsShape);
                if (meshData != null) {
                    modeldata.AddMeshData(meshData);
                }

                if (forBlockPos != null) {
                    modeldata.CustomInts = new CustomMeshDataPartInt(modeldata.FlagsCount);
                    modeldata.CustomInts.Values.Fill(67108864);
                    modeldata.CustomInts.Count = modeldata.FlagsCount;
                    modeldata.CustomFloats = new CustomMeshDataPartFloat(modeldata.FlagsCount * 2)
                    {
                        Count = modeldata.FlagsCount * 2
                    };
                }
            }

            return modeldata;
        }

        private MeshData GetContentMeshLiquids(ItemStack contentStack, ItemStack liquidContentStack, BlockPos forBlockPos, JsonObject containerProps) {
            bool flag = containerProps?["isopaque"].AsBool() ?? false;
            bool flag2 = containerProps?.Exists ?? false;
            if (liquidContentStack != null && (flag2 || contentStack == null)) {
                AssetLocation shapefilepath = ContentsShape;
                if (flag2) {
                    shapefilepath = (flag ? OpaqueLiquidContentsShape : LiquidContentsShape);
                }

                return GetContentMesh(liquidContentStack, forBlockPos, shapefilepath);
            }

            return null;
        }

        private MeshData GetContentMeshFromAttributes(ItemStack contentStack, ItemStack liquidContentStack, BlockPos forBlockPos) {
            if (liquidContentStack != null && (liquidContentStack.ItemAttributes?["inBarrelShape"].Exists).GetValueOrDefault()) {
                AssetLocation shapefilepath = AssetLocation.Create(liquidContentStack.ItemAttributes?["inBarrelShape"].AsString(), contentStack.Collectible.Code.Domain).WithPathPrefixOnce("shapes").WithPathAppendixOnce(".json");
                return GetContentMesh(contentStack, forBlockPos, shapefilepath);
            }

            return null;
        }

        protected MeshData GetContentMesh(ItemStack stack, BlockPos forBlockPos, AssetLocation shapefilepath) {
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
                texPositionSource = GetContentTexture(coreClientAPI, stack, out fillHeight);
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

        public static ITexPositionSource GetContentTexture(ICoreClientAPI capi, ItemStack stack, out float fillHeight) {
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
            return
            [
            new WorldInteraction
            {
                ActionLangCode = "heldhelp-place",
                HotKeyCode = "shift",
                MouseButton = EnumMouseButton.Right,
                ShouldApply = (wi, bs, es) => true
            }
            ];
        }

        public override void OnLoaded(ICoreAPI api) {
            base.OnLoaded(api);
            if (Attributes != null) {
                capacityLitresFromAttributes = Attributes["capacityLitres"].AsInt(50);
                EmptyShape = AssetLocation.Create(Attributes["emptyShape"].AsString(EmptyShape), Code.Domain);
                SealedShape = AssetLocation.Create(Attributes["sealedShape"].AsString(SealedShape), Code.Domain);
                ContentsShape = AssetLocation.Create(Attributes["contentsShape"].AsString(ContentsShape), Code.Domain);
                OpaqueLiquidContentsShape = AssetLocation.Create(Attributes["opaqueLiquidContentsShape"].AsString(OpaqueLiquidContentsShape), Code.Domain);
                LiquidContentsShape = AssetLocation.Create(Attributes["liquidContentsShape"].AsString(LiquidContentsShape), Code.Domain);
            }

            EmptyShape.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
            SealedShape.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
            ContentsShape.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
            OpaqueLiquidContentsShape.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
            LiquidContentsShape.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
            if (api.Side != EnumAppSide.Client) {
                return;
            }

            ICoreClientAPI capi = api as ICoreClientAPI;
            interactions = ObjectCacheUtil.GetOrCreate(api, "liquidContainerBase", delegate {
                List<ItemStack> list = [];
                foreach (CollectibleObject collectible in api.World.Collectibles) {
                    if (collectible is ILiquidSource || collectible is ILiquidSink || collectible is BlockWateringCan) {
                        List<ItemStack> handBookStacks = collectible.GetHandBookStacks(capi);
                        if (handBookStacks != null) {
                            list.AddRange(handBookStacks);
                        }
                    }
                }

                ItemStack[] lstacks = [.. list];
                ItemStack[] linenStack =
                [
                new ItemStack(api.World.GetBlock(new AssetLocation("linen-normal-down")))
                ];
                return new WorldInteraction[2]
                {
                new() {
                    ActionLangCode = "blockhelp-bucket-rightclick",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = lstacks,
                    GetMatchingStacks = delegate(WorldInteraction wi, BlockSelection bs, EntitySelection ws)
                    {
                        return (api.World.BlockAccessor.GetBlockEntity(bs.Position) is not BlockEntityMetalBarrel obj || obj.Sealed) ? null : lstacks;
                    }
                },
                new() {
                    ActionLangCode = "blockhelp-barrel-takecottagecheese",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "shift",
                    Itemstacks = linenStack,
                    GetMatchingStacks = (wi, bs, ws) => ((api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityMetalBarrel)?.Inventory[1].Itemstack?.Item?.Code?.Path == "cottagecheeseportion") ? linenStack : null
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
                return [];
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
                ItemStack itemStack = (contents[0] ?? contents[1]);
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
                text2 = text[num..];
                text = text[..num];
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
