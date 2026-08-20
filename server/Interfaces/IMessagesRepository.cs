using PartnersWebApi.Models;
namespace PartnersWebApi.Interfaces
{
    /// <summary>
    /// ממשק המגדיר את הפעולות לניהול הודעות פרטיות ושיחות בין משתמשים במערכת.
    /// </summary>
    public interface IMessagesRepository
    {
        /// <summary>
        /// שליחת הודעה פרטית ממשתמש אחד למשתמש אחר.
        /// </summary>
        /// <param name="senderId">מזהה המשתמש השולח.</param>
        /// <param name="receiverId">מזהה המשתמש המקבל.</param>
        /// <param name="content">תוכן ההודעה.</param>
        /// <returns>הודעת סטטוס המתארת את תוצאת פעולת השליחה.</returns>
        string SendMessage(int senderId, int receiverId, string content);
        /// <summary>
        /// שליפת היסטוריית השיחה בין שני משתמשים.
        /// </summary>
        /// <param name="myId">מזהה המשתמש הנוכחי.</param>
        /// <param name="otherId">מזהה המשתמש השני.</param>
        /// <returns>רשימת ההודעות שהוחלפו בין המשתמשים.</returns>
        List<MessageDto> GetConversation(int myId, int otherId);
        /// <summary>
        /// שליפת רשימת כל השיחות של משתמש מסוים.
        /// </summary>
        /// <param name="userId">מזהה המשתמש.</param>
        /// <returns>רשימת השיחות של המשתמש, כולל מידע על כל שיחה.</returns>
        List<ChatListItemDto> GetMyChats(int userId);
        /// <summary>
        /// מחיקת הודעה בודדת מתוך שיחה. רק המשתמש ששלח את ההודעה רשאי למחוק אותה.
        /// </summary>
        /// <param name="myId">מזהה המשתמש הנוכחי (המבקש למחוק).</param>
        /// <param name="messageId">מזהה ההודעה למחיקה.</param>
        /// <returns>
        /// הודעת סטטוס המתארת את תוצאת המחיקה:
        /// "ok" - נמחק בהצלחה, "forbidden" - אין הרשאה, "notfound" - ההודעה לא נמצאה.
        /// </returns>
        string DeleteMessage(int myId, int messageId);
    }
}
