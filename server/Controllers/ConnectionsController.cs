using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using PartnersWebApi.Interfaces;
using PartnersWebApi.Models;

namespace PartnersWebApi.Controllers
{
    /// <summary>
    /// בקר האחראי על ניהול קשרי החברות בין משתמשים במערכת.
    /// מאפשר שליחת בקשות חברות, אישור או דחייה של בקשות,
    /// צפייה בבקשות שהתקבלו, בדיקת סטטוס קשר ושליפת רשימת החברים.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ConnectionsController : ControllerBase
    {
        private readonly IConnectionsRepository _repo;

        /// <summary>
        /// אתחול הבקר עם הזרקת התלות של מאגר נתוני הקשרים בין המשתמשים.
        /// </summary>
        public ConnectionsController(IConnectionsRepository repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// שליפת מזהה המשתמש מתוך טוקן האימות (JWT).
        /// </summary>
        /// <returns>
        /// מזהה המשתמש המחובר, או Null אם לא ניתן לחלץ את המזהה מהטוקן.
        /// </returns>
        private int? GetUserId()
        {
            var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idStr, out int id)) return id;
            return null;
        }

        /// <summary>
        /// שליחת בקשת חברות למשתמש אחר.
        /// </summary>
        /// <param name="model">אובייקט המכיל את פרטי בקשת החברות.</param>
        /// <returns>הודעת הצלחה או שגיאה בהתאם לתוצאת הפעולה.</returns>
        [HttpPost("send")]
        public IActionResult Send([FromBody] SendRequestModel model)
        {
            var myId = GetUserId();
            if (myId == null) return Unauthorized();

            var result = _repo.SendRequest(myId.Value, model.ReceiverId, model.Message);
            return Ok(new { result });
        }

        /// <summary>
        /// אישור או דחייה של בקשת חברות שהתקבלה.
        /// </summary>
        /// <param name="model">אובייקט המכיל את מזהה הבקשה ואת החלטת המשתמש.</param>
        /// <returns>הודעת הצלחה במקרה של ביצוע הפעולה או הודעת שגיאה במקרה של כשל.</returns>
        [HttpPut("respond")]
        public IActionResult Respond([FromBody] RespondRequestModel model)
        {
            var myId = GetUserId();
            if (myId == null) return Unauthorized();

            var ok = _repo.RespondRequest(model.RequestId, myId.Value, model.Accept);
            if (ok) return Ok(new { message = model.Accept ? "החיבור אושר" : "הבקשה נדחתה" });
            return BadRequest(new { message = "הפעולה נכשלה" });
        }

        /// <summary>
        /// שליפת כל בקשות החברות שהתקבלו עבור המשתמש המחובר.
        /// </summary>
        /// <returns>רשימת בקשות החברות הממתינות לטיפול.</returns>
        [HttpGet("received")]
        public IActionResult Received()
        {
            var myId = GetUserId();
            if (myId == null) return Unauthorized();
            return Ok(_repo.GetReceivedRequests(myId.Value));
        }

        /// <summary>
        /// בדיקת סטטוס הקשר בין המשתמש המחובר לבין משתמש אחר.
        /// </summary>
        /// <param name="otherId">מזהה המשתמש השני.</param>
        /// <returns>סטטוס הקשר בין שני המשתמשים.</returns>
        [HttpGet("status/{otherId}")]
        public IActionResult Status(int otherId)
        {
            var myId = GetUserId();
            if (myId == null) return Unauthorized();
            return Ok(new { status = _repo.GetConnectionStatus(myId.Value, otherId) });
        }

        /// <summary>
        /// שליפת רשימת כל החברים של המשתמש המחובר.
        /// </summary>
        /// <returns>רשימת הקשרים הפעילים של המשתמש.</returns>
        [HttpGet("my")]
        public IActionResult My()
        {
            var myId = GetUserId();
            if (myId == null) return Unauthorized();
            return Ok(_repo.GetMyConnections(myId.Value));
        }
    }
}