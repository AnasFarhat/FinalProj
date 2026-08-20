namespace PartnersWebApi.Models
{
    /// <summary>
    /// מייצג הודעה פרטית שהוחלפה בין שני משתמשים במערכת.
    /// </summary>
    public class MessageDto
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }

    /// <summary>
    /// מייצג פריט ברשימת השיחות של המשתמש,
    /// כולל פרטי המשתמש השני, ההודעה האחרונה ומספר ההודעות שלא נקראו.
    /// </summary>
    public class ChatListItemDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string ImageUrl { get; set; }
        public string LastMessage { get; set; }
        public DateTime? LastTime { get; set; }
        public int UnreadCount { get; set; }
    }

    /// <summary>
    /// אובייקט בקשה לשליחת הודעה פרטית למשתמש אחר.
    /// </summary>
    public class SendMessageModel
    {
        public int ReceiverId { get; set; }
        public string Content { get; set; }
    }
}