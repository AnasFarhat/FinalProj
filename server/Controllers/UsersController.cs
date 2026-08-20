using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using PartnersWebApi.Models;
using PartnersWebApi.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace PartnersWebApi.Controllers
{
    /// <summary>
    /// בקר לניהול פעולות משתמשים: הרשמה, ניהול פרופיל, התאמות, נקודות והטבות
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUsersRepository _repo;

        public UsersController(IUsersRepository repo)
        {
            _repo = repo;
        }

        /// <summary>רישום משתמש חדש למערכת</summary>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult Register([FromBody] RegisterModel model)
        {
            try
            {
                if (model == null || string.IsNullOrEmpty(model.Email))
                    return BadRequest(new { message = "נתוני רישום חסרים או אימייל לא תקין" });

                if (_repo.IsEmailExists(model.Email))
                    return BadRequest(new { message = "האימייל הזה כבר קיים במערכת" });

                if (_repo.Register(model))
                    return StatusCode(StatusCodes.Status201Created, new { message = "הרישום בוצע בהצלחה!" });

                return BadRequest(new { message = "הרישום נכשל, בדוק את הנתונים" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"שגיאת שרת פנימית: {ex.Message}");
            }
        }

        /// <summary>שליפת פרופיל משתמש מלא לפי מזהה</summary>
        [HttpGet("{id}/profile")]
        [Authorize]
        public IActionResult GetProfile(int id)
        {
            try
            {
                var profile = _repo.GetUserProfile(id);
                if (profile == null) return NotFound(new { message = "מטייל לא נמצא" });
                return Ok(profile);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה בשליפת פרופיל: {ex.Message}");
            }
        }

        /// <summary>
        /// מנוע ההתאמה בין מטיילים (Matching) — GET api/Users/similar/{userId}
        /// זהו ה-Endpoint שנדרש ל-similar.jsx.
        /// </summary>
        [HttpGet("similar/{userId}")]
        [Authorize]
        public IActionResult GetSimilarUsers(int userId)
        {
            try
            {
                var users = _repo.GetSimilarUsers(userId);
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה בשליפת התאמות: {ex.Message}");
            }
        }

        /// <summary>עדכון העדפות אישיות ופרטי פרופיל</summary>
        [HttpPut("{id}/preferences")]
        [Authorize]
        public IActionResult UpdateUserPreferences(int id, [FromBody] UserPreferencesUpdateModel model)
        {
            try
            {
                if (model == null) return BadRequest(new { message = "נתונים לעדכון לא תקינים" });

                bool success = _repo.UpdatePreferences(
                    id, model.FullName, model.ImageUrl, model.Preferences,
                    model.FamilyStatus, model.City, model.BirthDate
                );

                if (success) return Ok(new { message = "הפרופיל והעדפותיך עודכנו בהצלחה" });
                return NotFound(new { message = "עדכון נכשל: המשתמש לא נמצא במערכת" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה בעדכון העדפות: {ex.Message}");
            }
        }

        /// <summary>עדכון וצבירת נקודות למשתמש בעקבות מענה על שאלת טריוויה</summary>
        [HttpPost("/api/users/{id}/add-points")] 
        [Authorize]
        public IActionResult AddPoints(int id, [FromBody] int pointsToAdd)
        {
            try
            {
                if (pointsToAdd <= 0) return BadRequest(new { message = "מספר הנקודות להוספה חייב להיות חיובי" });
                bool success = _repo.AddUserPoints(id, pointsToAdd);
                if (success) return Ok(new { message = "הניקוד עודכן בהצלחה במערכת!" });
                return NotFound(new { message = "המשתמש לא נמצא" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה בעדכון הניקוד: {ex.Message}");
            }
        }

        [Authorize]
        [HttpPost("{userId}/purchase-reward")]
        public async Task<IActionResult> PurchaseReward(int userId, [FromQuery] int pointsCost, [FromQuery] string rewardTitle)
        {
            try
            {
                var userIdFromToken = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdFromToken) || int.Parse(userIdFromToken) != userId)
                    return Unauthorized("פעולה לא מורשית.");

                string? couponCode = await _repo.PurchaseRewardAsync(userId, pointsCost);

                if (couponCode != null)
                {
                    var purchaseDate = DateTime.Now.ToString("yyyy/MM/dd");
                    var expiryDate = DateTime.Now.AddMonths(3).ToString("yyyy/MM/dd");

                    await _repo.SaveCouponAsync(userId, couponCode, rewardTitle, purchaseDate, expiryDate);

                    return Ok(new { success = true, coupon = couponCode, purchaseDate, expiryDate, message = "הרכישה בוצעה בהצלחה!" });
                }

                return BadRequest("אין לך מספיק נקודות לרכישת הטבה זו.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה פנימית בשרת בעת עיבוד הרכישה: {ex.Message}");
            }
        }

        [Authorize]
        [HttpGet("{userId}/coupons")]
        public async Task<IActionResult> GetUserCoupons(int userId)
        {
            try
            {
                var coupons = await _repo.GetUserCouponsAsync(userId);
                return Ok(coupons);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה בשליפת קופונים: {ex.Message}");
            }
        }

        [Authorize]
        [HttpGet("{userId}/leaderboard-percentile")]
        public async Task<IActionResult> GetLeaderboardPercentile(int userId)
        {
            try
            {
                double percentile = await _repo.GetUserLeaderboardPercentileAsync(userId);
                return Ok(new { percentile });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה בחישוב אחוזון ליגה: {ex.Message}");
            }
        }

        [Authorize]
        [HttpGet("top5")]
        public async Task<IActionResult> GetTop5Users()
        {
            try
            {
                var topUsers = await _repo.GetTop5UsersAsync();
                return Ok(topUsers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה בשליפת מובילי הליגה: {ex.Message}");
            }
        }

        [Authorize]
        [HttpPost("{userId}/purchase-ai-coupon")]
        public async Task<IActionResult> PurchaseAiCoupon(int userId)
        {
            try
            {
                var result = await _repo.GenerateAiMysteryCouponAsync(userId, 400);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


     

        /// <summary>מודל עזר פנימי המגדיר את השדות הניתנים לעדכון בפרופיל</summary>
        public class UserPreferencesUpdateModel
        {
            public string FullName { get; set; }
            public string ImageUrl { get; set; }
            public string Preferences { get; set; }
            public string FamilyStatus { get; set; }
            public string City { get; set; }
            public DateTime? BirthDate { get; set; }
        }
    }
}
