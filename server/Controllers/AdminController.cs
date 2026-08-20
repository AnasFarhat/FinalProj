using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PartnersWebApi.Interfaces;
using PartnersWebApi.Models;

namespace PartnersWebApi.Controllers
{
    /// <summary>
    /// בקר ניהול ומנהלה: ריכוז מדדי KPI, ניתוח נתונים, ניהול משתמשים ובקרת תוכן קהילתי
    /// מוגן ברמת תפקיד (Admin) למניעת גישה של משתמשים רגילים למידע רגיש
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // הגנה על מידע ניהולי ורגיש באמצעות אימות מבוסס תפקידים
    public class AdminController : ControllerBase
    {
        private readonly IDashboardRepository _repo;

        /// <summary>
        /// אתחול הבקר עם הזרקת תלות של מאגר נתוני הניהול (Dashboard Repository)
        /// </summary>
        public AdminController(IDashboardRepository repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// שליפת מדדי ביצוע מרכזיים (KPIs) של הארגון
        /// כולל כמות משתמשים כוללת, צמיחה ומדדי שביעות רצון כלליים
        /// </summary>
        /// <returns>אובייקט המכיל נתונים סטטיסטיים בסטטוס 200 Ok</returns>
        [HttpGet("stats/kpis")]
        public IActionResult GetKPIs()
        {
            try
            {
                var data = _repo.GetGeneralKPIs();
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "שגיאה בשליפת מדדי KPI: " + ex.Message);
            }
        }

        
        /// <summary>
        /// שליפת סיכום ניתוח סנטימנט (רגשות) מהמשובים
        /// מעבד נתונים ממשובים מילוליים וקוליים כדי להציג את הלך הרוח הכללי של המטיילים
        /// </summary>
        /// <returns>אובייקט המרכז את התפלגות הסנטימנט במערכת</returns>
        [HttpGet("stats/sentiment")]
        public IActionResult GetSentiment()
        {
            try
            {
                var sentiment = _repo.GetSentimentSummary();
                return Ok(sentiment);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "שגיאה בניתוח סנטימנט: " + ex.Message);
            }
        }


        /// <summary>
        /// שליפת רשימת המשתמשים המלאה לצורכי ניהול, חסימה ושינוי הרשאות
        /// </summary>
        /// <returns>רשימת משתמשים מפורטת</returns>
        [HttpGet("users")]
        public IActionResult GetUsers()
        {
            try
            {
                var users = _repo.GetUsersList();
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "שגיאה בשליפת רשימת המשתמשים: " + ex.Message);
            }
        }
        [HttpGet("stats/attendance-overview")]
        public IActionResult GetAttendanceOverview()
        {
            try { return Ok(_repo.GetAttendanceOverview()); }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }
        /// <summary>
        /// שינוי סטטוס חסימה של משתמש (Toggle Block)
        /// משמש להגבלת גישה למשתמשים המפרים את תקנון הקהילה
        /// </summary>
        /// <param name="userId">מזהה המשתמש לעדכון</param>
        /// <returns>הודעת הצלחה או שגיאה בביצוע</returns>
        [HttpPut("users/toggle-block/{userId}")]
        public IActionResult ToggleBlock(int userId)
        {
            try
            {
                // בדיקה בסיסית - האם אני מנסה לחסום את עצמי?
                // var currentAdminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                // if (currentAdminId == userId) return BadRequest("אדמין לא יכול לחסום את עצמו.");

                var success = _repo.ToggleUserBlock(userId);
                if (success)
                {
                    return Ok(new { message = "פעולת החסימה/שחרור בוצעה בהצלחה." });
                }

                // אם success הוא false, זה כנראה כי המשתמש לא קיים
                return NotFound(new { message = $"משתמש עם מזהה {userId} לא נמצא במערכת." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "שגיאת שרת פנימית", details = ex.Message });
            }
        }

        /// <summary>
        /// שליפת דיווחים מקובצים על תוכן פוגעני
        /// מאפשר למנהל לראות אילו פוסטים בקהילה דורשים התערבות ומהן סיבות הדיווח
        /// </summary>
        /// <returns>רשימת דיווחים מקובצים לפי פוסטים (Grouped Reports)</returns>
        [HttpGet("reports")]
        public IActionResult GetReports()
        {
            try
            {
                var reports = _repo.GetGroupedReports();
                return Ok(reports);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "שגיאה בשליפת דיווחים: " + ex.Message);
            }
        }

        /// <summary>
        /// שינוי נראות פוסט (הסתרה או חשיפה)
        /// פעולת משך לטיפול בפוסטים שדווחו כבעייתיים
        /// </summary>
        /// <param name="postId">מזהה הפוסט לעדכון</param>
        /// <returns>הודעת אישור על עדכון הנראות</returns>
        [HttpPut("posts/toggle-visibility/{postId}")]
        public IActionResult TogglePostVisibility(int postId)
        {
            try
            {
                var success = _repo.TogglePostVisibility(postId);
                if (success) return Ok(new { message = "סטטוס נראות הפוסט עודכן בהצלחה." });

                return BadRequest("שגיאה בעדכון הסטטוס של הפוסט.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "שגיאה טכנית בעדכון נראות הפוסט: " + ex.Message);
            }
        }
    }
}