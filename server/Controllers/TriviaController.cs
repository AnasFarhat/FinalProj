using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartnersWebApi.Interfaces;
using PartnersWebApi.Models;
using System.Linq;

namespace PartnersWebApi.Controllers
{
    /// <summary>
    /// בקר האחראי על ניהול משחק הטריוויה מבוסס המיקום.
    /// מאפשר זיהוי הגעת המשתמש לנקודת עניין, יצירת שאלות מותאמות באמצעות AI,
    /// שמירת תוצאות המשחק וטעינת נתוני נקודות עניין למערכת.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class TriviaController : ControllerBase
    {
        private readonly ITriviaRepository _triviaRepository;
        private readonly IGeminiTriviaService _geminiTriviaService;
        private readonly IUsersRepository _userRepository;

        /// <summary>
        /// אתחול הבקר עם הזרקת התלויות לניהול משחק הטריוויה,
        /// יצירת שאלות באמצעות AI ושליפת נתוני המשתמש.
        /// </summary>
        public TriviaController(
            ITriviaRepository triviaRepository,
            IGeminiTriviaService geminiTriviaService,
            IUsersRepository userRepository)
        {
            _triviaRepository = triviaRepository;
            _geminiTriviaService = geminiTriviaService;
            _userRepository = userRepository;
        }

        /// <summary>
        /// בודק האם המשתמש נמצא בתוך תחום גיאוגרפי של נקודת עניין.
        /// במקרה של זיהוי תקף, נוצר משחק טריוויה מותאם אישית
        /// בהתאם לגיל המשתמש ולהעדפות הטיול שלו.
        /// </summary>
        /// <param name="latitude">קו הרוחב של מיקום המשתמש.</param>
        /// <param name="longitude">קו האורך של מיקום המשתמש.</param>
        /// <returns>
        /// אובייקט משחק הכולל שאלות טריוויה, או הודעת שגיאה במקרה של כשל.
        /// </returns>
        [Authorize]
        [HttpPost("check-location")]
        public async Task<IActionResult> CheckLocation([FromQuery] double latitude, [FromQuery] double longitude)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized("משתמש לא מחובר או טוקן לא תקין.");
                }

                var locationDto = new UserLocationDto
                {
                    Latitude = latitude,
                    Longitude = longitude
                };

                var location = await _triviaRepository.CheckUserGeofenceAsync(locationDto);
                if (location == null)
                {
                    return Ok(null);
                }

                // Check if the user played at this location within the last 7 days
                bool playedRecently = await _triviaRepository.HasUserPlayedLocationRecentlyAsync(userId, location.Id, 7);
                if (playedRecently)
                {
                    return BadRequest("You have already played at this location this week.");
                }

                var user = _userRepository.GetUserProfile(userId);
                Console.WriteLine("========== USER DEBUG ==========");
                Console.WriteLine($"UserId: {userId}");
                Console.WriteLine($"FamilyStatus: {user?.FamilyStatus}");
                Console.WriteLine($"Preferences: {user?.Preferences}");
                Console.WriteLine("================================");


                int age = 20;
                string travelStyle = "עצמאי";
                int questionsCount = 3;
                if (user != null)
                {
                    if (user.BirthDate.HasValue)
                    {
                        age = DateTime.Today.Year - user.BirthDate.Value.Year;
                        if (user.BirthDate.Value.Date > DateTime.Today.AddYears(-age)) age--;
                        if (age <= 0) age = 20;
                    }

                    string prefs = user.Preferences ?? string.Empty;
                    string status = user.FamilyStatus ?? string.Empty;

                    

                    if (status.Equals("משפחה", StringComparison.OrdinalIgnoreCase))
                    {
                        questionsCount = 5;
                    }
                    Console.WriteLine($"QuestionsCount = {questionsCount}");
                    Console.WriteLine($"TravelStyle = {travelStyle}");

                    if (prefs.Contains("משפחה") || status.Contains("נשוי") || status.Contains("ילדים"))
                    {
                        travelStyle = "משפחה עם ילדים";
                    }
                }

                Console.WriteLine($"[DEBUG AI Prompt Parameters] UserId: {userId}, Age: {age}, Style: {travelStyle}, Location: {location.Name}");

                var aiQuestions = await _geminiTriviaService.GenerateQuizAsync(location.Name, age, travelStyle,  questionsCount);

                if (aiQuestions == null || !aiQuestions.Any())
                {
                    Console.WriteLine($"[FALLBACK] Gemini failed. Fetching hardcoded questions for {location.Name} from DB...");
                    if (aiQuestions == null || !aiQuestions.Any())
                    {
                        return BadRequest("אופס... שירות ה-AI לא זמין כעת ואין שאלות גיבוי זמינות.");
                    }
                }

                var gameObject = new LocationQuizGameDto
                {
                    LocationId = location.Id,
                    LocationName = location.Name,
                    Questions = aiQuestions,
                    PointsPerCorrectAnswer = travelStyle.Contains("משפחה") ? 20 : 30
                };

                return Ok(gameObject);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה פנימית בשרת: {ex.Message}");
            }
        }

        /// <summary>
        /// מסיים את משחק הטריוויה ושומר את תוצאות המשתמש במערכת.
        /// הפעולה מעדכנת את הניקוד שנצבר ואת התקדמות המשתמש.
        /// </summary>
        /// <param name="locationId">מזהה נקודת העניין שבה שוחק המשחק.</param>
        /// <param name="totalPointsEarned">סך הנקודות שהמשתמש צבר במשחק.</param>
        /// <returns>הודעת הצלחה במקרה של שמירה תקינה או הודעת שגיאה במקרה של כשל.</returns>
        [Authorize]
        [HttpPost("finish-game")]
        public async Task<IActionResult> FinishGame([FromQuery] int locationId, [FromQuery] int totalPointsEarned)
        {
            try
            {
                Console.WriteLine($"[DEBUG finish-game] קיבלתי בקשה עבור LocationId: {locationId}, Points: {totalPointsEarned}");

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized("משתמש לא מחובר.");
                }

                bool isCorrect = totalPointsEarned > 0;
                var success = await _triviaRepository.SaveUserProgressAsync(userId, locationId, totalPointsEarned, isCorrect);

                if (success)
                {
                    return Ok(new { success = true, message = $"המשחק הסתיים! נצברו {totalPointsEarned} נקודות." });
                }
                return BadRequest("שגיאה ברישום תוצאות המשחק.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SQL ERROR] שגיאה פנימית בשמירת הניקוד: {ex.Message}");
                return StatusCode(500, $"שגיאה פנימית בשרת: {ex.Message}");
            }
        }

        /// <summary>
        /// טוען למערכת את נתוני נקודות העניין מקובץ ה-CSV הממשלתי.
        /// הפעולה מיועדת לאתחול ועדכון מאגר הנתונים של המשחק.
        /// </summary>
        /// <returns>הודעת הצלחה אם הנתונים נטענו או הודעת שגיאה במקרה של כשל.</returns>
        [AllowAnonymous]
        [HttpGet("seed-gov-data")]
        public async Task<IActionResult> SeedData()
        {
            var result = await _triviaRepository.SeedLocationsFromGovAsync();
            if (result)
            {
                return Ok(new { message = "הנתונים מקובץ ה-CSV נמשכו והוזרקו בהצלחה וה-ID אופס!" });
            }
            return BadRequest("אופס... שגיאה במשיכת הנתונים מהקובץ.");
        }
    }
}