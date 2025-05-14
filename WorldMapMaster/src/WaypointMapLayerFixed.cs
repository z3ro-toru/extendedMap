using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace WorldMapMaster.src
{
    public class WaypointListItem
    {
        public string uid { get; set; } = string.Empty; // internal identifier of waypoint
        public string Title { get; set; } = string.Empty; // waypoint name
        public int Id { get; set; } // waypoint number
        public float Distance { get; set; } // distance


    }

    public class WaypointMapLayerFixed : WaypointMapLayer
    {
        private List<WaypointListItem> wpListData = new(); // stores waypoint data on the map
        private readonly string key = "worldmap-layer-waypoints"; // api key
        private readonly ICoreClientAPI capi; 
        private string qsText = string.Empty; // search
        private string wpListOrder = "timeasc"; // sorting (time ascendig by default)
        private GuiDialogWorldMap guiDialogWorldMap;
        private GuiComposer compo;
        private int anti = 2147483647;

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
            UpdateList();

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
                        .AddDropDown(wpListData.Select(o => o.uid).ToArray(),
                                     wpListData.Select(o => o.Title).ToArray(),
                                     0,
                                     onSelectionChanged,
                                     ElementBounds.Fixed(0, 75, 300, 35), "wplist")
                        .AddAutoclearingText(ElementBounds.Fixed(0, 30, 120, 35),
                                             onQSChanged,
                                             null,
                                             "qs")
                        .AddDropDown(new[] {Lang.Get("timeasc"), Lang.Get("timedesc"), Lang.Get("distanceasc"), Lang.Get("distancedesc"), Lang.Get("titleasc"), Lang.Get("titledesc")},
                                     new[] {Lang.Get("timeasc"), Lang.Get("timedesc"), Lang.Get("distanceasc"), Lang.Get("distancedesc"), Lang.Get("titleasc"), Lang.Get("titledesc")},
                                     0,
                                     onOrderingChanged,
                                     ElementBounds.Fixed(125, 30, 125, 35), 
                                     "orderlist")
                    .EndChildElements()
                    .Compose();

            var qsTextElement = this.guiDialogWorldMap.Composers[key].GetElement("qs") as GuiElementTextInput;
            qsTextElement.SetValue(qsText);
            qsTextElement.SetPlaceHolderText("Поиск...");
            var orderList = this.guiDialogWorldMap.Composers[key].GetElement("orderlist") as GuiElementDropDown;
            orderList.SetSelectedValue(wpListOrder);
            this.guiDialogWorldMap.Composers[key].Enabled = false;
        }

        private void onQSChanged(string text)
        {
            qsText = text;
            UpdateList();
        }

        private void onOrderingChanged(string uid, bool selected)
        {
            wpListOrder = uid;
            UpdateList();
        }

        private void UpdateList() //issue: When the player moves, the method is called too often (possibly to update the point information), which causes freezes
            
            {
                var wpList = this.guiDialogWorldMap.Composers[key]?.GetElement("wplist") as GuiElementDropDown; //display wpList contents in a drop-down list
                wpListData.Clear();
                int counter = 0; //to assign a point number

                EntityPos playerPosition = capi.World.Player.Entity.Pos; //player entity position is taken from the client API (possibly a request to the server)

                foreach (Waypoint waypoint in ownWaypoints) //checks the distance for each point
                {
                    if (waypoint.Title.Contains(qsText, StringComparison.InvariantCultureIgnoreCase)) //register-insensitive comparison
                    {
                        float distance = (float)Math.Sqrt(Math.Pow(playerPosition.X - waypoint.Position.X, 2) + Math.Pow(playerPosition.Z - waypoint.Position.Z, 2));
                        wpListData.Add(new WaypointListItem
                        {
                            uid = waypoint.Guid, //is taken from uid. If the UID is empty (null), then... (do processing)
                            Title = $"{waypoint.Title} - {distance:F2}m",
                            Distance = distance,
                            Id = counter++
                        }); //order number of the point in list
                    }
                }

                wpListData = wpListOrder switch
                {
                    "timeasc" => wpListData.OrderBy(o => o.Id).ToList(),
                    "timedesc" => wpListData.OrderByDescending(o => o.Id).ToList(),
                    "distanceasc" => wpListData.OrderBy(o => o.Distance).ToList(),
                    "distancedesc" => wpListData.OrderByDescending(o => o.Distance).ToList(),
                    "titleasc" => wpListData.OrderBy(o => o.Title).ToList(),
                    "titledesc" => wpListData.OrderByDescending(o => o.Title).ToList(),
                };

                wpListData.Insert(0, new WaypointListItem { uid = "--1", Title = "- - -", Distance = 0, Id = -1 });

                wpList?.SetList(wpListData.Select(o => o.uid).ToArray(), wpListData.Select(o => o.Title).ToArray());

            }

        private void onSelectionChanged(string uid, bool selected) //function of GuiElementDropDown
        {
            if (string.IsNullOrEmpty(uid))
            {
                api.Logger.Error("WMM RE: current waypoint uid is null (1st defence).");
                return;
            }
            if (uid.Equals("--1")) return;

            var mapElem = compo.GetElement("mapElem") as GuiElementMap;

            foreach (Waypoint waypoint in ownWaypoints)
            {
                if (waypoint?.Guid == null) //if uid is empty
                {
                    api.Logger.Error("WMM RE: current waypoint uid is null (2nd defence).");
                    continue; //skip
                }

                if (waypoint.Guid.Equals(uid)) //if uid equals waypoint guid
                {
                    BlockPos pos = waypoint.Position.AsBlockPos; //set point coordinates (BlockPos pos - XYZ coordinates of the block)
                    mapElem.CenterMapTo(pos); //center the map on coordinates
                    break;
                }
            }
        }

        public override void OnDataFromServer(byte[] data)
        {
            base.OnDataFromServer(data);
            UpdateList();
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
