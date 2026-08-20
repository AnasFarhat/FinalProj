namespace PartnersWebApi.Models
{
    /// <summary>
    /// מייצג משתמש בעל התאמה גבוהה למשתמש הנוכחי.
    /// כולל את נתוני המשתמש, ציוני ההתאמה והסיבות לחישוב ההתאמה.
    /// </summary>
    public class SimilarUserDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string City { get; set; }
        public string FamilyStatus { get; set; }
        public string Preferences { get; set; }
        public string ImageUrl { get; set; }

        public int MatchScore { get; set; }
        public int ProfileScore { get; set; }
        public int BehaviorScore { get; set; }

        public List<string> Reasons { get; set; } = new List<string>();
        public List<string> SharedCategories { get; set; } = new List<string>();
    }
}