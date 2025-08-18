using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace xtendedMap.src
{
    public partial class WaypointMapLayerFixed
    {
        public override void ComposeDialogExtras(GuiDialogWorldMap guiDialogWorldMap = null, GuiComposer compo = null)
        {            
            this.guiDialogWorldMap = guiDialogWorldMap ?? this.guiDialogWorldMap;
            this.compo = compo ?? this.compo;
            
            UpdateList();
            RefreshList();
            api.Logger.Event("[xtMap]: UpdateList() cause ComposeDialogExtras (line 15)"); // DEBUG ONLY //

            ElementBounds dlgBounds = ElementStdBounds.AutosizedMainDialog
                .WithFixedPosition
                    (
                        (this.compo.Bounds.renderX + this.compo.Bounds.OuterWidth) / RuntimeEnv.GUIScale + 10,
                        this.compo.Bounds.renderY / RuntimeEnv.GUIScale + 120
                    )
                .WithAlignment(EnumDialogArea.RightMiddle);

            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;

            this.guiDialogWorldMap.Composers[key] =
                capi.Gui
                    .CreateCompo(key, dlgBounds)
                    .AddShadedDialogBG(bgBounds, false)
                    .AddDialogTitleBar(Lang.Get("Your waypoints:"), () => { this.guiDialogWorldMap.Composers[key].Enabled = false; })
                    .BeginChildElements(bgBounds)
                        .AddDropDown
                                    (
                                        wpListData.Select(o => o.Guid).ToArray(),
                                        wpListData.Select(o => o.Title).ToArray(),
                                        0,
                                        onSelectionChanged,
                                        ElementBounds.Fixed(0, 75, 300, 35), "wplist"
                                    )
                        .AddAutoclearingText //custom element, see GuiElementAutoclearingText.cs
                                    (
                                        ElementBounds.Fixed(0, 30, 120, 35),
                                        onQSChanged,
                                        null,
                                        "qs"
                                    )
                        .AddDropDown // that's a nightmare!
                                    (
                                        new[] { Lang.Get("timeasc"), Lang.Get("timedesc"), Lang.Get("distanceasc"), Lang.Get("distancedesc"), Lang.Get("titleasc"), Lang.Get("titledesc") },
                                        new[] { Lang.Get("timeasc"), Lang.Get("timedesc"), Lang.Get("distanceasc"), Lang.Get("distancedesc"), Lang.Get("titleasc"), Lang.Get("titledesc") },
                                        0,
                                        onOrderingChanged,
                                        ElementBounds.Fixed(125, 30, 125, 35),
                                        "orderlist"
                                    )
                    .EndChildElements()
                    .Compose();

            var qsTextElement = this.guiDialogWorldMap.Composers[key].GetElement("qs") as GuiElementTextInput;
            var orderList = this.guiDialogWorldMap.Composers[key].GetElement("orderlist") as GuiElementDropDown;

            qsTextElement.SetValue(qsText);
            qsTextElement.SetPlaceHolderText(Lang.Get("Search..."));            
            orderList.SetSelectedValue(wpListOrder);

            this.guiDialogWorldMap.Composers[key].Enabled = false;
        }
    };
};