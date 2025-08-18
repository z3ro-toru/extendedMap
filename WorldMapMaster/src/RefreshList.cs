using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace xtendedMap.src
{
    public partial class WaypointMapLayerFixed
    {
        //to take the load off updateList()
        Dictionary<string, string> WaypointsQuickList = new Dictionary<string, string>();

        private void RefreshList()
        {    
            foreach (Waypoint waypoint in ownWaypoints)
            {
                WaypointsQuickList.Add(waypoint.Guid, Title);
            }
            api.Logger.Warning("[xtMap]: waypoints list refreshed");
        }

        public void CompareToDict()
        {
            foreach (Waypoint waypoint in ownWaypoints)
            {
                switch (WaypointsQuickList.ContainsKey(waypoint.Guid))
                {
                    case true: { api.Logger.Warning("[xtdmap]: waypoint already added to Dict!"); break; }
                    case false: { RefreshList(); break; }
                }
            }
        }
    }
}