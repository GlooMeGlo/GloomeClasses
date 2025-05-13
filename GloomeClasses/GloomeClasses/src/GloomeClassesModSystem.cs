using GloomeClasses.src.EntityBehaviors;
using HarmonyLib;
using System.Reflection;
using Vintagestory;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;


namespace GloomeClasses {
    public class GloomeClassesModSystem : ModSystem {

        private static Harmony harmony;

        public static ICoreAPI Api;
        public static ILogger Logger;

        internal static void ApplyHarmonyPatches(ICoreAPI api) {
            /*if (harmony == null) {
                harmony = new Harmony("GloomeClassesPatch");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }*/
        }

        public override void StartPre(ICoreAPI api) {
            Api = api;
            Logger = Mod.Logger;
        }

        public override void Start(ICoreAPI api) {
            //api.RegisterBlockBehaviorClass("CropDropBonus", typeof(CropBonusBehavior));
            //api.RegisterBlockBehaviorClass("LogDropBonus", typeof(TreelogBonusBehavior));
            //api.RegisterBlockBehaviorClass("LeavesBonusBehavior", typeof(LeavesBonusBehavior));
            //api.RegisterBlockBehaviorClass("CharcoalDropRate", typeof(CharcoalBonusBehavior));
            //api.RegisterBlockBehaviorClass("GrassDropRate", typeof(GrassBonusBehavior));

            //api.RegisterEntityBehaviorClass("GC_ClassesPlayerBehavior", typeof(ClassesPlayerBehavior));
            api.RegisterEntityBehaviorClass("EntityBehaviorDread", typeof(DreadBehavior));
        }

        public override void StartServerSide(ICoreServerAPI api) {
            //ApplyHarmonyPatches(api);

            /*api.Event.PlayerNowPlaying += (serverPlayer) => {
                if (serverPlayer.Entity.GetBehavior("GC_ClassesPlayerBehavior") == null) {
                    serverPlayer.Entity.AddBehavior(new ClassesPlayerBehavior(serverPlayer.Entity));
                }
            };*/
        }

        public override void StartClientSide(ICoreClientAPI api) {
            
        }
    }
}
