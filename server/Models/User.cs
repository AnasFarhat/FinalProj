namespace PartnersWebApi.Models
{
    /// <summary>
    /// ייצוג של משתמש מלא במערכת כפי שהוא נשמר בבסיס הנתונים
    /// </summary>
    public class User
    {
        // מזהה ייחודי של המשתמש (Primary Key)
        public int UserId { get; set; }

        // שם מלא של המשתמש
        public string FullName { get; set; }

        // כתובת אימייל המשמשת גם לזיהוי וכניסה
        public string Email { get; set; }

        // סיסמת המשתמש (מומלץ לשמור כ-Hash במימוש הסופי)
        public string Password { get; set; }

        // צבירת נקודות של המשתמש במערכת
        public int? Points { get; set; }

        // עיר המגורים של המשתמש
        public string City { get; set; }

        // תאריך לידה של המשתמש
        public DateTime? BirthDate { get; set; }

        // מצב משפחתי (למשל: רווק/ה, נשוי/ה וכו')
        public string FamilyStatus { get; set; }

        // העדפות אישיות של המשתמש (שמור כמחרוזת או JSON)
        public string Preferences { get; set; }

        // האם המשתמש מוגדר כשותף עסקי
        public bool IsPartner { get; set; }

        // נתיב או URL לתמונת הפרופיל של המשתמש
        public string ImageUrl { get; set; }

        // תפקיד המשתמש במערכת (User / Admin)
        public string Role { get; set; }

        // האם המשתמש חסום לגישה למערכת עקב הפרת תקנון
        public bool IsBlocked { get; set; }
    }

    /// <summary>
    /// מודל נתונים עבור תהליך הרשמה של משתמש חדש
    /// </summary>
    public class RegisterModel
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
   
        public string City { get; set; }
        public DateTime? BirthDate { get; set; }
        public string FamilyStatus { get; set; }

    }

    /// <summary>
    /// מודל נתונים עבור תהליך התחברות (Login)
    /// </summary>
    public class LoginModel
    {
        // אימייל המשתמש המנסה להתחבר
        public string Email { get; set; }

        // הסיסמה שהוזנה בטופס ההתחברות
        public string Password { get; set; }
    }
}