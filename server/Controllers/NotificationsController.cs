using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartnersWebApi.Interfaces;
using PartnersWebApi.Models;
using FcmNotification = FirebaseAdmin.Messaging.Notification;

namespace PartnersWebApi.Controllers
{
    /// <summary>
    /// בקר האחראי על ניהול ההתראות במערכת.
    /// מאפשר שליפת התראות, סימון התראות כנקראו, שמירת טוקני FCM
    /// ושליחת התראות למשתמשים באמצעות מסד הנתונים ו-Firebase Cloud Messaging.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationsRepository _repo;
        private readonly ILogger<NotificationsController> _logger;

        /// <summary>
        /// אתחול הבקר עם הזרקת התלויות לניהול ההתראות ורישום לוגים.
        /// </summary>
        public NotificationsController(
            INotificationsRepository repo,
            ILogger<NotificationsController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        /// <summary>
        /// שליפת כל ההתראות של משתמש מסוים.
        /// </summary>
        /// <param name="userId">מזהה המשתמש.</param>
        /// <returns>רשימת ההתראות של המשתמש או הודעת שגיאה במקרה של כשל.</returns>
        [HttpGet("user/{userId}")]
        [Authorize]
        public IActionResult Get(int userId)
        {
            try
            {
                return Ok(_repo.GetByUserId(userId));
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה בשליפת התראות: {ex.Message}");
            }
        }

        /// <summary>
        /// סימון התראה כנקראה.
        /// </summary>
        /// <param name="id">מזהה ההתראה.</param>
        /// <returns>הודעת הצלחה אם ההתראה עודכנה או הודעת שגיאה במקרה של כשל.</returns>
        [HttpPut("{id}/read")]
        [Authorize]
        public IActionResult MarkRead(int id)
        {
            try
            {
                if (_repo.MarkAsRead(id))
                    return Ok(new { Message = "סטטוס ההתראה עודכן בהצלחה" });

                return NotFound(new { Message = "ההתראה המבוקשת לא נמצאה" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"שגיאה בעדכון סטטוס קריאה: {ex.Message}");
            }
        }

        /// <summary>
        /// שמירת טוקן ה-FCM של המכשיר הנוכחי.
        /// הטוקן נשמר במסד הנתונים לצורך שליחת התראות Push למשתמש.
        /// </summary>
        /// <param name="req">אובייקט המכיל את טוקן ה-FCM של המכשיר.</param>
        /// <returns>אישור על שמירת הטוקן או הודעת שגיאה במקרה של כשל.</returns>
        [HttpPut("fcm-token")]
        [Authorize]
        public IActionResult SaveFcmToken([FromBody] FcmTokenRequest req)
        {
            if (string.IsNullOrWhiteSpace(req?.FcmToken))
                return BadRequest("טוקן חסר");

            // שולפים את ה-userId מה-JWT
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            _repo.SaveFcmToken(userId, req.FcmToken);
            return Ok(new { message = "טוקן נשמר" });
        }

        /// <summary>
        /// שליחת התראה למשתמשים.
        /// ההתראה נשמרת במסד הנתונים ונשלחת גם כהודעת Push
        /// באמצעות Firebase Cloud Messaging למכשירים הרלוונטיים.
        /// </summary>
        /// <param name="req">אובייקט המכיל את פרטי ההתראה לשליחה.</param>
        /// <returns>מידע על תוצאות השליחה למסד הנתונים ול-FCM.</returns>
        [HttpPost("send")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SendNotification([FromBody] SendNotificationRequest req)
        {
            if (req == null) return BadRequest("נתוני הודעה חסרים");

            // שלב 1 — שמירה ל-DB (היסטוריה פנימית) — בדיוק כמו קודם
            bool dbSuccess = _repo.SendNotification(req);

            // שלב 2 — FCM: שליחה אמיתית לכל המכשירים הרלוונטיים
            int fcmSent = 0;
            int fcmFailed = 0;

            try
            {
                var tokens = _repo.GetFcmTokens(req.TripId).ToList();

                if (tokens.Count > 0)
                {
                    // FCM תומך ב-500 טוקנים לכל קריאה (MulticastMessage)
                    foreach (var batch in tokens.Chunk(500))
                    {
                        var multicast = new MulticastMessage
                        {
                            Tokens = batch.ToList(),
                            Notification = new FcmNotification
                            {
                                Title = req.Title,
                                Body = req.Message,
                            },
                            // Android — הודעה תעיר את האפליקציה גם ברקע
                            Android = new AndroidConfig
                            {
                                Priority = Priority.High,
                            },
                            // iOS
                            Apns = new ApnsConfig
                            {
                                Aps = new Aps { Sound = "default" }
                            }
                        };

                        var response = await FirebaseMessaging.DefaultInstance
                            .SendEachForMulticastAsync(multicast);

                        fcmSent += response.SuccessCount;
                        fcmFailed += response.FailureCount;

                        _logger.LogInformation(
                            "[FCM] Batch sent: {Success} success, {Fail} failed",
                            response.SuccessCount, response.FailureCount);
                    }
                }
                else
                {
                    _logger.LogWarning("[FCM] No FCM tokens found for this send request.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FCM] Error sending push notifications");
            }

            if (!dbSuccess && fcmSent == 0)
                return BadRequest(new { message = "שגיאה: לא נמצאו משתמשים תואמים." });

            return Ok(new
            {
                message = "ההודעה נשלחה בהצלחה!",
                dbSaved = dbSuccess,
                fcmSent,
                fcmFailed
            });
        }
    }

    /// <summary>
    /// אובייקט המכיל את טוקן ה-FCM של המכשיר לצורך רישומו במערכת.
    /// </summary>
    public record FcmTokenRequest(string FcmToken);
}