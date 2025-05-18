using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography; //not needed?
using System.Text; //not needed?
using System.Threading.Tasks; //not needed?
using System.Diagnostics;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using System.Collections;

namespace WorldMapMaster.src
{
    public class WaypointListItem
    {
        public string uid { get; set; } = string.Empty; // internal identifier of waypoint/identificator like {xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx}
        public string Title { get; set; } = string.Empty; // waypoint name
        public int Id { get; set; } // waypoint number
        public float Distance { get; set; } // distance
    }

    public class WaypointMapLayerFixed : WaypointMapLayer
    {
        private List<WaypointListItem> wpTempDataList = new(); // for sorting and temporary storage
        private readonly string key = "worldmap-layer-waypoints"; // api key
        private readonly ICoreClientAPI capi;
        private string qsText = string.Empty; // search
        private string sortingOrder = string.Empty;
        private GuiDialogWorldMap guiDialogWorldMap;
        private GuiComposer compo;
        //private int anti = 2147483647;

        public WaypointMapLayerFixed(ICoreAPI api, IWorldMapManager mapSink)
            : base(api, mapSink)
        {
            WorldMapMasterModSystem.Api.Logger.Event($"[worldmapmaster] WaypointMapLayerFixed instantiated. Side: {WorldMapMasterModSystem.Api.Side}");
            capi = api as ICoreClientAPI;
        }

        public override void ComposeDialogExtras(GuiDialogWorldMap guiDialogWorldMap = null, GuiComposer compo = null)
        {
            this.guiDialogWorldMap = guiDialogWorldMap ?? this.guiDialogWorldMap;
            this.compo = compo ?? this.compo;
            UpdateList(); //called 1
            api.Logger.Event("WMM RE: UpdateList() cause ComposeDialogExtras (line 42)"); // DEBUG ONLY //

            ElementBounds dlgBounds = ElementStdBounds.AutosizedMainDialog
                .WithFixedPosition(
                    (this.compo.Bounds.renderX + this.compo.Bounds.OuterWidth) / RuntimeEnv.GUIScale + 10,
                    this.compo.Bounds.renderY / RuntimeEnv.GUIScale + 120
                )
                .WithAlignment(EnumDialogArea.None);

            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;

            this.guiDialogWorldMap.Composers[key] =
                capi.Gui
                    .CreateCompo(key, dlgBounds)
                    .AddShadedDialogBG(bgBounds, false)
                    .AddDialogTitleBar(Lang.Get("Your waypoints:"), () => { this.guiDialogWorldMap.Composers[key].Enabled = false; })
                    .BeginChildElements(bgBounds)
                        .AddDropDown(wpTempDataList.Select(o => o.uid).ToArray(),
                                     wpTempDataList.Select(o => o.Title).ToArray(),
                                     0,
                                     onSelectionChanged,
                                     ElementBounds.Fixed(0, 75, 300, 35), "wpDisplayedList")
                        .AddAutoclearingText(ElementBounds.Fixed(0, 30, 120, 35),
                                             onQSChanged,
                                             null,
                                             "qs")
                        .AddDropDown(new[] { Lang.Get("timeasc"), Lang.Get("timedesc"), Lang.Get("distanceasc"), Lang.Get("distancedesc"), Lang.Get("titleasc"), Lang.Get("titledesc") },
                                     new[] { Lang.Get("timeasc"), Lang.Get("timedesc"), Lang.Get("distanceasc"), Lang.Get("distancedesc"), Lang.Get("titleasc"), Lang.Get("titledesc") },
                                     0,
                                     onOrderingChanged,
                                     ElementBounds.Fixed(125, 30, 125, 35),
                                     "orderlist")
                    .EndChildElements()
                    .Compose();

            var qsTextElement = this.guiDialogWorldMap.Composers[key].GetElement("qs") as GuiElementTextInput;
            qsTextElement.SetValue(qsText);
            qsTextElement.SetPlaceHolderText(Lang.Get("Search..."));
            var orderList = this.guiDialogWorldMap.Composers[key].GetElement("orderlist") as GuiElementDropDown;
            orderList.SetSelectedValue(sortingOrder);
            this.guiDialogWorldMap.Composers[key].Enabled = false;
        }

        
        private void onQSChanged(string text)
        {
            api.Logger.Event("WMM RE: UpdateList() cause onQSChanged (line 91)"); // DEBUG ONLY //
            qsText = text;
            UpdateList(); //called 2
        }

        private void onOrderingChanged(string uid, bool selected)
        {
            api.Logger.Event("WMM RE: UpdateList() cause OnOrderingChanged (line 98)"); // DEBUG ONLY //
            sortingOrder = uid;
            UpdateList(); //called 3
        }

        

        private void UpdateList() //issue: When the player moves, the method is called too often (possibly to update the point information), which causes freezes            
        {            
            var wpDisplayedList = this.guiDialogWorldMap.Composers[key]?.GetElement("wpDisplayedList") as GuiElementDropDown; //link wpDisplayedList contents in a drop-down list
            wpTempDataList.Clear();
            int counter = 0; //to assign a point number
            EntityPos playerPosition = capi.World.Player.Entity.Pos; //player entity position is taken via client API (possibly a request to the server)

            foreach (Waypoint waypoint in ownWaypoints) //checks the distance for each point
            {
                if (waypoint.Title.Contains(qsText, StringComparison.InvariantCultureIgnoreCase)) //register-insensitive comparison
                {
                        if (waypoint.Guid == null) //server returns an empty Guid for story locations and death points (can be solved by restarting the server)
                        {
                            waypoint.Guid = Guid.NewGuid().ToString();
                            api.Logger.Warning("WMM RE: Waypoint (" + waypoint.Title + ") GUID fixed"); //fixing trouble of vanilla game (Tyron pls fix it) // DEBUG ONLY //
                        };
                        float distance = (float)Math.Sqrt(
                            Math.Pow(playerPosition.X - waypoint.Position.X, 2) 
                            + Math.Pow(playerPosition.Z - waypoint.Position.Z, 2));
                        wpTempDataList.Add(new WaypointListItem
                        {
                            uid = waypoint.Guid, //is taken from server Guid
                            Title = $"{waypoint.Title} - {distance:F2}m",
                            Distance = distance,
                            Id = counter++ //order number of the point in list
                        });

                        //api.Logger.Event("WMM RE: UpdateList() - waypoint " + waypoint.Guid + " " + waypoint.Title + " added"); // DEBUG ONLY //
                }
            }

                
           switch (sortingOrder) //sorting
            {
                case "timeasc":
                    wpTempDataList.Sort((a, b) => a.Id.CompareTo(b.Id));
                    break;
                case "timedesc":
                    wpTempDataList.Sort((a, b) => b.Id.CompareTo(a.Id));
                    break;
                case "distanceasc":
                    wpTempDataList.Sort((a, b) => a.Distance.CompareTo(b.Distance));
                    break;
                case "distancedesc":
                    wpTempDataList.Sort((a, b) => b.Distance.CompareTo(a.Distance));
                    break;
                case "titleasc":
                    wpTempDataList.Sort((a, b) => string.Compare(a.Title, b.Title, StringComparison.InvariantCulture));
                    break;
                case "titledesc":
                    wpTempDataList.Sort((a, b) => string.Compare(b.Title, a.Title, StringComparison.InvariantCulture));
                    break;
                default:
                    // sort by default
                    wpTempDataList.Sort((a, b) => a.Id.CompareTo(b.Id));
                    break;
            }

            wpTempDataList.Insert(0, new WaypointListItem { uid = "--1", Title = "- - -", Distance = 0, Id = -1 }); //empty string
            wpDisplayedList?.SetList(
                wpTempDataList.Select(o => o.uid).ToArray(), 
                wpTempDataList.Select(o => o.Title).ToArray()
                );           
        }

        private void onSelectionChanged(string uid, bool selected) //function of GuiElementDropDown
        {
            if (string.IsNullOrEmpty(uid))
            {
                api.Logger.Error("WMM RE: current waypoint uid is null (1st defence) line 147.");
                return;
            }
            if (uid.Equals("--1")) return; //skip

            var mapElem = compo.GetElement("mapElem") as GuiElementMap;

            foreach (Waypoint waypoint in ownWaypoints)
            {
                if ((waypoint?.Guid) != null) //if uid is empty
                {
                    if (waypoint.Guid.Equals(uid)) //if uid equals waypoint guid
                    {
                        BlockPos pos = waypoint.Position.AsBlockPos; //set point coordinates (BlockPos pos - XYZ coordinates of the block)
                        mapElem.CenterMapTo(pos); //center the map on coordinates
                        break;
                    }
                }
                else
                {
                    api.Logger.Error("WMM RE: current waypoint uid is null (2nd defence) line 158.");
                    continue; //skip
                }
            }
        }

        public override void OnDataFromServer(byte[] data)
        {
            base.OnDataFromServer(data);
            api.Logger.Event("WMM RE: UpdateList() cause OnDataFromServer, line 175"); // DEBUG ONLY //
            Task.Run(UpdateList);
        }

        public override void OnMapClosedClient()
        {
            base.OnMapClosedClient();
        }

        public override void Dispose()
        {
            base.Dispose();
        }
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
May-14-2025: 1.0.4-pre.3 - Fix: if the server returns Guid == null, mod generates a new one for the point; total and complete code commenting XD
May-17-2025: 1.0.4-pre.4 - UpdateList is allocated to a separate thread ()*/