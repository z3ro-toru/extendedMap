using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.API.Common.Entities;

namespace WorldMapMaster.src.harmony
{
    // Add check for a "Delete waypoint" hotkey
    [HarmonyPatch(typeof(WaypointMapComponent), "OnMouseMove")]
    public class WaypointMapComponent_OnMouseMove
    {
        public static void Postfix(ICoreClientAPI ___capi, bool ___mouseOver, Waypoint ___waypoint, int ___waypointIndex, MouseEvent args, GuiElementMap mapElem, StringBuilder hoverText)
        {
            Vec2f viewPos = new Vec2f();
            mapElem.TranslateWorldPosToViewPos(___waypoint.Position, ref viewPos);

            double x = viewPos.X + mapElem.Bounds.renderX;
            double y = viewPos.Y + mapElem.Bounds.renderY;

            if (___waypoint.Pinned)
            {
                mapElem.ClampButPreserveAngle(ref viewPos, 2);
                x = viewPos.X + mapElem.Bounds.renderX;
                y = viewPos.Y + mapElem.Bounds.renderY;

                x = (float)GameMath.Clamp(x, mapElem.Bounds.renderX + 2, mapElem.Bounds.renderX + mapElem.Bounds.InnerWidth - 2);
                y = (float)GameMath.Clamp(y, mapElem.Bounds.renderY + 2, mapElem.Bounds.renderY + mapElem.Bounds.InnerHeight - 2);
            }
            double dX = args.X - x;
            double dY = args.Y - y;

            var size = RuntimeEnv.GUIScale * 8;
            if (___mouseOver = Math.Abs(dX) < size && Math.Abs(dY) < size)
            {
                WorldMapMasterModSystem.wpIndex = ___waypointIndex;
                EntityPos playerPosition = ___capi.World.Player.Entity.Pos;
                double distance = Math.Sqrt(Math.Pow(playerPosition.X - ___waypoint.Position.X, 2) + Math.Pow(playerPosition.Z - ___waypoint.Position.Z, 2));
                string text = $"{distance:F2} m";
                string currentHover = hoverText.ToString().Trim();
                if (!currentHover.EndsWith(text)) 
                    hoverText.AppendLine(text);
            }
        }
    }

    // Reset last visited waypoint
    [HarmonyPatch(typeof(WaypointMapLayer), "OnMouseMoveClient")]
    public class WaypointMapLayer_OnMouseMoveClient
    {
        public static bool Prefix()
        {
            WorldMapMasterModSystem.wpIndex = -1;
            
            return true;
        }
    }

    // Set internal WorldPos (the only way found)
    [HarmonyPatch(typeof(GuiDialogAddWayPoint), "TryOpen")]
    public class GuiDialogAddWayPoint_TryOpen
    {
        public static bool Prefix(ref Vec3d ___WorldPos)
        {
            if(___WorldPos == null)
                ___WorldPos = WorldMapMasterModSystem.newWpPos;

            return true;
        }
    }

    // Change WaypointMapLayer with the fixed one
    [HarmonyPatch(typeof(WorldMapManager), "RegisterDefaultMapLayers")]
    public class WorldMapManager_RegisterDefaultMapLayers
    {
        /*
        public static bool Prefix(WorldMapManager __instance)
        {
            __instance.RegisterMapLayer<ChunkMapLayer>("chunks", 0);
            __instance.RegisterMapLayer<PlayerMapLayer>("players", 0.5);
            __instance.RegisterMapLayer<EntityMapLayer>("entities", 0.5);
            __instance.RegisterMapLayer<WaypointMapLayerFixed>("waypoints", 1);

            return false;
        }
        */
        
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo m_RegisterMapLayer = AccessTools.Method(typeof(WorldMapManager), "RegisterMapLayer", new Type[] { typeof(string), typeof(double) }, new Type[] { typeof(WaypointMapLayer) });
            MethodInfo m_RegisterMapLayerFixed = AccessTools.Method(typeof(WorldMapManager), "RegisterMapLayer", new Type[] { typeof(string), typeof(double) }, new Type[] { typeof(WaypointMapLayerFixed) });

            foreach (CodeInstruction instruction in instructions)
            {
                if(instruction.Calls(m_RegisterMapLayer))
                {
                    yield return new CodeInstruction(OpCodes.Call, m_RegisterMapLayerFixed);
                    continue;
                }
                yield return instruction;
            }
        }
        
    }


}
