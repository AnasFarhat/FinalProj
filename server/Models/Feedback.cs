namespace PartnersWebApi.Models
{
    /// <summary>
    /// מודל המייצג משוב (Feedback) שמשתמש משאיר לאחר טיול
    /// משלב דירוג מספרי, תגיות בחירה חופשית, וניתוח חוויית משתמש
    /// </summary>
    public class Feedback
    {
        // מזהה ייחודי של המשוב (אופציונלי ביצירה, Primary Key בבסיס הנתונים)
        public int? FeedbackId { get; set; }

        // מזהה המשתמש שכתב את המשוב (Foreign Key)
        public int UserId { get; set; }

        // מזהה הטיול עליו ניתן המשוב (Foreign Key)
        public int TripId { get; set; }

        // דירוג איכות ההדרכה (למשל: בסולם של 1-5)
        public int GuideRating { get; set; }

        // דירוג איכות המסלול והחוויה הכללית (למשל: בסולם של 1-5)
        public int TrackRating { get; set; }

        // רשימת תגיות חיוביות שנבחרו (שמור כמחרוזת מופרדת בפסיקים או JSON)
        public string? GoodTags { get; set; }

        // רשימת תגיות שליליות או נקודות לשיפור שנבחרו
        public string? BadTags { get; set; }

        // תוכן המשוב בטקסט חופשי שהמשתמש הזין
        public string? FreeText { get; set; }

        // סטטוס ניתוח סנטימנט (למשל: Positive, Negative, Neutral) המופק מהטקסט או הקול
        public string? SentimentStatus { get; set; }

        // נתיב פיזי בשרת או URL לקובץ הקלטה קולית שהמשתמש צירף למשוב
        public string? VoiceFilePath { get; set; }

        // תאריך ושעת יצירת המשוב (ברירת מחדל היא זמן היצירה הנוכחי)
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}