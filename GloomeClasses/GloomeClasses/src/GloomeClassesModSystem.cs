using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
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
            api.RegisterBlockBehaviorClass("charcoalDropRate", typeof(CharcoalBonusBehavior));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            api.Logger.Notification("Hello from template mod server side: " + Lang.Get("gloomeclasses:hello"));


            base.StartServerSide(api);
            ApplyHarmonyPatches(api);
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            api.Logger.Notification("Hello from template mod client side: " + Lang.Get("gloomeclasses:hello"));
        }
    }
}
