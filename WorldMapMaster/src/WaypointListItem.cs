namespace xtendedMap.src
{
    public class WaypointListItem
    {
        public string Guid { get; set; } = string.Empty; // internal identifier of waypoint/identificator like {xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx}
        public string Title { get; set; } = string.Empty; // waypoint name
        public int Id { get; set; } // waypoint number for sorting by time of creating
        public float Distance { get; set; } // distance
        public bool Pinned { get; set; } //future feature
    }
};