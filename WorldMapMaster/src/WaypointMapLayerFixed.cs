using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace xtendedMap.src
{
    // NOTE: Cut down this monstrous class into small ones
    public partial class WaypointMapLayerFixed : WaypointMapLayer
    {
        public WaypointMapLayerFixed(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink) //preparing to work!
        {
            WorldMapMasterModSystem.Api.Logger.Event($"[worldmapmaster] WaypointMapLayerFixed instantiated. Side: {WorldMapMasterModSystem.Api.Side}");
            capi = api as ICoreClientAPI;
        }
        
        private List<WaypointListItem> wpListData = new(); // stores waypoint data on the map
        private readonly string key = "worldmap-layer-waypoints"; // api key
        public readonly ICoreClientAPI capi;
        private string qsText = string.Empty; // search
        private string wpListOrder = "timeasc"; // sorting (time ascendig by default)
        private GuiDialogWorldMap guiDialogWorldMap;
        private GuiComposer compo;
       
        

        #region working with a ready-made wpListData list
        private void onQSChanged(string text)
        {
            api.Logger.Event("[xtMap]: UpdateList() cause onQSChanged (line 28)"); // DEBUG ONLY //
            qsText = text;
            UpdateList(); //called 2
        }
        private void onOrderingChanged(string uid, bool selected)
        {
            api.Logger.Event("[xtMap]: UpdateList() cause OnOrderingChanged (line 35)"); // DEBUG ONLY //
            wpListOrder = uid;
            UpdateList(); //called 3
        }       
        //after this function OnDataFromServer() calling too... WHY?
        private void onSelectionChanged(string uid, bool selected) //function of GuiElementDropDown
        {
            switch (string.IsNullOrEmpty(uid))
            {
                case true: 
                    api.Logger.Error("[xtMap]: current waypoint uid is null (line 44)."); return;
                case false:
                    break;
            }
            if (uid.Equals("--1")) return; //skip

            var mapElem = compo.GetElement("mapElem") as GuiElementMap;

            foreach (Waypoint waypoint in ownWaypoints)
            {
                if (waypoint.Guid.Equals(uid)) //if uid equals waypoint guid
                {
                    BlockPos pos = waypoint.Position.AsBlockPos; //set point coordinates (BlockPos pos - XYZ coordinates of the block)
                    mapElem.CenterMapTo(pos); //center the map on coordinates
                    break;
                }
            }
        }
        #endregion

        #region working with server and graphics... 
        public override void OnDataFromServer(byte[] data) //server send ownWaypoints list to client...
        {
            base.OnDataFromServer(data);
            api.Logger.Event("[xtMap]: UpdateList() cause OnDataFromServer(line 65)"); // DEBUG ONLY //
            UpdateList(); //called 4
        }
        public override void Dispose()
        {
            base.Dispose();
        }
        #endregion

        #region TEMP/TEST STUFF
        public override void OnMapClosedClient()
        {
            base.OnMapClosedClient();
            api.Logger.Warning("[xtMap]: OnMapClosedClient detected!"); // DEBUG ONLY //
        }
        public override void OnMapOpenedClient()
        {
            base.OnMapOpenedClient();
            api.Logger.Warning("[xtMap]: OnMapOpenedClient detected!"); // DEBUG ONLY //
        }
        #endregion
    }
}
/* Original author: ZigTheHedge (all code of mod)
 z3r0-t0ru: partial refactoring, bugfixes, comments
history:
Jan-22-2025: 1.0 - initial commit
Jan-24-2025: 1.0.1 - update, GUI fixes, added OnDataFromServer
Jan-28-2025: 1.0.2 - Really fixed WorldMapLayer patch - refactoring, added initialization Log message
Jan-29-2025: 1.0.3 - Fixed "My waypoints" windows to stay on screen forever - added DropDownList
Jan-29-2025: 1.0.4-pre.1 - Filtering and Sorting - added UpdateList() 
May-13-2025: 1.0.4-pre.2 - rewrite, added anti-null exception
May-14-2025: 1.0.4-pre.3 - Fix: if the server returns Guid == null, mod generates a new one for the point; total and complete code commenting XD */