/* TEMPLATE!!!
 * This source code contains basic methods that can be used in several places. 
 * To simplify the code, they are all collected here. */
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace xtendedMap.src
{    
    public class MiniFunc:WaypointMapLayerFixed
    {
        public MiniFunc(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink) //preparing to work!
        {
            capi = api as ICoreClientAPI;
        }

        private readonly string key = "worldmap-layer-waypoints"; // api key
        public readonly ICoreClientAPI capi;

        
    }
}