using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using PartnersWebApi.Models;
using System.Data;
using System;
using PartnersWebApi.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace PartnersWebApi.Controllers
{
    /// <summary>
    /// בקר לניהול הקהילה: טיפול בפוסטים, תגובות, לייקים ומערכת הדיווחים על תוכן פוגעני
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class CommunityController : ControllerBase
    {
        private readonly ICommunityRepository _repo;

        /// <summary>
        /// אתחול הבקר עם הזרקת תלות של מאגר הקהילה
        /// </summary>
        public CommunityController(ICommunityRepository repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// מחיקת פוסט מהמערכת בצורה מאובטחת
        /// המזהה נלקח ישירות מה-Token כדי למנוע ממשתמשים למחוק פוסטים של אחרים
        /// </summary>
        /// <param name="postId">מזהה הפוסט למחיקה</param>
        /// <returns>סטטוס הצלחה או שגיאת הרשאה</returns>
        [HttpDelete("post/{postId}")]
        [Authorize]
        public IActionResult DeletePost(int postId)
        {
            try
            {
                // 1. שליפת זהות המשתמש מתוך ה-Token המאובטח
                var identity = HttpContext.User.Identity as ClaimsIdentity;
                if (identity == null) return Unauthorized(new { message = "משתמש לא מזוהה" });

                // 2. שליפת ה-UserId מתוך ה-Claims (נשמר כ-NameIdentifier)
                var userIdClaim = identity.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                if (userIdClaim == null) return Unauthorized(new { message = "מזהה משתמש חסר בטוקן" });
                var roleClaim = identity.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
                bool isAdmin = roleClaim != null && roleClaim.Value == "Admin";
                int userId = int.Parse(userIdClaim.Value);

                // 3. ביצוע המחיקה רק במידה והמשתמש מורשה (בעלים או מנהל)
                int result = _repo.DeletePost(postId, userId, isAdmin);

                return result switch
                {
                    200 => Ok(new { message = "הפוסט נמחק בהצלחה!" }),
                    404 => NotFound(new { message = "הפוסט לא נמצא במערכת" }),
                    401 => Unauthorized(new { message = "אין לך הרשאה למחוק פוסט זה" }),
                    _ => StatusCode(500, "שגיאה טכנית במחיקה")
                };
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "שגיאה בעיבוד הבקשה", error = ex.Message });
            }
        }

        /// <summary>
        /// שליפת פיד הפוסטים בקהילה
        /// </summary>
        /// <param name="userId">מזהה המשתמש הצופה (לצורך התאמת נתוני לייקים אישיים)</param>
        /// <returns>רשימת פוסטים בסטטוס 200 Ok</returns>
        [HttpGet("posts/{userId}")]
        [Authorize]
        public IActionResult GetPosts(int userId)
        {
            try
            {
                var posts = _repo.GetCommunityPosts(userId);
                return Ok(posts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "שגיאה בטעינת הפיד הקהילתי: " + ex.Message);
            }
        }

        /// <summary>
        /// יצירת פוסט חדש (טקסט ומדיה) בקהילה
        /// </summary>
        /// <param name="model">נתוני הפוסט החדש</param>
        /// <returns>מזהה הפוסט שנוצר</returns>
        [HttpPost("post")]
        [Authorize]
        public IActionResult CreatePost([FromBody] CreatePostRequest model)
        {
            try
            {
                int postId = _repo.CreatePost(model);
                return Ok(new { message = "הפוסט נוצר בהצלחה!", postId = postId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"שגיאה ביצירת פוסט: {ex.Message}" });
            }
        }

        /// <summary>
        /// הוספה או הסרה של לייק מפוסט (Toggle)
        /// </summary>
        /// <param name="like">נתוני הלייק (מזהה פוסט ומשתמש)</param>
        /// <returns>סטטוס הפעולה (Liked/Unliked)</returns>
        [HttpPost("like")]
        [Authorize]
        public IActionResult ToggleLike([FromBody] LikeInput like)
        {
            try
            {
                string result = _repo.ToggleLike(like);
                return Ok(new { message = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// הוספת תגובה חדשה לפוסט
        /// </summary>
        /// <param name="comment">נתוני התגובה</param>
        /// <returns>הודעת הצלחה או שגיאה</returns>
        [HttpPost("comment")]
        [Authorize]
        public IActionResult AddComment([FromBody] CommentInput comment)
        {
            try
            {
                if (_repo.AddComment(comment))
                {
                    return Ok(new { message = "התגובה נוספה בהצלחה" });
                }
                return StatusCode(500, "שגיאה בהוספת תגובה");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// שליחת דיווח על תוכן פוגעני (פוסט או תגובה) לבדיקת מנהל
        /// </summary>
        /// <param name="report">נתוני הדיווח והסיבה</param>
        /// <returns>הודעת אישור על קבלת הדיווח</returns>
        [HttpPost("report")]
        [Authorize]
        public IActionResult ReportContent([FromBody] CreateReportRequest report)
        {
            try
            {
                if (_repo.ReportContent(report))
                {
                    return Ok(new { message = "הדיווח נשלח בהצלחה למנהלי המערכת." });
                }
                return BadRequest("שגיאה בשמירת הדיווח.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// מחיקת תגובה ספציפית מהמערכת
        /// </summary>
        /// <param name="commentId">מזהה התגובה למחיקה</param>
        /// <returns>סטטוס הצלחה</returns>
        [HttpDelete("comment/{commentId}")]
        [Authorize]
        public IActionResult DeleteComment(int commentId)
        {
            try
            {
                if (_repo.DeleteComment(commentId))
                {
                    return Ok(new { message = "התגובה נמחקה בהצלחה" });
                }
                return NotFound("התגובה לא נמצאה");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "שגיאה במחיקת התגובה");
            }
        }
    }
}