using System.Threading.Tasks;
using PartnersWebApi.Models;

namespace PartnersWebApi.Interfaces
{
    /// <summary>
    /// ממשק המגדיר את הפעולות לניהול משחק הטריוויה מבוסס המיקום והתקדמות המשתמשים.
    /// </summary>
    public interface ITriviaRepository
    {
        /// <summary>
        /// בודק האם המשתמש נמצא בתוך תחום גיאוגרפי (Geofence) של אחת מנקודות העניין.
        /// </summary>
        /// <param name="location">אובייקט המכיל את מיקום המשתמש.</param>
        /// <returns>
        /// אובייקט Location אם המשתמש נמצא באזור תקף, אחרת Null.
        /// </returns>
        Task<Location?> CheckUserGeofenceAsync(UserLocationDto location);

        /// <summary>
        /// שומר את התקדמות המשתמש לאחר מענה על שאלת טריוויה.
        /// </summary>
        /// <param name="userId">מזהה המשתמש.</param>
        /// <param name="locationId">מזהה נקודת העניין.</param>
        /// <param name="pointsEarned">מספר הנקודות שהמשתמש צבר.</param>
        /// <param name="isCorrect">מציין האם התשובה הייתה נכונה.</param>
        /// <returns>True אם הנתונים נשמרו בהצלחה, אחרת False.</returns>
        Task<bool> SaveUserProgressAsync(int userId, int locationId, int pointsEarned, bool isCorrect);

        /// <summary>
        /// טוען למערכת את נקודות העניין ממאגר המידע הממשלתי.
        /// </summary>
        /// <returns>True אם הטעינה הצליחה, אחרת False.</returns>
        Task<bool> SeedLocationsFromGovAsync();

        /// <summary>
        /// בודק האם המשתמש כבר השתתף במשחק עבור נקודת עניין מסוימת במהלך מספר הימים שהוגדר.
        /// </summary>
        /// <param name="userId">מזהה המשתמש.</param>
        /// <param name="locationId">מזהה נקודת העניין.</param>
        /// <param name="days">מספר הימים לבדיקה לאחור.</param>
        /// <returns>True אם המשתמש כבר שיחק לאחרונה, אחרת False.</returns>
        Task<bool> HasUserPlayedLocationRecentlyAsync(int userId, int locationId, int days);
    }
}