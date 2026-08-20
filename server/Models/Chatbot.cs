using System;
using System.Collections.Generic;

namespace PartnersWebApi.Models
{
    /// <summary>
    /// מייצג רשומה בטבלת היסטוריית הצ'אט במסד הנתונים
    /// </summary>
    public class Chatbot
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public string Intent { get; set; } = string.Empty;
        public bool WasHandledByBot { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string SessionId { get; set; } = string.Empty;
    }

    /// <summary>
    /// אובייקט בקשה המגיע מהפרונט-אנד (React)
    /// </summary>
    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public int? TripId { get; set; }
    }

    /// <summary>
    /// אובייקט תגובה הנשלח חזרה למשתמש
    /// </summary>
    public class ChatResponse
    {
        public string Response { get; set; } = string.Empty;
        public string Intent { get; set; } = string.Empty;
        public bool NeedsHuman { get; set; } = false;
        public List<QuickReply> QuickReplies { get; set; } = new List<QuickReply>();
    }

    /// <summary>
    /// מייצג כפתור תגובה מהירה בממשק הצ'אט
    /// </summary>
    public class QuickReply
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}