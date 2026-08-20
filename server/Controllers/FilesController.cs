using Microsoft.AspNetCore.Mvc;
using PartnersWebApi.Models;
using PartnersWebApi.Interfaces;

namespace PartnersWebApi.Controllers
{
    /// <summary>
    /// בקר לניהול קבצים ומדיה: טיפול בהעלאת קבצים בודדים או מרובים לשרת
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private readonly string _uploadFolderPath;

        /// <summary>
        /// אתחול הבקר והגדרת נתיב תיקיית היעד להעלאות
        /// </summary>
        /// <param name="env">ממשק לסביבת העבודה של השרת</param>
        public FilesController(IWebHostEnvironment env)
        {
            // הגדרת הנתיב הפיזי לתיקיית ההעלאות בשורש הפרויקט
            _uploadFolderPath = Path.Combine(env.ContentRootPath, "Uploads");
        }

        /// <summary>
        /// העלאת קובץ בודד לשרת
        /// </summary>
        /// <param name="model">מודל המכיל את הקובץ שהתקבל מהטופס</param>
        /// <returns>הנתיב היחסי של הקובץ שנשמר</returns>
        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] FileUploadModel model)
        {
            // בדיקה אם הועלה קובץ תקין
            if (model.File == null || model.File.Length == 0)
            {
                return BadRequest("לא נבחר קובץ להעלאה.");
            }

            try
            {
                // יצירת תיקיית ההעלאות במידה ואינה קיימת בשרת
                Directory.CreateDirectory(_uploadFolderPath);

                // אבטחת שם הקובץ: שימוש ב-GUID למניעת כפילויות ושמירה על סיומת הקובץ המקורית
                var fileExtension = Path.GetExtension(model.File.FileName);
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(_uploadFolderPath, fileName);

                // שמירת זרם הנתונים (Stream) של הקובץ אל הדיסק הפיזי
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.File.CopyToAsync(stream);
                }

                // החזרת נתיב יחסי בלבד (ללא כתובת השרת) לצורך גמישות ב-React
                var fileUrl = $"/Uploads/{fileName}";

                return Ok(new { FilePath = fileUrl });
            }
            catch (Exception ex)
            {
                // טיפול בשגיאת כתיבה לדיסק או הרשאות
                return StatusCode(StatusCodes.Status500InternalServerError, $"שגיאה בהעלאת הקובץ: {ex.Message}");
            }
        }

        /// <summary>
        /// העלאת מספר קבצים בו-זמנית (למשל עבור גלריית תמונות בפוסט)
        /// </summary>
        /// <param name="files">רשימת קבצים שהתקבלו מהטופס</param>
        /// <returns>רשימה של נתיבים יחסיים עבור כל הקבצים שנשמרו בהצלחה</returns>
        [HttpPost("upload-multiple")]
        public async Task<IActionResult> UploadMultiple([FromForm] List<IFormFile> files)
        {
            // בדיקה האם הרשימה ריקה
            if (files == null || files.Count == 0)
            {
                return BadRequest("לא נבחרו קבצים להעלאה.");
            }

            try
            {
                // וידוא קיום תיקיית היעד
                Directory.CreateDirectory(_uploadFolderPath);

                List<string> uploadedUrls = new List<string>();

                // מעבר על כל קובץ ברשימה ושמירתו בנפרד
                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        // יצירת מזהה ייחודי לכל קובץ ברשימה
                        var fileExtension = Path.GetExtension(file.FileName);
                        var fileName = $"{Guid.NewGuid()}{fileExtension}";
                        var filePath = Path.Combine(_uploadFolderPath, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        // הוספת הנתיב לרשימת הכתובות המוחזרת
                        var fileUrl = $"/Uploads/{fileName}";
                        uploadedUrls.Add(fileUrl);
                    }
                }

                // החזרת מערך הכתובות לשימוש ב-Frontend
                return Ok(new { filePaths = uploadedUrls });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"שגיאה בהעלאת הקבצים: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// מודל עזר לקליטת קובץ בודד בבקשת Multipart Form Data
    /// </summary>
    public class FileUploadModel
    {
        public IFormFile File { get; set; }
    }
}