namespace PartnersWebApi.Models
{
    /// <summary>
    /// אובייקט המכיל את מיקומו הנוכחי של המשתמש.
    /// משמש להעברת נתוני מיקום לצורך זיהוי נקודות עניין במשחק הטריוויה.
    /// </summary>
    public class UserLocationDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}