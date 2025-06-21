using GloomeClasses.src.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace GloomeClasses.src.BlockBehaviors {

    public class TranslocatorTrackerBlockEntityBehavior : BlockEntityBehavior {

        GenericPOI poi;

        public TranslocatorTrackerBlockEntityBehavior(BlockEntity blockentity) : base(blockentity) {
            
        }

        public override void Initialize(ICoreAPI api, JsonObject properties) {
            base.Initialize(api, properties);

            GloomeClassesModSystem.Logger.Warning("Translocator initialized with Tracking behavior! Loc: " + Pos);
            if (poi == null) {
                poi = new GenericPOI(Pos.ToVec3d(), "translocator");
                api.ModLoader.GetModSystem<POIRegistry>().AddPOI(poi);
            }
        }

        public override void OnPlacementBySchematic(ICoreServerAPI api, IBlockAccessor blockAccessor, BlockPos pos, Dictionary<int, Dictionary<int, int>> replaceBlocks, int centerrockblockid, Block layerBlock, bool resolveImports) {
            GloomeClassesModSystem.Logger.Warning("Translocator initialized with Tracking behavior! Loc: " + Pos);
            if (poi == null) {
                poi = new GenericPOI(Pos.ToVec3d(), "translocator");
                api.ModLoader.GetModSystem<POIRegistry>().AddPOI(poi);
            }
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null) {
            GloomeClassesModSystem.Logger.Warning("Translocator initialized with Tracking behavior! Loc: " + Pos);
            if (poi == null) {
                poi = new GenericPOI(Pos.ToVec3d(), "translocator");
                Api.ModLoader.GetModSystem<POIRegistry>().AddPOI(poi);
            }
        }

        public override void OnBlockUnloaded() {
            if (poi != null) {
                Api.ModLoader.GetModSystem<POIRegistry>().RemovePOI(poi);
                poi = null;
            }
        }

        public override void OnBlockRemoved() {
            if (poi != null) {
                Api.ModLoader.GetModSystem<POIRegistry>().RemovePOI(poi);
                poi = null;
            }
        }

        public override void OnBlockBroken(IPlayer byPlayer = null) {
            if (poi != null) {
                Api.ModLoader.GetModSystem<POIRegistry>().RemovePOI(poi);
                poi = null;
            }
        }
    }
}
