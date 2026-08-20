using PartnersWebApi.Models;

namespace PartnersWebApi.Interfaces
{
    /// <summary>
    /// ממשק המגדיר את הפעולות הנדרשות לניהול תוכן קהילתי ואינטראקציות חברתיות
    /// מטפל בפוסטים, תגובות, לייקים ומנגנוני דיווח על תוכן
    /// </summary>
    public interface ICommunityRepository
    {
        /// <summary>
        /// שליפת כל הפוסטים בקהילה עבור פיד המשתמש
        /// </summary>
        /// <param name="userId">מזהה המשתמש הצופה (לצורך סימון לייקים אישיים)</param>
        /// <returns>אוסף של אובייקטים המייצגים פוסטים כולל נתוני כותב, מדיה ואינטראקציות</returns>
        IEnumerable<object> GetCommunityPosts(int userId);

        /// <summary>
        /// יצירת פוסט חדש בקהילה
        /// </summary>
        /// <param name="model">מודל המכיל את תוכן הפוסט, תמונות ושיוך אופציונלי לטיול</param>
        /// <returns>מזהה הפוסט החדש שנוצר בבסיס הנתונים</returns>
        int CreatePost(CreatePostRequest model);

        /// <summary>
        /// ביצוע פעולת Toggle ללייק (הוספה אם לא קיים, הסרה אם כבר קיים)
        /// </summary>
        /// <param name="like">מודל המכיל את מזהה הפוסט והמשתמש המבצע</param>
        /// <returns>מחרוזת המציינת את תוצאת הפעולה (למשל: "Liked" או "Unliked")</returns>
        string ToggleLike(LikeInput like);

        /// <summary>
        /// הוספת תגובה חדשה לפוסט קיים
        /// </summary>
        /// <param name="comment">מודל המכיל את מזהה הפוסט, המשתמש ותוכן התגובה</param>
        /// <returns>True אם התגובה נוספה בהצלחה, אחרת False</returns>
        bool AddComment(CommentInput comment);

        /// <summary>
        /// מחיקת פוסט מהמערכת
        /// </summary>
        /// <param name="postId">מזהה הפוסט למחיקה</param>
        /// <param name="userId">מזהה המשתמש המבקש (לצורך אימות הרשאות בעלים)</param>
        /// <returns>קוד תוצאה (למשל: 200 להצלחה, 401 לחוסר הרשאה, 404 אם לא נמצא)</returns>
        int DeletePost(int postId, int userId, bool isAdmin);

        /// <summary>
        /// יצירת דיווח על תוכן פוגעני (פוסט או תגובה)
        /// </summary>
        /// <param name="report">מודל הבקשה הכולל את הסיבה ופרטי התוכן המדווח</param>
        /// <returns>True אם הדיווח נרשם בהצלחה בבסיס הנתונים, אחרת False</returns>
        bool ReportContent(CreateReportRequest report);

        /// <summary>
        /// מחיקת תגובה ספציפית מהמערכת (בדרך כלל על ידי מנהל או בעל התגובה)
        /// </summary>
        /// <param name="commentId">מזהה התגובה למחיקה</param>
        /// <returns>True אם המחיקה בוצעה בהצלחה, אחרת False</returns>
        bool DeleteComment(int commentId);
    }
}