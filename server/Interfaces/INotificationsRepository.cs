using PartnersWebApi.Models;

namespace PartnersWebApi.Interfaces
{
    /// <summary>
    /// ממשק המגדיר את הפעולות לניהול התראות וטוקני FCM במערכת.
    /// </summary>
    public interface INotificationsRepository
    {
        /// <summary>
        /// שליפת כל ההתראות של משתמש מסוים.
        /// </summary>
        /// <param name="userId">מזהה המשתמש.</param>
        /// <returns>אוסף ההתראות המשויכות למשתמש.</returns>
        IEnumerable<object> GetByUserId(int userId);

        /// <summary>
        /// סימון התראה כנקראה.
        /// </summary>
        /// <param name="id">מזהה ההתראה.</param>
        /// <returns>True אם פעולת העדכון הצליחה, אחרת False.</returns>
        bool MarkAsRead(int id);

        /// <summary>
        /// שליחת התראה למשתמשים במערכת.
        /// </summary>
        /// <param name="req">אובייקט המכיל את פרטי ההתראה לשליחה.</param>
        /// <returns>True אם ההתראה נשלחה בהצלחה, אחרת False.</returns>
        bool SendNotification(SendNotificationRequest req);

        /// <summary>
        /// שמירת טוקן FCM של מכשיר לצורך שליחת התראות Push.
        /// </summary>
        /// <param name="userId">מזהה המשתמש.</param>
        /// <param name="fcmToken">טוקן ה-FCM של המכשיר.</param>
        /// <returns>True אם השמירה הצליחה, אחרת False.</returns>
        bool SaveFcmToken(int userId, string fcmToken);

        /// <summary>
        /// שליפת כל טוקני ה-FCM הרלוונטיים לצורך שליחת התראות.
        /// </summary>
        /// <param name="tripId">
        /// מזהה הטיול. אם הערך הוא Null, יישלפו כל הטוקנים הרלוונטיים בהתאם ללוגיקה של המערכת.
        /// </param>
        /// <returns>אוסף טוקני FCM לשליחת התראות Push.</returns>
        IEnumerable<string> GetFcmTokens(int? tripId);
    }
}