using GloomeClasses;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Client;
using Vintagestory.Common;
using Vintagestory.Server;


namespace GloomeClasses
{
    public class GloomeClassesModSystem : ModSystem
    {
        // Called on server and client
        // Useful for registering block/entity classes on both sides
        private static Harmony harmony;

        public ICoreAPI Api;



        internal static void ApplyHarmonyPatches(ICoreAPI api)
        {
            if (harmony == null)
            {
                harmony = new Harmony("GloomeClassesPatch");
                harmony.PatchAll(Assembly.GetExecutingAssembly());

            }



        }




        public override void Start(ICoreAPI api)
        {
            api.Logger.Notification("Hello from template mod: " + api.Side);

            api.RegisterBlockBehaviorClass("CropDropBonus", typeof(CropBonusBehavior));
            api.RegisterBlockBehaviorClass("LogDropBonus", typeof(TreelogBonusBehavior));
            api.RegisterBlockBehaviorClass("LeavesBonusBehavior", typeof(LeavesBonusBehavior));
            api.RegisterBlockBehaviorClass("CharcoalDropRate", typeof(CharcoalBonusBehavior));
            api.RegisterBlockBehaviorClass("GrassDropRate", typeof(GrassBonusBehavior));

            api.RegisterEntityBehaviorClass("GC_ClassesPlayerBehavior", typeof(ClassesPlayerBehavior));
            api.RegisterEntityBehaviorClass("DreadBehaviorClass", typeof(DreadBehavior));
            
            
            
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            api.Logger.Notification("Hello from template mod server side: " + Lang.Get("gloomeclasses:hello"));


            base.StartServerSide(api);
            ApplyHarmonyPatches(api);


            api.Event.PlayerNowPlaying += (serverPlayer) =>
            {
                if (serverPlayer.Entity.GetBehavior("GC_ClassesPlayerBehavior") == null)
                {
                    serverPlayer.Entity.AddBehavior(new ClassesPlayerBehavior(serverPlayer.Entity));
                }
            };
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            api.Logger.Notification("Hello from template mod client side: " + Lang.Get("gloomeclasses:hello"));
        }
    }
}
