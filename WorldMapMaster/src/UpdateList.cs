using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace xtendedMap.src
{
    public partial class WaypointMapLayerFixed
    {
//wanna the final boss? He is there =)
/* issue: When the player moves, the method is called too often 
(possibly to update the point information), which causes freezes 
4 times when server send data to client (source of freezes)
many times when player type text in quick search bar
1 times when ordering of waypoints changed
many times when player choose a waypoint
2 times when player toggle on/off minimap (WHY)*/

/*SOLUTION
 idk now*/

        public void UpdateList() 
        {
            #region beta-test
            List<Waypoint> ownWaypointsCOPY = new List<Waypoint>(ownWaypoints); //copy of ownWaypoints. future feature
            #endregion

            var wpList = this.guiDialogWorldMap.Composers[key]?.GetElement("wplist") 
                as GuiElementDropDown; //display wpList contents in a drop-down list. Now list is empty.
            wpListData.Clear();
            int counter = -1; //to assign a point number            

            EntityPos playerPosition = capi.World.Player.Entity.Pos; //look for the player coordinates

            foreach (Waypoint waypoint in ownWaypoints) //waypoints computing
            {
                if (waypoint.Title.Contains(qsText, StringComparison.InvariantCultureIgnoreCase)) //register-insensitive comparison
                {
                    /* issue of vanilla game:
                    game doesn't generate GUIDs for death points 
                    and plot locations (can be solved by restarting the server)
                    WTF, developers? */
                    if (waypoint.Guid == null)
                    {
                        waypoint.Guid = Guid.NewGuid().ToString();
                        api.Logger.Warning("[xtMap]: Waypoint (" + waypoint.Title + ") GUID regenerated"); // DEBUG ONLY //
                    }
                    /* idea for optimization: calculate the distance only 
                    when player has opened a large world map 
                    need to find a normal way to determine if window is open or not.*/
                    float distance = (float)Math.Sqrt(Math.Pow(playerPosition.X - waypoint.Position.X, 2) + Math.Pow(playerPosition.Z - waypoint.Position.Z, 2));
                    // we check that name of waypoint contains text from search bar, calculated the distance, and now add it to the list
                    wpListData.Add(new WaypointListItem
                    {
                        Id = counter++, //order number of point in list. It's starts with zero 
                        Guid = waypoint.Guid,
                        Title = $"{waypoint.Title} - {distance:F2}m",
                        Distance = distance,                        
                    });
                    // api.Logger.Event("[xtMap]: UpdateList() - waypoint " + waypoint.Guid + " " + waypoint.Title + " added"); // DEBUG ONLY //
                }
            }
            wpListData = wpListOrder switch // sorting 
            {
                "timeasc" => wpListData.OrderBy(o => o.Id).ToList(),
                "timedesc" => wpListData.OrderByDescending(o => o.Id).ToList(),
                "distanceasc" => wpListData.OrderBy(o => o.Distance).ToList(),
                "distancedesc" => wpListData.OrderByDescending(o => o.Distance).ToList(),
                "titleasc" => wpListData.OrderBy(o => o.Title).ToList(),
                "titledesc" => wpListData.OrderByDescending(o => o.Title).ToList(),
            };
            // ñrutch solution: so that we have an empty line in DropDownElement, we insert this string into the list.
            wpListData.Insert(0, new WaypointListItem { Guid = "--1", Title = "- - -", Distance = 0, Id = -1 });

            //first, the GIUD is put, then the names of points to display them in the drop-down list.
            //GUID's needed for correct work with waypoints. Do not simlify this check!!!
            if (wpList != null) wpList.SetList(
                    wpListData.Select(o => o.Guid).ToArray(), 
                    wpListData.Select(o => o.Title).ToArray()
                    );

            api.Logger.Warning("[xtMap]: waypoints list updated, (" + counter++ + ") waypoints added");
        }
    };
};