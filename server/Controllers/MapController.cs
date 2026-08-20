using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using PartnersWebApi.Interfaces;
using PartnersWebApi.Models;

namespace PartnersWebApi.Controllers
{
    /// <summary>
    /// בקר האחראי על ניהול נתוני המפה של המשתמשים.
    /// מאפשר שליפת מסלולים, שמירת מסלולים ועדכון מיקום בזמן אמת.
    /// הגישה לפעולות הבקר מחייבת משתמש מאומת.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MapController : ControllerBase
    {
        private readonly IMapService _mapService;

        /// <summary>
        /// אתחול הבקר עם הזרקת התלות של שירות ניהול המפה.
        /// </summary>
        public MapController(IMapService mapService) => _mapService = mapService;

        /// <summary>
        /// שליפת כל המסלולים השמורים של המשתמש המחובר.
        /// </summary>
        /// <returns>רשימת המסלולים של המשתמש או הודעת שגיאה במקרה של משתמש לא מזוהה.</returns>
        [HttpGet("routes")]
        public async Task<IActionResult> GetRoutes()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // בדיקה שהערך קיים
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

            // המרה ל-int במקום ל-Guid
            if (int.TryParse(userIdString, out int userId))
            {
                return Ok(await _mapService.GetRoutesAsync(userId));
            }

            return BadRequest("Invalid User ID format");
        }

        /// <summary>
        /// עדכון המיקום הנוכחי של המשתמש במערכת.
        /// משמש להצגת מיקום בזמן אמת על גבי המפה.
        /// </summary>
        /// <param name="dto">אובייקט המכיל את קווי הרוחב והאורך של המשתמש.</param>
        /// <returns>אישור על ביצוע הפעולה או הודעת שגיאה במקרה של כשל.</returns>
        [HttpPost("location")]
        public async Task<IActionResult> UpdateLocation([FromBody] LiveLocationDto dto)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

            if (int.TryParse(userIdString, out int userId))
            {
                await _mapService.UpdateLocationAsync(userId, dto.Lat, dto.Lng);
                return Ok();
            }

            return BadRequest("Invalid User ID format");
        }

        /// <summary>
        /// שמירת מסלול חדש עבור המשתמש המחובר.
        /// המסלול חייב להכיל לפחות שתי נקודות ציון.
        /// </summary>
        /// <param name="dto">אובייקט המכיל את פרטי המסלול ונקודות הדרך.</param>
        /// <returns>הודעת הצלחה או שגיאה בהתאם לתוצאת הפעולה.</returns>
        [HttpPost("routes")]
        public async Task<IActionResult> SaveRoute([FromBody] RouteDto dto)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

            if (int.TryParse(userIdString, out int userId))
            {
                if (dto == null || dto.Waypoints == null || dto.Waypoints.Count < 2)
                    return BadRequest("המסלול חייב לכלול לפחות 2 נקודות");

                await _mapService.SaveRouteAsync(userId, dto);
                return Ok(new { message = "המסלול נשמר בהצלחה" });
            }

            return BadRequest("Invalid User ID format");
        }
        /// <summary>
        /// מחיקת מסלול שמור של המשתמש המחובר.
        /// הפעולה מוחקת גם את כל נקודות הדרך (Waypoints) המשויכות למסלול.
        /// ניתן למחוק רק מסלול השייך למשתמש המאומת.
        /// </summary>
        /// <param name="routeId">מזהה המסלול למחיקה.</param>
        /// <returns>
        /// הודעת הצלחה במקרה של מחיקה,
        /// הודעת שגיאה אם המסלול לא נמצא או שהמשתמש אינו מורשה.
        /// </returns>

        [HttpDelete("routes/{routeId}")]
        public async Task<IActionResult> DeleteRoute(int routeId)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized();

            if (!int.TryParse(userIdString, out int userId))
                return BadRequest("Invalid User ID format");

            bool deleted = await _mapService.DeleteRouteAsync(userId, routeId);

            if (!deleted)
                return NotFound("המסלול לא נמצא.");

            return Ok(new { message = "המסלול נמחק בהצלחה." });
        }
    }
}