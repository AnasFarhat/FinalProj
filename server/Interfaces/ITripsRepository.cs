using PartnersWebApi.Models;

namespace PartnersWebApi.Interfaces
{
    /// <summary>
    /// ממשק המגדיר את הפעולות הנדרשות לניהול ושליפת נתוני טיולים מהמערכת
    /// </summary>
    public interface ITripsRepository
    {
        /// <summary>
        /// שליפת רשימת הטיולים אליהם רשום משתמש ספציפי
        /// </summary>
        /// <param name="userId">מזהה המשתמש</param>
        /// <returns>אוסף של טיולים (IEnumerable) המשויכים למשתמש</returns>
        IEnumerable<Trip> GetUserTrips(int userId);

        /// <summary>
        /// שליפת מידע מלא על טיול בודד לפי המזהה שלו
        /// </summary>
        /// <param name="id">מזהה הטיול</param>
        /// <returns>אובייקט Trip המכיל את כל פרטי המסלול והלוגיסטיקה</returns>
        Trip GetTripById(int id);

        /// <summary>
        /// שליפת נתונים מרוכזים עבור ה-Hub (לוח הבקרה) של המשתמש
        /// כולל סטטיסטיקות, טיולים קרובים ועדכונים
        /// </summary>
        /// <param name="userId">מזהה המשתמש</param>
        /// <returns>אובייקט דינמי המכיל את נתוני ה-Hub</returns>
        object GetAiRecommendations(int userId);

        /// <summary>
        /// עדכון פרטי טיול קיים במערכת (פעולת מנהל)
        /// </summary>
        /// <param name="id">מזהה הטיול לעדכון</param>
        /// <param name="model">מודל המכיל את הנתונים המעודכנים</param>
        /// <returns>True אם העדכון הצליח, אחרת False</returns>
        bool UpdateTrip(int id, TripUpdateModel model);

        bool UpdateAttendanceStatus(int userId, int tripId, bool attendanveStatus);

        /// <summary>
        /// שליפת כל הטיולים הקיימים במערכת עבור תצוגת פיד או ניהול
        /// </summary>
        /// <returns>אוסף של אובייקטים המייצגים את כל הטיולים</returns>
        IEnumerable<object> GetAllTrips();

        bool? GetAttendanceStatus(int userId, int tripId);
    }
}