namespace PartnersWebApi.Models
{
    /// <summary>
    /// מודל נתונים עבור בקשת יצירת פוסט חדש בקהילה
    /// </summary>
    public class CreatePostRequest
    {
        // מזהה המשתמש המפרסם את הפוסט
        public int UserId { get; set; }

        // תוכן הטקסט של הפוסט (מאותחל למחרוזת ריקה למניעת Null)
        public string Content { get; set; } = string.Empty;

        // רשימת נתיבים לתמונות שצורפו לפוסט (תואם לסכימת בסיס הנתונים)
        public List<string> ImageUrls { get; set; } = new List<string>();

        // מזהה טיול אופציונלי לשיוך הפוסט לחוויית טיול ספציפית
        public int? TripId { get; set; }
    }

    /// <summary>
    /// מודל נתונים לביצוע פעולת לייק (Like) על פוסט
    /// </summary>
    public class LikeInput
    {
        public int PostId { get; set; }
        public int UserId { get; set; }
    }

    /// <summary>
    /// מודל נתונים להוספת תגובה חדשה לפוסט
    /// </summary>
    public class CommentInput
    {
        public int PostId { get; set; }
        public int UserId { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// מודל נתונים לשליחת דיווח על תוכן פוגעני (פוסט או תגובה)
    /// </summary>
    public class CreateReportRequest
    {
        // מזהה הפוסט המדווח (במידה והדיווח הוא על פוסט)
        public int? PostId { get; set; }

        // מזהה התגובה המדווחת (במידה והדיווח הוא על תגובה ספציפית)
        public int? CommentId { get; set; }

        // מזהה המשתמש ששלח את הדיווח
        public int UserId { get; set; }

        // קטגוריית הדיווח (למשל: ספאם, תוכן פוגעני, הטרדה)
        public string ReasonCategory { get; set; }

        // פירוט נוסף בטקסט חופשי מהמשתמש המדווח
        public string CustomReason { get; set; }
    }

    /// <summary>
    /// מודל תצוגה עבור המנהל המקבץ את כל הדיווחים עבור פוסט ספציפי
    /// משמש לניהול יעיל של תוכן בממשק הניהול
    /// </summary>
    public class GroupedReportDto
    {
        public int PostId { get; set; }
        public string PostContent { get; set; }
        public string PostAuthor { get; set; }

        // האם הפוסט מוסתר כרגע מהפיד הציבורי עקב ריבוי דיווחים
        public bool IsHidden { get; set; }

        // סך כל כמות הדיווחים שהצטברו לפוסט זה
        public int TotalReports { get; set; }

        // רשימה מפורטת של כל דיווח ודיווח שנשלח עבור הפוסט
        public List<ReportDetailDto> ReportDetails { get; set; }
    }

    /// <summary>
    /// ייצוג של פרטי דיווח בודד בתוך רשימת דיווחים מקובצת
    /// </summary>
    public class ReportDetailDto
    {
        // שם המשתמש שדיווח
        public string ReporterName { get; set; }

        // הקטגוריה שנבחרה לדיווח
        public string ReasonCategory { get; set; }

        // הערות נוספות שכתב המדווח
        public string CustomReason { get; set; }

        // מועד שליחת הדיווח
        public DateTime CreatedAt { get; set; }
    }
}