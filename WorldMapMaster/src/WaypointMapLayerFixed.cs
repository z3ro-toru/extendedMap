using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace WorldMapMaster.src
{
    public class WaypointMapLayerFixed : WaypointMapLayer
    {
        List<string> valData = new List<string>();
        List<string> dispData = new List<string>();
        readonly string key = "worldmap-layer-waypoints";
        private ICoreClientAPI capi;
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

            valData.Clear();
            dispData.Clear();

            valData.Add("--1");
            dispData.Add("None");

            foreach (Waypoint waypoint in ownWaypoints)
            {
                valData.Add(waypoint.Guid);
                dispData.Add(waypoint.Title);
            }

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
                        .AddDropDown(valData.ToArray(), dispData.ToArray(), 0, onSelectionChanged, ElementBounds.Fixed(0, 30, 160, 35), "wplist")
                    .EndChildElements()
                    .Compose()
            ;
            this.guiDialogWorldMap.Composers[key].Enabled = false;
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
            GuiElementDropDown wpList = this.guiDialogWorldMap.Composers[key].GetElement("wplist") as GuiElementDropDown;

            valData.Clear();
            dispData.Clear();

            valData.Add("--1");
            dispData.Add("None");

            foreach (Waypoint waypoint in ownWaypoints)
            {
                valData.Add(waypoint.Guid);
                dispData.Add(waypoint.Title);
            }

            wpList.SetList(valData.ToArray(), dispData.ToArray());
            //ComposeDialogExtras();
        }

        public override void OnMapClosedClient()
        {
            base.OnMapClosedClient();
        }
    }
}
