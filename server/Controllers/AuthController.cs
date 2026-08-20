using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using PartnersWebApi.Interfaces;
using PartnersWebApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PartnersWebApi.Controllers
{
    /// <summary>
    /// בקר אימות וחסימה: אחראי על תהליך ההתחברות (Login), הנפקת טוקנים (JWT) ובדיקת סטטוס חשבון
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IUsersRepository _repo;

        /// <summary>
        /// אתחול הבקר עם הזרקת הגדרות המערכת ומאגר המשתמשים
        /// </summary>
        public AuthController(IConfiguration config, IUsersRepository repo)
        {
            _config = config;
            _repo = repo;
        }

        /// <summary>
        /// ביצוע תהליך התחברות למערכת
        /// בודק פרטי זהות, מוודא שהחשבון אינו חסום ומנפיק טוקן גישה
        /// </summary>
        /// <param name="login">מודל המכיל אימייל וסיסמה</param>
        /// <returns>אובייקט הכולל טוקן JWT ונתוני פרופיל בסיסיים לממשק המשתמש</returns>
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult Login([FromBody] LoginModel login)
        {
            try
            {
                // 1. בדיקת תקינות קלט בסיסית
                if (login == null || string.IsNullOrEmpty(login.Email))
                {
                    return BadRequest(new { message = "נתוני התחברות חסרים" });
                }

                // 2. אימות המשתמש מול בסיס הנתונים
                var user = _repo.Login(login.Email, login.Password);

                if (user == null)
                {
                    return Unauthorized(new { message = "פרטי התחברות שגויים" });
                }

                // 3. בדיקת חסימת חשבון (Account Block)
                // במידה והמשתמש נחסם על ידי מנהל, הגישה תימנע למרות שהסיסמה נכונה
                if (user.IsBlocked)
                {
                    return Unauthorized(new { message = "חשבונך נחסם על ידי מנהל המערכת. אנא צור קשר עם התמיכה." });
                }

                // 4. ניהול ה-Claims (טענות) עבור ה-JWT
                // הטענות מוטמעות בתוך הטוקן ומאפשרות לזהות את המשתמש ותפקידו ללא פנייה חוזרת ל-DB
                var claims = new[] {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Email, user.Email ?? ""),
                    new Claim("FullName", user.FullName ?? ""),
                    new Claim(ClaimTypes.Role, user.Role ?? "")
                };

                // 5. הגדרת מפתח ההצפנה והחתימה הדיגיטלית
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                // 6. יצירת אובייקט ה-Token
                var token = new JwtSecurityToken(
                    issuer: _config["Jwt:Issuer"],
                    audience: _config["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(60), // הטוקן תקף לשעה אחת
                    signingCredentials: creds
                );

                // 7. החזרת תשובה מלאה הכוללת את הטוקן ונתוני פרופיל לצורך אתחול ה-State ב-React
                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    userId = user.UserId,
                    fullName = user.FullName,
                    imageUrl = user.ImageUrl,
                    city = user.City,
                    points = user.Points,
                    isPartner = user.IsPartner,
                    role = user.Role
                });
            }
            catch (Exception ex)
            {
                // טיפול בשגיאת שרת פנימית והחזרת פירוט לצורך ניפוי שגיאות (Debug)
                return StatusCode(StatusCodes.Status500InternalServerError, $"שגיאת שרת פנימית: {ex.Message}");
            }
        }
    }
}