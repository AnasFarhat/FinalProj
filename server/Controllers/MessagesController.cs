using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using PartnersWebApi.Interfaces;
using PartnersWebApi.Models;

namespace PartnersWebApi.Controllers
{
    /// <summary>
    /// בקר (Controller) לניהול הודעות פרטיות ושיחות בין משתמשים.
    /// כל הנקודות דורשות אימות (Authorize) ומזהות את המשתמש לפי הטוקן.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MessagesController : ControllerBase
    {
        private readonly IMessagesRepository _repo;

        /// <summary>
        /// אתחול הבקר עם שכבת הגישה לנתוני ההודעות.
        /// </summary>
        /// <param name="repo">מאגר ההודעות (Repository).</param>
        public MessagesController(IMessagesRepository repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// שליפת מזהה המשתמש הנוכחי מתוך ה-Claims של הטוקן.
        /// </summary>
        /// <returns>מזהה המשתמש, או null אם לא נמצא/לא תקין.</returns>
        private int? GetUserId()
        {
            var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idStr, out int id)) return id;
            return null;
        }

        /// <summary>
        /// שליחת הודעה פרטית ממשתמש למשתמש אחר.
        /// POST /api/Messages/send
        /// </summary>
        /// <param name="model">אובייקט המכיל את מזהה המקבל ותוכן ההודעה.</param>
        /// <returns>200 בהצלחה, אחרת 400 עם הודעת שגיאה.</returns>
        [HttpPost("send")]
        public IActionResult Send([FromBody] SendMessageModel model)
        {
            var myId = GetUserId();
            if (myId == null) return Unauthorized();

            var result = _repo.SendMessage(myId.Value, model.ReceiverId, model.Content);
            if (result == "ok") return Ok(new { result });
            return BadRequest(new { result, message = "השליחה נכשלה" });
        }

        /// <summary>
        /// שליפת היסטוריית השיחה בין המשתמש הנוכחי למשתמש אחר.
        /// GET /api/Messages/conversation/{otherId}
        /// </summary>
        /// <param name="otherId">מזהה המשתמש השני בשיחה.</param>
        /// <returns>רשימת ההודעות בשיחה.</returns>
        [HttpGet("conversation/{otherId}")]
        public IActionResult Conversation(int otherId)
        {
            var myId = GetUserId();
            if (myId == null) return Unauthorized();
            return Ok(_repo.GetConversation(myId.Value, otherId));
        }

        /// <summary>
        /// שליפת רשימת כל השיחות של המשתמש הנוכחי.
        /// GET /api/Messages/chats
        /// </summary>
        /// <returns>רשימת השיחות של המשתמש.</returns>
        [HttpGet("chats")]
        public IActionResult Chats()
        {
            var myId = GetUserId();
            if (myId == null) return Unauthorized();
            return Ok(_repo.GetMyChats(myId.Value));
        }

        /// <summary>
        /// מחיקת הודעה בודדת. רק השולח של ההודעה רשאי למחוק אותה.
        /// DELETE /api/Messages/{messageId}
        /// </summary>
        /// <param name="messageId">מזהה ההודעה למחיקה.</param>
        /// <returns>
        /// 200 בהצלחה, 403 אם אין הרשאה, 404 אם ההודעה לא נמצאה, אחרת 400.
        /// </returns>
        [HttpDelete("{messageId}")]
        public IActionResult Delete(int messageId)
        {
            var myId = GetUserId();
            if (myId == null) return Unauthorized();

            var result = _repo.DeleteMessage(myId.Value, messageId);
            if (result == "ok") return Ok(new { result });
            if (result == "forbidden") return StatusCode(403, new { result, message = "אין הרשאה למחוק הודעה זו" });
            if (result == "notfound") return NotFound(new { result, message = "ההודעה לא נמצאה" });
            return BadRequest(new { result, message = "המחיקה נכשלה" });
        }
    }
}
