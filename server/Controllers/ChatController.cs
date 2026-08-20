using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PartnersWebApi.Interfaces;
using PartnersWebApi.Models;
using PartnersWebApi.Services;
using System.Security.Claims;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PartnersWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IChatRepository _chatRepo; // גישה למסד הנתונים של הצ'אט והטיולים
        private readonly IChatAiService _aiService; // גישה לשירות הבינה המלאכותית (Gemini)

        // Constructor - הזרקת התלויות הנדרשות לקונטרולר
        public ChatController(IChatRepository chatRepo, IChatAiService aiService)
        {
            _chatRepo = chatRepo;
            _aiService = aiService;
        }

        /// <summary>
        /// שליפת מזהה המשתמש מתוך ה-JWT Token המחובר
        /// </summary>
        private int GetUserIdFromToken()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "userId");
            return int.Parse(claim.Value);
        }

        /// <summary>
        /// נקודת קצה לקבלת הודעה מהמשתמש, עיבודה מול AI והחזרת תשובה
        /// </summary>
        [Authorize]
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.Message)) return BadRequest("Message is required");

            int userId = GetUserIdFromToken();
            var trips = _chatRepo.GetUserTripsDetailed(userId);

            string currentDate = DateTime.Now.ToString("yyyy-MM-dd");
            string dateContext = $"הוראה קריטית: תאריך היום הוא {currentDate}.\n\n";
            string tripsContext = trips.Any()
                ? "הנה כל הטיולים של המשתמש:\n" + string.Join("\n", trips.Select(t =>
                    string.Join(" | ", t.Where(kv => kv.Value != null).Select(kv => $"{kv.Key}: {kv.Value}"))))
                : "למשתמש אין טיולים רשומים כרגע.";

            string context = dateContext + tripsContext;

            // ── שליפת היסטוריית הסשן הנוכחי ──
            var historyForAI = new List<(string role, string text)>();
            if (!string.IsNullOrEmpty(request.SessionId))
            {
                var sessionHistory = await _chatRepo.GetChatHistoryBySessionAsync(userId, request.SessionId);
                foreach (var item in sessionHistory)
                {
                    var type = item.GetType();
                    var userMsg = type.GetProperty("user")?.GetValue(item)?.ToString() ?? "";
                    var botMsg = type.GetProperty("bot")?.GetValue(item)?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(userMsg)) historyForAI.Add(("user", userMsg));
                    if (!string.IsNullOrEmpty(botMsg)) historyForAI.Add(("model", botMsg));
                }
            }

            // ── שליחה ל-AI עם היסטוריה ──
            string responseText = await _aiService.GetAiResponseAsync(request.Message, context, historyForAI);

            _chatRepo.SaveChat(userId, request.Message, responseText, "AI", request.SessionId);

            return Ok(new ChatResponse
            {
                Response = responseText,
                Intent = "AI",
                QuickReplies = GetQuickReplies(request.Message)
            });
        }

        /// <summary>
        /// ייצור רשימת כפתורי תגובה מהירה (Quick Replies) המבוססת על תוכן שאלת המשתמש
        /// </summary>
        private List<QuickReply> GetQuickReplies(string message)
        {
            var replies = new List<QuickReply>();
            var msg = message.ToLower();

            // אם המשתמש שאל על זמן/תאריך - נציע לו לשאול על מיקום או ציוד
            if (msg.Contains("מתי") || msg.Contains("תאריך"))
            {
                replies.Add(new QuickReply { Label = "📍 איפה הטיול?", Value = "איפה הטיול שלי?" });
                replies.Add(new QuickReply { Label = "🎒 מה להביא?", Value = "מה להביא לטיול?" });
            }
            // אם המשתמש שאל על מיקום - נציע לו לשאול על זמן או כמות משתתפים
            else if (msg.Contains("איפה") || msg.Contains("מיקום"))
            {
                replies.Add(new QuickReply { Label = "📅 מתי הטיול?", Value = "מתי הטיול שלי?" });
                replies.Add(new QuickReply { Label = "👥 כמה אנשים?", Value = "כמה אנשים רשומים?" });
            }
            // אם המשתמש בירך לשלום - נציע לו לראות את הטיולים שלו או ציוד
            else if (msg.Contains("שלום") || msg.Contains("היי"))
            {
                replies.Add(new QuickReply { Label = "📅 הטיולים שלי", Value = "מה הטיולים שאני רשום אליהם?" });
                replies.Add(new QuickReply { Label = "🎒 מה להביא?", Value = "מה להביא לטיול?" });
            }
            // במקרים אחרים - נציע חזרה לתפריט הראשי או רשימת טיולים
            else
            {
                replies.Add(new QuickReply { Label = "🏠 חזרה לתפריט", Value = "שלום" });
                replies.Add(new QuickReply { Label = "📅 הטיולים שלי", Value = "מה הטיולים שלי?" });
            }

            return replies;
        }

        /// <summary>
        /// שליפת כל סשני השיחות (Sessions) של המשתמש עבור ה-Sidebar
        /// </summary>
        [Authorize]
        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessions() => Ok(await _chatRepo.GetUserSessionsAsync(GetUserIdFromToken()));

        /// <summary>
        /// שליפת היסטוריית ההודעות המלאה עבור סשן ספציפי
        /// </summary>
        [Authorize]
        [HttpGet("history/{sessionId}")]
        public async Task<IActionResult> GetHistory(string sessionId) => Ok(await _chatRepo.GetChatHistoryBySessionAsync(GetUserIdFromToken(), sessionId));
    }
}