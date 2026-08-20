namespace PartnersWebApi.Models
{
    /// <summary>
    /// אובייקט בקשה לשמירת מסלול חדש במערכת.
    /// כולל את פרטי המסלול ורשימת נקודות הדרך המרכיבות אותו.
    /// </summary>
    public class RouteDto
    {
        public string Name { get; set; }
        public string Profile { get; set; }
        public double DistanceKm { get; set; }
        public List<WaypointDto> Waypoints { get; set; }
    }

    /// <summary>
    /// מייצג נקודת דרך במסלול.
    /// כולל את מיקום הנקודה ותווית אופציונלית לזיהויה.
    /// </summary>
    public class WaypointDto
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
        public string Label { get; set; }
    }
}