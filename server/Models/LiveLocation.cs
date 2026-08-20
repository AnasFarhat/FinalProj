namespace PartnersWebApi.Models
{
    /// <summary>
    /// מייצג את מיקומו העדכני של משתמש במערכת.
    /// משמש לשיתוף מיקום בזמן אמת ולהצגה על גבי המפה.
    /// </summary>
    public class LiveLocation
    {
        public int UserId { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// אובייקט בקשה לעדכון המיקום הנוכחי של המשתמש.
    /// </summary>
    public class LiveLocationDto
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
    }
}