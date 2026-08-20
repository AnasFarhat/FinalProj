namespace PartnersWebApi.Models
{
    /// <summary>
    /// מייצג נקודת עניין גיאוגרפית המשמשת למשחק הטריוויה מבוסס המיקום.
    /// כולל את שם המיקום, הקואורדינטות ורדיוס הזיהוי (Geofence).
    /// </summary>
    public class Location
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double RadiusInMeters { get; set; }
    }
}