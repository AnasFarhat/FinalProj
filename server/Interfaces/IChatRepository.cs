using PartnersWebApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PartnersWebApi.Interfaces
{
    /// <summary>
    /// ממשק המגדיר את פעולות הגישה לנתונים עבור מערכת הצ'אט והטיולים
    /// </summary>
    public interface IChatRepository
    {
        /// <summary>
        /// שמירת הודעת משתמש ותגובת הבוט במסד הנתונים
        /// </summary>
        /// <param name="userId">מזהה המשתמש</param>
        /// <param name="message">הודעת המשתמש</param>
        /// <param name="response">תגובת הבוט</param>
        /// <param name="intent">הכוונה שזוהתה (למשל AI או Intent לוגיסטי)</param>
        /// <param name="sessionId">מזהה הסשן לקיבוץ השיחה</param>
        void SaveChat(int userId, string message, string response, string intent, string sessionId);

        /// <summary>
        /// שליפת מידע מפורט על הטיולים של המשתמש במבנה דינמי (Dictionary)
        /// שימושי להעברת הקשר (Context) עשיר לבינה המלאכותית
        /// </summary>
        List<Dictionary<string, object>> GetUserTripsDetailed(int userId);

        /// <summary>
        /// שליפת רשימת כל סשני השיחה של משתמש ספציפי (עבור תצוגת ההיסטוריה ב-Sidebar)
        /// </summary>
        Task<List<object>> GetUserSessionsAsync(int userId);
       
        /// <summary>
        /// שליפת היסטוריית ההודעות המלאה עבור סשן שיחה ספציפי
        /// </summary>
        Task<List<object>> GetChatHistoryBySessionAsync(int userId, string sessionId);
    }
}