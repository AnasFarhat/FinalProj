namespace PartnersWebApi.Models
{
    /// <summary>
    /// ייצוג של רישום משתמש לטיול בבסיס הנתונים
    /// מחלקה זו מקשרת בין ישות המשתמש לישות הטיול (Many-to-Many)
    /// </summary>
    public class UserTrip
    {
        // מזהה ייחודי של הרישום (Primary Key)
        public int RegistrationId { get; set; }

        // מזהה המשתמש שנרשם לטיול (Foreign Key)
        public int UserId { get; set; }

        // מזהה הטיול אליו המשתמש נרשם (Foreign Key)
        public int TripId { get; set; }

        // מספר המבוגרים הכלולים בהזמנה זו
        public int AdultsCount { get; set; }

        // מספר הילדים הכלולים בהזמנה זו
        public int ChildrenCount { get; set; }

        // סטטוס ההרשמה (למשל: "Confirmed", "Pending", "Cancelled")
        public string Status { get; set; }

        // דירוג שהמשתמש נתן לטיול לאחר סיומו (1-5)
        public int? Rating { get; set; }
        public bool? AttendanceConfirmation { get; set; }
    }

    /// <summary>
    /// מודל תצוגה (DTO) המשלב נתוני טיול יחד עם נתוני ההזמנה של המשתמש
    /// משמש להצגת היסטוריית טיולים או "הטיולים שלי" בממשק המשתמש
    /// </summary>
    public class UserTripInfo
    {
        // שם הטיול מתוך טבלת הטיולים
        public string Title { get; set; }

        // תאריך הטיול
        public DateTime TripDate { get; set; }

        // מיקום הטיול
        public string Location { get; set; }

        // מספר המבוגרים שנרשמו בהזמנה הספציפית
        public int Adults { get; set; }

        // מספר הילדים שנרשמו בהזמנה הספציפית
        public int Children { get; set; }

        // סטטוס ההזמנה הנוכחי
        public string Status { get; set; }

        // רמת הקושי של הטיול (למידע מהיר למשתמש)
        public string Difficulty { get; set; }

        // הציוד הנדרש לטיול (תזכורת למשתמש לקראת היציאה)
        public string Equipment { get; set; }
    }
}