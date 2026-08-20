using PartnersWebApi.Models;

namespace PartnersWebApi.Interfaces
{
    /// <summary>
    /// ממשק המגדיר את הפעולות לניהול קשרים ובקשות חברות בין משתמשים במערכת.
    /// </summary>
    public interface IConnectionsRepository
    {
        /// <summary>
        /// שליחת בקשת חברות ממשתמש אחד למשתמש אחר.
        /// </summary>
        /// <param name="senderId">מזהה המשתמש השולח את הבקשה.</param>
        /// <param name="receiverId">מזהה המשתמש המקבל את הבקשה.</param>
        /// <param name="message">הודעה אופציונלית המצורפת לבקשה.</param>
        /// <returns>הודעת סטטוס המתארת את תוצאת הפעולה.</returns>
        string SendRequest(int senderId, int receiverId, string message);

        /// <summary>
        /// אישור או דחייה של בקשת חברות שהתקבלה.
        /// </summary>
        /// <param name="requestId">מזהה בקשת החברות.</param>
        /// <param name="receiverId">מזהה המשתמש המקבל את הבקשה.</param>
        /// <param name="accept">ערך המציין האם לאשר (True) או לדחות (False) את הבקשה.</param>
        /// <returns>True אם הפעולה הצליחה, אחרת False.</returns>
        bool RespondRequest(int requestId, int receiverId, bool accept);

        /// <summary>
        /// שליפת כל בקשות החברות שהתקבלו עבור משתמש מסוים.
        /// </summary>
        /// <param name="userId">מזהה המשתמש.</param>
        /// <returns>רשימת בקשות החברות שהתקבלו.</returns>
        List<ReceivedRequestDto> GetReceivedRequests(int userId);

        /// <summary>
        /// בדיקת סטטוס הקשר בין שני משתמשים.
        /// </summary>
        /// <param name="myId">מזהה המשתמש הנוכחי.</param>
        /// <param name="otherId">מזהה המשתמש השני.</param>
        /// <returns>מחרוזת המתארת את סטטוס הקשר בין המשתמשים.</returns>
        string GetConnectionStatus(int myId, int otherId);

        /// <summary>
        /// שליפת רשימת כל החברים (הקשרים) של משתמש מסוים.
        /// </summary>
        /// <param name="userId">מזהה המשתמש.</param>
        /// <returns>רשימת הקשרים של המשתמש.</returns>
        List<ConnectionDto> GetMyConnections(int userId);
    }
}