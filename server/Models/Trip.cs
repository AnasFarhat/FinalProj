namespace PartnersWebApi.Models
{
    /// <summary>
    /// ייצוג מלא של טיול במערכת
    /// כולל את כל המידע המוצג למשתמש על מסלול הטיול והלוגיסטיקה
    /// </summary>
    public class Trip
    {
        // מזהה ייחודי של הטיול
        public int TripId { get; set; }

        // כותרת הטיול (שם המסלול או האירוע)
        public string Title { get; set; }

        // תאריך ושעת הטיול
        public DateTime TripDate { get; set; }

        // מיקום גיאוגרפי או נקודת מפגש
        public string Location { get; set; }

        // נתיב לתמונה המייצגת של הטיול
        public string ImageUrl { get; set; }

        // תיאור קצר ותמציתי עבור רשימת הטיולים
        public string Description { get; set; }

        // קטגוריית הטיול (למשל: משפחות, מטיבי לכת, זוגות)
        public string Category { get; set; }

        // גיל מינימלי להשתתפות בטיול (אופציונלי)
        public int? MinAge { get; set; }

        // כותרת משנה המוצגת בדף הטיול
        public string Subtitle { get; set; }

        // פירוט נרחב על הטיול והמסלול
        public string About { get; set; }

        // פירוט קהל היעד (למשל: "מתאים לצעירים", "דתיים בלבד" וכו')
        public string TargetAudience { get; set; }

        // רמת קושי של המסלול (למשל: קל, בינוני, קשה)
        public string Difficulty { get; set; }

        // פירוט טכני של ההליכה (למשל: עליות תלולות, הליכה במים)
        public string WalkDetails { get; set; }

        // אורך המסלול (למשל: "5 ק"מ", "3 שעות הליכה")
        public string RouteLength { get; set; }

        // ציוד נדרש (למשל: נעלי הליכה, כובע, 3 ליטר מים)
        public string Equipment { get; set; }

        // שם המדריך או הגוף המארגן
        public string Guide { get; set; }

        // האם המסלול נגיש לעגלות או לבעלי מוגבלויות
        public bool IsAccessible { get; set; }


    }

    /// <summary>
    /// מודל המשמש לעדכון נתונים של טיול קיים
    /// מכיל את השדות שניתן לערוך דרך ממשק הניהול
    /// </summary>
    public class TripUpdateModel
    {
        public string Title { get; set; }
        public DateTime TripDate { get; set; }
        public string Location { get; set; }
        public string Category { get; set; }
        public string ImageUrl { get; set; }
        public string Description { get; set; }
        public string Subtitle { get; set; }
        public string About { get; set; }
        public string TargetAudience { get; set; }
        public string Difficulty { get; set; }
        public string WalkDetails { get; set; }
        public string RouteLength { get; set; }
        public string Equipment { get; set; }
        public string Guide { get; set; }
        public int? MinAge { get; set; }
        public bool IsAccessible { get; set; }
    }
}