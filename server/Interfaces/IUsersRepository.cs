using PartnersWebApi.Models;

namespace PartnersWebApi.Interfaces
{
    /// <summary>
    /// ממשק המגדיר את הפעולות הנדרשות לניהול משתמשים מול בסיס הנתונים
    /// </summary>
    public interface IUsersRepository
    {
        /// <summary>אימות פרטי כניסה של משתמש</summary>
        User Login(string email, string password);

        /// <summary>רישום משתמש חדש במערכת</summary>
        bool Register(RegisterModel model);

        /// <summary>שליפת מידע מלא על פרופיל המשתמש</summary>
        User GetUserProfile(int id);

        /// <summary>עדכון פרטי הפרופיל והעדפות המשתמש</summary>
        bool UpdatePreferences(int id, string fullName, string imageUrl, string preferences, string familyStatus, string city, DateTime? birthDate);

        /// <summary>בדיקה האם כתובת אימייל כבר קיימת במערכת</summary>
        bool IsEmailExists(string email);

        // ===== מנוע ההתאמה (Matching) — נדרש ל-similar.jsx =====
        /// <summary>
        /// מחזיר רשימת משתמשים דומים עם ציון התאמה, סיבות וקטגוריות משותפות.
        /// </summary>
        List<SimilarUserDto> GetSimilarUsers(int userId);

        // ===== נקודות המשחק והטבות (Trivia / Rewards) =====
        bool AddUserPoints(int userId, int points);
        Task<string?> PurchaseRewardAsync(int userId, int pointsCost);
        Task<bool> SaveCouponAsync(int userId, string couponCode, string title, string purchaseDate, string expiryDate);
        Task<List<object>> GetUserCouponsAsync(int userId);

        Task<double> GetUserLeaderboardPercentileAsync(int userId);
        Task<List<object>> GetTop5UsersAsync();
        Task<object> GenerateAiMysteryCouponAsync(int userId, int pointsCost);
    }
}
