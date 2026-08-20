namespace PartnersWebApi.Models
{
    /// <summary>
    /// מייצג מסלול שנוצר או נשמר על ידי משתמש במערכת.
    /// כולל את פרטי המסלול, מאפייניו ורשימת נקודות הדרך המרכיבות אותו.
    /// </summary>
    public class Route
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Profile { get; set; }
        public double DistanceKm { get; set; }
        public string ShareToken { get; set; }
        public List<Waypoint> Waypoints { get; set; } = new List<Waypoint>();
    }
}