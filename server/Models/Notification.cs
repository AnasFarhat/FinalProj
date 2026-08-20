namespace PartnersWebApi.Models
{
    /// <summary>
    /// ייצוג של התראה במערכת הנשמרת בבסיס הנתונים
    /// משמש להצגת היסטוריית התראות אישית לכל משתמש
    /// </summary>
    public class Notification
    {
        // מזהה ייחודי של ההתראה (Primary Key)
        public int Id { get; set; }

        // מזהה המשתמש שאליו מיועדת ההתראה (Foreign Key)
        public int UserId { get; set; }

        // מזהה הטיול הרלוונטי להתראה (אופציונלי - במידה וההתראה קשורה לטיול ספציפי)
        public int? TripId { get; set; }

        // כותרת ההתראה המוצגת למשתמש
        public string Title { get; set; }

        // תוכן ההודעה המפורט של ההתראה
        public string Message { get; set; }

        // סוג ההתראה לצורך עיצוב או סינון (למשל: "General", "Post", "TripUpdate")
        public string Type { get; set; }

        // תאריך ושעת יצירת ההתראה
        public DateTime CreatedAt { get; set; }

        // האם המשתמש כבר צפה/קרא את ההתראה (משמש לניהול ה-Badge ב-UI)
        public bool IsRead { get; set; }
    }
}