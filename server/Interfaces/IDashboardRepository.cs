using PartnersWebApi.Models;

namespace PartnersWebApi.Interfaces
{
    /// <summary>
    /// ממשק המגדיר את הפעולות הנדרשות עבור לוח הבקרה הניהולי (Admin Dashboard)
    /// מספק נתונים סטטיסטיים, מדדי ביצוע (KPIs) וכלי ניהול משתמשים ותוכן
    /// </summary>
    public interface IDashboardRepository
    {
        /// <summary>
        /// שליפת מדדי ביצוע כלליים של המערכת (כמות משתמשים, טיולים פעילים וכו')
        /// </summary>
        /// <returns>אובייקט המכיל נתונים סטטיסטיים מסוכמים</returns>
        object GetGeneralKPIs();

    

        /// <summary>
        /// שליפת סיכום ניתוח סנטימנט המבוסס על משובים מילוליים וקוליים
        /// נותן תמונת מצב על שביעות רצון המשתמשים בחתך רוחבי
        /// </summary>
        /// <returns>אובייקט המרכז את התפלגות הסנטימנט (חיובי/שלילי/ניטרלי)</returns>
        object GetSentimentSummary();

       

        /// <summary>
        /// שליפת רשימה מלאה של כל המשתמשים במערכת לצורכי ניהול
        /// </summary>
        /// <returns>אוסף של אובייקטים המייצגים את משתמשי המערכת</returns>
        IEnumerable<object> GetUsersList();

        /// <summary>
        /// שינוי סטטוס חסימה של משתמש (חסימה או שחרור מחסימה)
        /// </summary>
        /// <param name="userId">מזהה המשתמש לביצוע הפעולה</param>
        /// <returns>True אם הפעולה הצליחה, אחרת False</returns>
        bool ToggleUserBlock(int userId);

        /// <summary>
        /// שליפת דיווחים על תוכן פוגעני, מקובצים לפי פוסטים
        /// מאפשר למנהל לראות אילו פוסטים צברו הכי הרבה תלונות
        /// </summary>
        /// <returns>אוסף של אובייקטי DTO המכילים נתוני דיווח מקובצים</returns>
        IEnumerable<GroupedReportDto> GetGroupedReports();

        /// <summary>
        /// שינוי סטטוס נראות של פוסט (הסתרה מהפיד הציבורי או חשיפה מחדש)
        /// משמש לטיפול בתוכן שדווח כפוגעני
        /// </summary>
        /// <param name="postId">מזהה הפוסט לביצוע הפעולה</param>
        /// <returns>True אם הפעולה הצליחה, אחרת False</returns>
        bool TogglePostVisibility(int postId);
        object GetAttendanceOverview();
    }
}