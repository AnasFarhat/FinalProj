namespace PartnersWebApi.Models
{
    /// <summary>
    /// מייצג נקודת דרך (Waypoint) במסלול.
    /// כל נקודה כוללת מיקום גיאוגרפי, סדר הופעה במסלול ותווית אופציונלית.
    /// </summary>
    public class Waypoint
    {
        public int Id { get; set; }
        public int RouteId { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
        public string Label { get; set; }
        public int Sequence { get; set; }
    }
}