namespace PartnersWebApi.Models
{
    /// <summary>
    /// מודל נתונים עבור בקשת שליחת התראה מהשרת למשתמשים
    /// משמש בדרך כלל את ממשק המנהל לשליחת עדכונים כלליים או ספציפיים לטיול
    /// </summary>
    public class SendNotificationRequest
    {
        // כותרת ההתראה (למשל: "עדכון חשוב", "הטיול בוטל")
        public string Title { get; set; }

        // גוף ההתראה הכולל את הפירוט המלא של ההודעה
        public string Message { get; set; }

        /// <summary>
        /// מזהה הטיול אליו ההתראה קשורה.
        /// במידה והערך הוא Null, ההתראה תישלח לכלל המשתמשים במערכת.
        /// במידה וקיים מזהה, ההתראה תישלח רק למשתמשים הרשומים לאותו טיול.
        /// </summary>
        public int? TripId { get; set; }

        // סוג ההתראה (למשל: "General", "Update", "Emergency") - ברירת מחדל היא "General"
        public string Type { get; set; } = "General";
    }
}