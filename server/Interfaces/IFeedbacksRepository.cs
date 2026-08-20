using PartnersWebApi.Models;

namespace PartnersWebApi.Interfaces
{
    /// <summary>
    /// ממשק המגדיר את הפעולות הנדרשות לניהול משובים ודירוגים במערכת
    /// מטפל בשמירת חוויות המשתמש וניתוחן, ובהצגת היסטוריית משובים
    /// </summary>
    public interface IFeedbacksRepository
    {
        /// <summary>
        /// שליפת היסטוריית המשובים שהשאיר משתמש ספציפי
        /// משמש להצגת חוויות העבר של המשתמש במערכת
        /// </summary>
        /// <param name="userId">מזהה המשתמש עבורו נשלפת ההיסטוריה</param>
        /// <returns>אוסף של אובייקטים (IEnumerable) המכילים את פרטי המשובים והטיולים הרלוונטיים</returns>
        IEnumerable<object> GetHistoryByUserId(int userId);

        /// <summary>
        /// שמירת משוב חדש במערכת לאחר סיום טיול
        /// כולל שמירת הנתונים הגולמיים לצד תוצרי ניתוח (סנטימנט ודירוג משוקלל)
        /// </summary>
        /// <param name="feedback">אובייקט המשוב המכיל דירוגים, תגיות וטקסט חופשי</param>
        /// <param name="sentiment">תוצאת ניתוח הסנטימנט (Positive/Negative/Neutral)</param>
        /// <param name="avgRating">ציון ממוצע משוקלל שחושב עבור המשוב</param>
        /// <returns>True אם השמירה בבסיס הנתונים הצליחה, אחרת False</returns>
        bool SaveFeedback(Feedback feedback, string sentiment, double avgRating, int sentimentScore, string sentimentSummary);
    }
}