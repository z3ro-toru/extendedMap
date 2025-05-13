using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using static System.Net.Mime.MediaTypeNames;

namespace WorldMapMaster.src
{
    public class WaypointListItem
    {
        public string code = "";
        public string title = "";
        public int id = 0;
        public float distance = 0;
    }
    public class WaypointMapLayerFixed : WaypointMapLayer
    {
        /*
        List<string> valData = new List<string>();
        List<string> dispData = new List<string>();
        */
        List<WaypointListItem> wpListData = new List<WaypointListItem>();

        readonly string key = "worldmap-layer-waypoints";
        private ICoreClientAPI capi;
        private string qsText = "";
        private string wpListOrder = "timeasc";
        private GuiDialogWorldMap guiDialogWorldMap;
        private GuiComposer compo;


        public WaypointMapLayerFixed(ICoreAPI api, IWorldMapManager mapSink) 
            : base(api, mapSink)
        {
            WorldMapMasterModSystem.Api.Logger.Event("[worldmapmaster] WaypointMapLayerFixed instanciated. Side: " + WorldMapMasterModSystem.Api.Side);
            capi = api as ICoreClientAPI;
        }

        public override void ComposeDialogExtras(GuiDialogWorldMap guiDialogWorldMap = null, GuiComposer compo = null)
        {
            if(guiDialogWorldMap != null)
                this.guiDialogWorldMap = guiDialogWorldMap;
            if(compo != null)
                this.compo = compo;

            UpdateList();

            ElementBounds dlgBounds =
                ElementStdBounds.AutosizedMainDialog
                .WithFixedPosition(
                    (this.compo.Bounds.renderX + this.compo.Bounds.OuterWidth) / RuntimeEnv.GUIScale + 10,
                    this.compo.Bounds.renderY / RuntimeEnv.GUIScale + 120
                )
                .WithAlignment(EnumDialogArea.None)
            ;

            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;

            this.guiDialogWorldMap.Composers[key] =
                capi.Gui
                    .CreateCompo(key, dlgBounds)
                    .AddShadedDialogBG(bgBounds, false)
                    .AddDialogTitleBar(Lang.Get("Your waypoints:"), () => { this.guiDialogWorldMap.Composers[key].Enabled = false; })
                    .BeginChildElements(bgBounds)
                        .AddDropDown(wpListData.Select(o => o.code).ToArray(), wpListData.Select(o => o.title).ToArray(), 0, onSelectionChanged, ElementBounds.Fixed(0, 75, 300, 35), "wplist")
                        .AddAutoclearingText(ElementBounds.Fixed(0, 30, 120, 35), onQSChanged, null, "qs")
                        .AddDropDown(new string[] { "timeasc", "timedesc", "distanceasc", "distancedesc", "titleasc", "titledesc" }, new string[] { "Время (возраст.)", "Время (убыв.)", "Расстояние (возраст.)", "Расстояние (убыв.)", "Название (а->я)", "Название (я->a)" }, 0, onOrderingChanged, ElementBounds.Fixed(125, 30, 125, 35), "orderlist")
                    .EndChildElements()
                    .Compose()
            ;
            GuiElementTextInput qsTextElement = this.guiDialogWorldMap.Composers[key].GetElement("qs") as GuiElementTextInput;
            qsTextElement.SetValue(qsText);
            qsTextElement.SetPlaceHolderText("Поиск...");
            GuiElementDropDown orderList = this.guiDialogWorldMap.Composers[key].GetElement("orderlist") as GuiElementDropDown;
            orderList.SetSelectedValue(wpListOrder);
            this.guiDialogWorldMap.Composers[key].Enabled = false;
        }

        private void onQSChanged(string text)
        {
            qsText = text;
            UpdateList();
        }
        private void onOrderingChanged(string code, bool selected)
        {
            wpListOrder = code;
            UpdateList();
        }
        private void UpdateList()
        {
            GuiElementDropDown wpList = null;

            if(this.guiDialogWorldMap.Composers[key] != null)
                wpList = this.guiDialogWorldMap.Composers[key].GetElement("wplist") as GuiElementDropDown;

            /*
            valData.Clear();
            dispData.Clear();
            
            valData.Add("--1");
            dispData.Add("None");

            foreach (Waypoint waypoint in ownWaypoints)
            {
                if (waypoint.Title.Contains(qsText, StringComparison.InvariantCultureIgnoreCase))
                {
                    valData.Add(waypoint.Guid);
                    dispData.Add(waypoint.Title);
                }
            }
            */

            wpListData.Clear();
            int counter = 0;

            EntityPos playerPosition = capi.World.Player.Entity.Pos;

            foreach (Waypoint waypoint in ownWaypoints)
            {
                if (waypoint.Title.Contains(qsText, StringComparison.InvariantCultureIgnoreCase))
                {
                    float distance = (float)Math.Sqrt(Math.Pow(playerPosition.X - waypoint.Position.X, 2) + Math.Pow(playerPosition.Z - waypoint.Position.Z, 2));
                    wpListData.Add(new WaypointListItem() { code = waypoint.Guid, title = $"{waypoint.Title} - {distance:F2}m", distance = distance, id = counter++ });
                }
            }

            switch(wpListOrder) /* sorting of waypoints */
            {
                case "timedesc":
                    wpListData = wpListData.OrderByDescending(o => o.id).ToList();
                    break;
                case "distanceasc":
                    wpListData = wpListData.OrderBy(o => o.distance).ToList();
                    break;
                case "distancedesc":
                    wpListData = wpListData.OrderByDescending(o => o.distance).ToList();
                    break;
                case "timeasc":
                    break;
                case "titledesc":
                    wpListData = wpListData.OrderByDescending(o => o.title).ToList();
                    break;
                case "titleasc":
                    wpListData = wpListData.OrderBy(o => o.title).ToList();
                    break;
            }

            wpListData.Insert(0, new WaypointListItem() { code = "--1", title = "- - -", distance = 0, id = -1 });

            if (wpList != null)
                wpList.SetList(wpListData.Select(o => o.code).ToArray(), wpListData.Select(o => o.title).ToArray());

        }
        private void onSelectionChanged(string code, bool selected)
        {
            if (code.Equals("--1")) return;

            GuiElementMap mapElem = compo.GetElement("mapElem") as GuiElementMap;

            foreach (Waypoint waypoint in ownWaypoints)
            {
                if (waypoint.Guid.Equals(code))
                {
                    BlockPos pos = waypoint.Position.AsBlockPos;
                    mapElem.CenterMapTo(pos);
                    break;
                }
            }
        }

        public override void OnDataFromServer(byte[] data)
        {
            base.OnDataFromServer(data);
            UpdateList();
            //ComposeDialogExtras();
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
