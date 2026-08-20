using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartnersWebApi.Interfaces;
using PartnersWebApi.Models;
using PartnersWebApi.Services;   // תמיכה ב-IChatAiService וב-SentimentResult
using System;
using System.Threading.Tasks;    // תמיכה בפעולות אסינכרוניות (Task)

namespace PartnersWebApi.Controllers
{
    /// <summary>
    /// בקר לניהול משובים: קליטת דירוגים, ניתוח חוויות מטיילים ושליפת היסטוריית משובים
    /// כולל ניתוח סנטימנט חכם ומתקדם וזיהוי "דגלים אדומים" לבטיחות באמצעות Gemini AI
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class FeedbacksController : ControllerBase
    {
        private readonly IFeedbacksRepository _repo;
        private readonly ITripsRepository _tripRepo;
        private readonly IChatAiService _aiService;   // שירות ה-AI החדש

        /// <summary>
        /// אתחול הבקר עם הזרקת תלות של מאגר המשובים, מאגר הטיולים ושירות ה-AI
        /// </summary>
        public FeedbacksController(IFeedbacksRepository repo, ITripsRepository tripRepo, IChatAiService aiService)
        {
            _repo = repo;
            _tripRepo = tripRepo;
            _aiService = aiService;   // אתחול השירות
        }

        /// <summary>
        /// שמירת משוב חדש במערכת או עדכון משוב קיים
        /// כולל שילוב חכם בין דירוג הכוכבים לניתוח הסנטימנט של Gemini AI
        /// </summary>
        /// <param name="feedback">אובייקט המשוב המכיל דירוגים וטקסט חופשי</param>
        /// <returns>תוצאת ניתוח המשוב וסטטוס שמירה</returns>
        [HttpPost]
        [Authorize] // אבטחה מבוססת Token
        public async Task<IActionResult> PostFeedback([FromBody] Feedback feedback)
        {
            try
            {
                // 1. וולידציה ראשונית: וידוא קבלת נתונים
                if (feedback == null) return BadRequest(new { message = "נתוני המשוב לא התקבלו" });

                // 2. אימות קיום הטיול ומועד ההגשה (מותר להגיש משוב רק לאחר סיום הטיול)
                var trip = _tripRepo.GetTripById(feedback.TripId);
                if (trip == null) return NotFound(new { message = "הטיול לא נמצא במערכת" });

                if (DateTime.Now < trip.TripDate.AddDays(1))
                {
                    return BadRequest(new { message = "ניתן להגיש משוב רק החל מיום לאחר סיום הטיול." });
                }

                // 3. חישוב ממוצע הכוכבים הנוכחי (הדרכה + מסלול)
                double avgRating = (feedback.GuideRating + feedback.TrackRating) / 2.0;

                // ציון הכוכבים בסולם 0-100 (5 כוכבים=100, 1 כוכב=20)
                double starsScore = (avgRating / 5.0) * 100.0;

                int finalScore;
                string sentiment;
                string summary;

                bool hasText = !string.IsNullOrWhiteSpace(feedback.FreeText);

                // 4. אלגוריתם השילוב החכם (המשוואה החדשה)
                if (hasText)
                {
                    // יש טקסט -> משלבים: 50% כוכבים + 50% ניתוח Gemini של הטקסט
                    var ai = await _aiService.AnalyzeSentimentAsync(feedback.FreeText);

                    // אם ה-AI זיהה דגל אדום (סכנה/בטיחות) - גובר על הכל ומוריד ציון ל-25 ומטה
                    if (ai.Sentiment == "Urgent_Negative")
                    {
                        finalScore = Math.Min(ai.Score, 25);
                        sentiment = "Urgent_Negative";
                    }
                    else
                    {
                        finalScore = (int)Math.Round(starsScore * 0.5 + ai.Score * 0.5);
                        sentiment = LabelFromScore(finalScore);
                    }
                    summary = ai.Summary;
                }
                else
                {
                    // אין טקסט -> מסתמכים 100% על הכוכבים שהוזנו
                    finalScore = (int)Math.Round(starsScore);
                    sentiment = LabelFromScore(finalScore);
                    summary = "";
                }

                // 5. שמירה או עדכון בבסיס הנתונים דרך שכבת ה-Repository
                if (_repo.SaveFeedback(feedback, sentiment, avgRating, finalScore, summary))
                {
                    return Ok(new
                    {
                        message = "המשוב נשמר ונותח בהצלחה!",
                        sentiment,
                        score = finalScore,
                        summary,
                        isUrgent = sentiment == "Urgent_Negative"
                    });
                }

                return StatusCode(500, "נכשלה שמירת הנתונים במסד הנתונים");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// פונקציית עזר להמרת ציון מספרי (0-100) לתווית סנטימנט מתאימה
        /// </summary>
        private string LabelFromScore(int score)
        {
            if (score >= 65) return "Positive";
            if (score <= 40) return "Negative";
            return "Neutral";
        }

        /// <summary>
        /// שליפת היסטוריית המשובים האישית של המשתמש
        /// </summary>
        /// <param name="userId">מזהה המשתמש</param>
        /// <returns>רשימת משובים קודמים בסטטוס 200 Ok</returns>
        [HttpGet("user/{userId}")]
        [Authorize]
        public IActionResult GetHistory(int userId)
        {
            try
            {
                var data = _repo.GetHistoryByUserId(userId);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה בשרת: {ex.Message}");
            }
        }
    }
}