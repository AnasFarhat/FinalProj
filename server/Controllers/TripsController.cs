using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using PartnersWebApi.Interfaces;
using PartnersWebApi.Models;
using Microsoft.AspNetCore.Authorization;

namespace PartnersWebApi.Controllers
{
    /// <summary>
    /// בקר לניהול עולם הטיולים: שליפת מסלולים, ניהול ה-Hub האישי ועדכון נתונים לוגיסטיים
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class TripsController : ControllerBase
    {
        private readonly ITripsRepository _repo;

        /// <summary>
        /// אתחול הבקר עם הזרקת תלות של מאגר הטיולים
        /// </summary>
        public TripsController(ITripsRepository repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// שליפת היסטוריית הטיולים ולוח הזמנים העתידי של המטייל
        /// דורש הזדהות (JWT) כדי להגן על פרטיות המשתמש
        /// </summary>
        /// <param name="userId">מזהה המשתמש</param>
        /// <returns>רשימת טיולים משויכים בסטטוס 200 Ok</returns>
        [HttpGet("mytrips/{userId}")]
        [Authorize]
        public IActionResult GetMyTrips(int userId)
        {
            try
            {
                var trips = _repo.GetUserTrips(userId);
                return Ok(trips);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה בשליפת טיולים: {ex.Message}");
            }
        }

        /// <summary>
        /// שליפת מידע לוגיסטי מפורט על טיול (רמת קושי, ציוד נדרש ונגישות)
        /// נועד להנגיש מידע קריטי למטייל טרם היציאה לשטח
        /// </summary>
        /// <param name="id">מזהה הטיול</param>
        /// <returns>אובייקט טיול מלא או 404 אם לא נמצא</returns>
        [HttpGet("{id}")]
        [Authorize]
        public IActionResult GetTripDetails(int id)
        {
            try
            {
                var trip = _repo.GetTripById(id);
                if (trip == null)
                {
                    return NotFound(new { message = "הטיול המבוקש לא נמצא במערכת" });
                }
                return Ok(trip);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה בשליפת פרטי הטיול: {ex.Message}");
            }
        }

        /// <summary>
        /// אלגוריתם פנימי לניתוח סנטימנט (רגשות) בעברית מתוך טקסט המשוב
        /// מסווג את המשוב ל: חיובי, שלילי או ניטרלי לפי מילות מפתח
        /// </summary>
        private string AnalyzeHebrewSentiment(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "Neutral";

            // מילות מפתח לזיהוי חוויות חיוביות או נקודות תורפה
            string[] positiveWords = { "מעולה", "מצוין", "מדהים", "כיף", "טוב", "נהנינו", "מומלץ", "נפלא", "קסום", "יפה", "מקסים", "אהבנו" };
            string[] negativeWords = { "גרוע", "רע", "קשה", "חם", "אכזבה", "משעמם", "נורא", "מסוכן", "עמוס", "לא מאורגן", "מאכזב", "לא מומלץ" };

            int posScore = positiveWords.Count(w => text.Contains(w));
            int negScore = negativeWords.Count(w => text.Contains(w));

            if (posScore > negScore) return "Positive";
            return negScore > posScore ? "Negative" : "Neutral";
        }

        /// <summary>
        /// שליפת נתוני ה-Hub האישי (לוח המחוונים) של המטייל
        /// כולל המלצות מותאמות אישית, טיול קרוב וסטטיסטיקות
        /// </summary>
        /// <param name="userId">מזהה המשתמש</param>
        /// <returns>אובייקט נתונים מרוכז ל-Dashboard</returns>
        [HttpGet("GetRecommendationsAI/{userId}")]
        [Authorize]
        public IActionResult GetRecommendationsAI(int userId)
        {
            try
            {
                // הניתוח והשליפה מתבצעים ב-Repository לביצועים אופטימליים
                var data = _repo.GetAiRecommendations(userId);

                if (data == null) return NotFound(new { message = "לא נמצאו נתונים עבור המטייל" });

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "שגיאה בטעינת המצפן הדיגיטלי", error = ex.Message });
            }
        }

        /// <summary>
        /// עדכון פרטי טיול קיים (פעולת מנהל)
        /// מאפשר לשנות נתיבי מסלול, תאריכים ומידע לוגיסטי
        /// </summary>
        [HttpPut("edit/{id}")]
        [Authorize]
        public IActionResult UpdateTrip(int id, [FromBody] TripUpdateModel updatedTrip)
        {
            try
            {
                if (updatedTrip == null) return BadRequest(new { message = "נתוני עדכון לא תקינים" });

                bool success = _repo.UpdateTrip(id, updatedTrip);

                if (success)
                {
                    return Ok(new { message = "הטיול עודכן בהצלחה במערכת" });
                }
                return NotFound(new { message = "הטיול המבוקש לא נמצא לעדכון" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "שגיאה בעדכון הטיול", error = ex.Message });
            }
        }

        /// <summary>
        /// העלאת תמונת טיול לשרת ואחסונה בתיקיית הנכסים (Assets)
        /// כולל יצירת שם ייחודי למניעת כפילויות ואבטחת הקובץ
        /// </summary>
        /// <param name="file">קובץ התמונה מהטופס</param>
        /// <returns>ה-URL היחסי של התמונה שנשמרה</returns>
        [HttpPost("uploadImage")]
        [Authorize]
        public async Task<IActionResult> UploadImage([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "לא נבחר קובץ להעלאה" });

            try
            {
                // יצירת מזהה ייחודי (GUID) לשם הקובץ
                var fileExtension = Path.GetExtension(file.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";

                // הגדרת נתיב השמירה בתיקיית התמונות הסטטית
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");

                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                var filePath = Path.Combine(folderPath, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // החזרת נתיב יחסי לצורך תצוגה ב-Frontend
                var imageUrl = $"/images/{uniqueFileName}";
                return Ok(new { url = imageUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "שגיאה טכנית בשמירת המדיה", error = ex.Message });
            }
        }

        /// <summary>
        /// שליפת קטלוג הטיולים המלא
        /// פתוח לצפייה ללא צורך בהתחברות (AllowAnonymous) לצורך שיווק וחשיפה
        /// </summary>
        /// <returns>רשימת כל הטיולים הפעילים במערכת</returns>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetAllTrips()
        {
            try
            {
                var trips = _repo.GetAllTrips();
                return Ok(trips);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "שגיאה בשליפת קטלוג הטיולים", error = ex.Message });
            }
        }

        [HttpPut("attendance/{userId}/{tripId}")]
        [Authorize]
        public IActionResult UpdateAttendance(int userId, int tripId, [FromBody] AttendanceUpdateModel model)
        {
            try
            {
                bool success = _repo.UpdateAttendanceStatus(userId, tripId, model.AttendanceStatus);
                if (success)
                    return Ok(new { message = "סטטוס ההגעה עודכן בהצלחה" });
                return BadRequest(new { message = "העדכון נכשל" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        public class AttendanceUpdateModel
        {
            public bool AttendanceStatus { get; set; }
        }



        [HttpGet("attendance/{userId}/{tripId}")]
        [Authorize]
        public IActionResult GetAttendanceStatus(int userId, int tripId)
        {
            try
            {
                var status = _repo.GetAttendanceStatus(userId, tripId);
                return Ok(new { attendanceStatus = status });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }


    }
}