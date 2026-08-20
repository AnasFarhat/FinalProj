namespace PartnersWebApi.Models
{
    /// <summary>
    /// מייצג בקשת חברות שהתקבלה ממשתמש אחר,
    /// כולל פרטי השולח וההודעה שצורפה לבקשה.
    /// </summary>
    public class ReceivedRequestDto
    {
        public int RequestId { get; set; }
        public int SenderId { get; set; }
        public string FullName { get; set; }
        public string City { get; set; }
        public string ImageUrl { get; set; }
        public string Preferences { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// מייצג משתמש המחובר למשתמש הנוכחי.
    /// משמש להצגת רשימת החברים והצ'אטים במערכת.
    /// </summary>
    public class ConnectionDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string City { get; set; }
        public string ImageUrl { get; set; }
        public DateTime? ConnectedAt { get; set; }
    }

    /// <summary>
    /// אובייקט בקשה לשליחת בקשת חברות למשתמש אחר.
    /// </summary>
    public class SendRequestModel
    {
        public int ReceiverId { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// אובייקט בקשה לאישור או דחייה של בקשת חברות.
    /// </summary>
    public class RespondRequestModel
    {
        public int RequestId { get; set; }
        public bool Accept { get; set; }
    }
}