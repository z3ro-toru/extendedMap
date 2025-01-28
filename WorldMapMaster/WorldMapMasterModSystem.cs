using HarmonyLib;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using WorldMapMaster.src;

namespace WorldMapMaster
{
    public class WorldMapMasterModSystem : ModSystem
    {
        Harmony harmony = new Harmony("com.cwelth.worldmapmaster");
        public static int wpIndex = -1;
        public static Vec3d newWpPos;
        public static ICoreAPI Api;
        public static ICoreClientAPI capi;
        GuiDialogAddWayPoint addWpDlg;

        //public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Client;

        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);
            Api = api;
            api.Logger.Event("[worldmapmaster] Starting " + api.Side);
            //Harmony.DEBUG = true;
            harmony.PatchAll();

        }
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            capi = api;

            api.Input.RegisterHotKey("waypointDelete", "Remove hovered Waypoint", GlKeys.Delete, HotkeyType.HelpAndOverlays);
            api.Input.RegisterHotKey("waypointAdd", "Add a Waypoint at current position", GlKeys.PageDown);
            api.Input.RegisterHotKey("waypointQuickAdd", "Add a Waypoint at current position with default title", GlKeys.KeypadPlus);

            api.Input.SetHotKeyHandler("waypointDelete", DeleteWaypoint);
            api.Input.SetHotKeyHandler("waypointAdd", AddNewWaypoint);
            api.Input.SetHotKeyHandler("waypointQuickAdd", QuickAddWaypoint);
        }
        private bool DeleteWaypoint(KeyCombination keyCombination)
        {
            if (wpIndex > -1)
            {
                capi.SendChatMessage(string.Format("/waypoint remove {0}", wpIndex));
                wpIndex = -1;
            }
            return true;
        }

        private bool AddNewWaypoint(KeyCombination keyCombination)
        {
            if (addWpDlg != null)
            {
                addWpDlg.TryClose();
                addWpDlg.Dispose();
            }
            var maplayers = capi.ModLoader.GetModSystem<WorldMapManager>().MapLayers;

            var wml = maplayers.FirstOrDefault(l => l is WaypointMapLayer) as WaypointMapLayer;

            addWpDlg = new GuiDialogAddWayPoint(capi, wml);
            newWpPos = capi.World.Player.Entity.Pos.XYZ;
            addWpDlg.TryOpen();

            return true;
        }

        private bool QuickAddWaypoint(KeyCombination keyCombination)
        {
            Vec3d curPos = capi.World.Player.Entity.Pos.XYZ;
            Vec3d hrPos = curPos.Clone().Sub(capi.World.DefaultSpawnPosition.AsBlockPos);
            capi.SendChatMessage(string.Format("/waypoint addati {0} ={1} ={2} ={3} {4} {5} {6}", "circle", 
                curPos.XInt.ToString(GlobalConstants.DefaultCultureInfo), 
                curPos.YInt.ToString(GlobalConstants.DefaultCultureInfo),
                curPos.ZInt.ToString(GlobalConstants.DefaultCultureInfo), "false", "white", $"{hrPos.XInt}, {hrPos.YInt}, {hrPos.ZInt}"));

            return true;
        }
        public override void Dispose()
        {
            base.Dispose();
            harmony.UnpatchAll("com.cwelth.worldmapmaster");
        }
    }
}
