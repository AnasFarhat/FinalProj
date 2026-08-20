using PartnersWebApi.Interfaces;
using PartnersWebApi.Models;
using System.Data;
using System.Data.SqlClient;

namespace PartnersWebApi.Repositories
{
    /// <summary>
    /// מימוש של ממשק הטיולים באמצעות מסד נתונים SQL Server
    /// אחראי על שליפת מסלולים, ניהול המידע הלוגיסטי והפעלת מנוע המלצות AI
    /// </summary>
    public class SQLTripsRepository : ITripsRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// אתחול ה-Repository ושליפת מחרוזת החיבור מהגדרות האפליקציה
        /// </summary>
        public SQLTripsRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        /// <summary>
        /// שליפת רשימת הטיולים אליהם רשום המטייל
        /// מבצע INNER JOIN בין טבלת הטיולים לטבלת הרישומים (UserTrips)
        /// </summary>
        /// <param name="userId">מזהה המשתמש הייחודי</param>
        /// <returns>אוסף של אובייקטי Trip הכוללים מידע לוגיסטי מלא</returns>
        public IEnumerable<Trip> GetUserTrips(int userId)
        {
            var myTrips = new List<Trip>();

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    // קריאה לפרוצדורה החדשה
                    SqlCommand cmd = new SqlCommand("SP_GetUserTrips_Nature", connection);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@UserId", userId);

                    connection.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // שימוש במתודת המיפוי (בדיוק כמו ב-Users, וודאי שיש לך MapReaderToTrip)
                            myTrips.Add(MapReaderToTrip(reader));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetUserTrips: {ex.Message}");
                throw;
            }

            return myTrips;
        }
        /// <summary>
        /// שליפת פרטי טיול מלאים לפי מזהה ייחודי
        /// משמש להצגת דף המידע הלוגיסטי המורחב למטייל (קושי, ציוד, נגישות)
        /// </summary>
        /// <param name="id">מזהה הטיול</param>
        /// <returns>אובייקט Trip מלא או null אם לא נמצא</returns>
        public Trip GetTripById(int id)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    // קריאה לפרוצדורה החדשה עם הסיומת _Nature
                    SqlCommand cmd = new SqlCommand("SP_GetTripById_Nature", connection);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    // העברת הפרמטר (וודאי שהשם ב-C# תואם לשם ב-SQL: @TripId)
                    cmd.Parameters.AddWithValue("@TripId", id);

                    connection.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return MapReaderToTrip(reader);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetTripById: {ex.Message}");
                throw;
            }
            return null;
        }
        /// <summary>
        /// עדכון פרטי טיול קיימים (פעולת מנהל)
        /// מבטיח שלמות נתונים בכל שדות הטיול הקריטיים
        /// </summary>
        public bool UpdateTrip(int id, TripUpdateModel updatedTrip)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    SqlCommand cmd = new SqlCommand("SP_UpdateTrip_Nature", connection);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    // הוספת הפרמטרים
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@Title", updatedTrip.Title ?? "");
                    cmd.Parameters.AddWithValue("@Category", updatedTrip.Category ?? "");
                    cmd.Parameters.AddWithValue("@TripDate", updatedTrip.TripDate);
                    cmd.Parameters.AddWithValue("@ImageUrl", updatedTrip.ImageUrl ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Location", updatedTrip.Location ?? "");
                    cmd.Parameters.AddWithValue("@Description", updatedTrip.Description ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Subtitle", updatedTrip.Subtitle ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@About", updatedTrip.About ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@TargetAudience", updatedTrip.TargetAudience ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Difficulty", updatedTrip.Difficulty ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@WalkDetails", updatedTrip.WalkDetails ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@RouteLength", updatedTrip.RouteLength ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Equipment", updatedTrip.Equipment ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Guide", updatedTrip.Guide ?? (object)DBNull.Value);

                    connection.Open();
                    // מחזיר true אם לפחות שורה אחת עודכנה
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateTrip: {ex.Message}");
                throw;
            }
        }
        /// <summary>
        /// מנוע המלצות היברידי משודרג v3
        /// שיפורים: Time Decay, Difficulty Progression, Geo Distance, Diversity Filter, Exploration, Implicit Feedback
        /// </summary>
        public object GetAiRecommendations(int userId)
        {
            int userAge = 18;
            string userCity = "";
            string userPrefs = "";                 // ⭐ העדפות המשתמש (רשימת קטגוריות מופרדת בפסיקים)
            int totalTrips = 0;
            var categoryScores = new Dictionary<string, double>();
            var difficultyScores = new Dictionary<string, double>();
            var candidateTrips = new List<dynamic>();

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    SqlCommand cmd = new SqlCommand("SP_GetAiRecommendationData_Nature", connection);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    connection.Open();
                    using (var r = cmd.ExecuteReader())
                    {
                        // סט 1: דמוגרפיה
                        if (r.Read())
                        {
                            userAge = Convert.ToInt32(r["Age"]);
                            userCity = r["City"].ToString();
                            totalTrips = Convert.ToInt32(r["TotalTrips"]);
                            // ⭐ קריאת ההעדפות (עמודה חדשה שנוספה ל-SP)
                            userPrefs = r["Preferences"] != DBNull.Value ? r["Preferences"].ToString() : "";
                        }

                        // סט 2: היסטוריית דירוגים + ניתוח רגש
                        r.NextResult();
                        while (r.Read())
                        {
                            string cat = r["Category"].ToString();
                            int rating = Convert.ToInt32(r["Rating"]);

                            // ציון מהכוכבים: [-1..1]  (5 כוכבים=+1, 1 כוכב=-1)
                            double starSignal = (rating - 3.0) / 2.0;

                            // ציון מניתוח הרגש (Gemini), אם קיים משוב טקסטואלי: [-1..1]
                            // SentimentScore הוא 0-100, ממירים ל-[-1..1] סביב 50
                            double finalSignal;
                            if (r["SentimentScore"] != DBNull.Value)
                            {
                                int sentiment = Convert.ToInt32(r["SentimentScore"]);
                                double sentimentSignal = (sentiment - 50.0) / 50.0;
                                // שילוב: 60% רגש (מהטקסט החכם) + 40% כוכבים
                                finalSignal = sentimentSignal * 0.6 + starSignal * 0.4;
                            }
                            else
                            {
                                // אין משוב טקסטואלי -> מסתמכים על הכוכבים בלבד
                                finalSignal = starSignal;
                            }

                            categoryScores[cat] = categoryScores.GetValueOrDefault(cat) + finalSignal;

                            // התקדמות רמת קושי: רק אם החוויה הייתה חיובית (כוכבים גבוהים)
                            if (rating >= 4)
                                difficultyScores[r["Difficulty"].ToString()] = difficultyScores.GetValueOrDefault(r["Difficulty"].ToString()) + 1.0;
                        }

                        // סט 3: אינטראקציה חברתית
                        r.NextResult();
                        while (r.Read())
                        {
                            double weight = Convert.ToInt32(r["SocialScore"]) * 0.4;
                            categoryScores[r["Category"].ToString()] = categoryScores.GetValueOrDefault(r["Category"].ToString()) + weight;
                        }

                        // סט 4: מועמדים
                        r.NextResult();
                        while (r.Read())
                        {
                            candidateTrips.Add(new
                            {
                                TripId = (int)r["TripId"],
                                Title = r["Title"].ToString(),
                                Category = r["Category"].ToString(),
                                Difficulty = r["Difficulty"].ToString(),
                                CityName = r["CityName"].ToString(),
                                ImageUrl = r["ImageUrl"]?.ToString(),
                                Location = r["Location"]?.ToString() ?? ""
                            });
                        }
                    }
                }

                // ⭐ קטגוריות מועדפות מהפרופיל (Preferences) — מפוצלות לרשימה נקייה
                var preferredCategories = new HashSet<string>(
                    (userPrefs ?? "")
                        .Split(',')
                        .Select(p => p.Trim())
                        .Where(p => p.Length > 0)
                );

                // 6. קטגוריות "מוכרות" — דירוג מפורש / אינטראקציה חברתית חיובית / העדפה מהפרופיל
                // (הוספת ההעדפות כאן מונעת שקטגוריה שהמשתמש בחר במפורש תיחשב "גילוי חדש")
                var knownCategories = new HashSet<string>(
                    categoryScores.Where(kv => kv.Value > 0).Select(kv => kv.Key)
                );
                foreach (var pc in preferredCategories) knownCategories.Add(pc);

                // 7. חישוב MatchScore לכל מועמד
                var scored = candidateTrips.Select(trip =>
                {
                    string category = trip.Category;
                    string difficulty = trip.Difficulty;
                    string tripCity = trip.CityName;

                    // ===== תיקון נירמול =====
                    // categoryScores מצטבר על פני כל הדירוגים והאינטראקציות באותה קטגוריה,
                    // ולכן הוא חסר חסם (יכול להגיע ל-+10 ויותר). בעבר זה ניפח את הציון
                    // הרבה מעבר ל-100. כאן מקצרים ל-[-1..1] כדי לשמור על משקל ±25 עקבי.
                    double catScore = Math.Clamp(categoryScores.GetValueOrDefault(category, 0.0), -1.0, 1.0);

                    // ⭐ דחיפת העדפות: אם המשתמש בחר קטגוריה זו בפרופיל, מחזקים את ציון הקטגוריה.
                    // חשוב במיוחד ל-Cold Start (משתמש חדש כמעט בלי דירוגים): קטגוריה מועדפת
                    // עם catScore=0 תעלה ל-0.4 → תוספת ~10 נק' שמצליחה להעלות אותה מעל סף 30.
                    bool isPreferred = preferredCategories.Contains(category);
                    if (isPreferred)
                        catScore = Math.Clamp(catScore + 0.4, -1.0, 1.0);

                    double diffScore = ScoreDifficultyProgression(difficulty, totalTrips); // [0..1]
                    double geoScore = ScoreGeo(userCity, tripCity);                      // [0..1]

                    // ציון מנורמל [0..100]
                    double matchScore = 50
                        + catScore * 25   // קטגוריה (כולל דחיפת העדפות): ±25
                        + diffScore * 15   // קושי מתאים: עד +15
                        + geoScore * 10;  // קרוב גיאוגרפית: עד +10

                    // חסם סופי קשיח כדי להבטיח טווח חוקי 0..100
                    matchScore = Math.Clamp(matchScore, 0, 100);

                    return new
                    {
                        trip.TripId,
                        trip.Title,
                        trip.Category,
                        trip.ImageUrl,
                        trip.Location,
                        MatchScore = (int)Math.Round(matchScore),
                        IsExploration = !knownCategories.Contains(category)
                    };
                })
                .Where(t => t.MatchScore >= 30)
                .OrderByDescending(t => t.MatchScore)
                .ToList();

                // 8. Diversity Filter — מקסימום 2 מאותה קטגוריה + לפחות 1 exploration
                var result = new List<dynamic>();
                var catCount = new Dictionary<string, int>();
                bool hasExploration = false;

                foreach (var trip in scored)
                {
                    if (result.Count >= 4) break;
                    catCount.TryGetValue(trip.Category, out int c);
                    if (c >= 2) continue;

                    if (trip.IsExploration) hasExploration = true;
                    catCount[trip.Category] = c + 1;
                    result.Add(trip);
                }

                // אם אין exploration — מחליפים את הפריט האחרון
                if (!hasExploration && result.Count == 4)
                {
                    var exploration = scored.FirstOrDefault(t =>
                        t.IsExploration &&
                        !result.Any(r => r.TripId == t.TripId));
                    if (exploration != null)
                        result[3] = exploration;
                }

                return new { recommended = result };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Hub AI v3: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// ציון קושי לפי רמת ניסיון: מתחיל → קל, ותיק → בינוני-קשה
        /// </summary>
        private double ScoreDifficultyProgression(string difficulty, int totalTrips)
        {
            int level = totalTrips <= 3 ? 0 : totalTrips <= 10 ? 1 : 2;
            return difficulty?.ToLower() switch
            {
                "קל" => level == 0 ? 1.0 : level == 1 ? 0.5 : 0.1,
                "בינוני" => level == 0 ? 0.4 : level == 1 ? 1.0 : 0.7,
                "קשה" => level == 0 ? 0.0 : level == 1 ? 0.4 : 1.0,
                _ => 0.5
            };
        }

        /// <summary>
        /// ציון גיאוגרפי גמיש — בדיקה חלקית עם מיפוי אזורי
        /// </summary>
        private double ScoreGeo(string userCity, string tripCity)
        {
            if (string.IsNullOrEmpty(userCity) || string.IsNullOrEmpty(tripCity))
                return 0.3;

            string u = userCity.Trim().ToLower();
            string t = tripCity.Trim().ToLower();

            if (t.Contains(u) || u.Contains(t)) return 1.0;

            var regionMap = new Dictionary<string, string[]>
            {
                ["צפון"] = new[] { "חיפה", "נצרת", "עכו", "טבריה", "צפת" },
                ["מרכז"] = new[] { "תל אביב", "רמת גן", "פתח תקווה", "ראשון לציון", "חולון" },
                ["דרום"] = new[] { "באר שבע", "אילת", "נתיבות", "אשדוד", "אשקלון" },
                ["ירושלים"] = new[] { "ירושלים", "בית שמש", "מודיעין" }
            };

            string userRegion = regionMap.FirstOrDefault(kv => kv.Value.Any(c => u.Contains(c))).Key;
            string tripRegion = regionMap.FirstOrDefault(kv => kv.Value.Any(c => t.Contains(c))).Key;

            if (userRegion != null && userRegion == tripRegion) return 0.6;

            return 0.1;
        }

        /// <summary>
        /// שליפת קטלוג הטיולים המלא
        /// מותאם לתצוגה מהירה ב-React (אובייקט אנונימי ופורמט תאריך ISO)
        /// </summary>
        public IEnumerable<object> GetAllTrips()
        {
            var allTrips = new List<object>();
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    // קריאה לפרוצדורה החדשה
                    SqlCommand cmd = new SqlCommand("SP_GetAllTrips_Nature", connection);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    connection.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            allTrips.Add(new
                            {
                                TripId = (int)reader["TripId"],
                                Title = reader["Title"].ToString(),
                                Category = reader["Category"].ToString(),
                                ImageUrl = reader["ImageUrl"] != DBNull.Value ? reader["ImageUrl"].ToString() : null,
                                // שמירה על פורמט התאריך שמתאים ל-React
                                TripDate = reader["TripDate"] != DBNull.Value ? Convert.ToDateTime(reader["TripDate"]).ToString("yyyy-MM-dd") : "",
                                About = reader["About"] != DBNull.Value ? reader["About"].ToString() : ""
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllTrips: {ex.Message}");
                throw;
            }
            return allTrips;
        }
        /// <summary>
        /// מתודת עזר למיפוי שורה מהמסד לאובייקט Trip
        /// </summary>
        private Trip MapReaderToTrip(SqlDataReader reader)
        {
            return new Trip
            {
                TripId = (int)reader["TripId"],
                Title = reader["Title"].ToString(),
                Category = reader["Category"].ToString(),
                ImageUrl = reader["ImageUrl"] != DBNull.Value ? reader["ImageUrl"].ToString() : null,
                Location = reader["Location"].ToString(),
                Subtitle = reader["Subtitle"] != DBNull.Value ? reader["Subtitle"].ToString() : null,
                TripDate = reader["TripDate"] != DBNull.Value ? Convert.ToDateTime(reader["TripDate"]) : DateTime.MinValue,
                Description = reader["Description"] != DBNull.Value ? reader["Description"].ToString() : null,
                About = reader["About"] != DBNull.Value ? reader["About"].ToString() : null,
                Difficulty = reader["Difficulty"] != DBNull.Value ? reader["Difficulty"].ToString() : null,
                TargetAudience = reader["TargetAudience"] != DBNull.Value ? reader["TargetAudience"].ToString() : null,
                Equipment = reader["Equipment"] != DBNull.Value ? reader["Equipment"].ToString() : null,
                Guide = reader["Guide"] != DBNull.Value ? reader["Guide"].ToString() : null,
                RouteLength = reader["RouteLength"] != DBNull.Value ? reader["RouteLength"].ToString() : null,
                IsAccessible = reader["IsAccessible"] != DBNull.Value && (bool)reader["IsAccessible"],
          
            };
        }

        public bool UpdateAttendanceStatus(int userId, int tripId, bool attendanceStatus)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    SqlCommand cmd = new SqlCommand("SP_UpdateTripAttendance_Nature", connection);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@TripId", tripId);
                    cmd.Parameters.AddWithValue("@AttendanceStatus", attendanceStatus); 

                    connection.Open();
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateAttendanceStatus: {ex.Message}");
                return false;
            }
        }

        public bool? GetAttendanceStatus(int userId, int tripId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    SqlCommand cmd = new SqlCommand(
                        "SELECT AttendanceStatus FROM UserTrips_Nature WHERE UserId = @UserId AND TripId = @TripId",
                        connection);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@TripId", tripId);
                    connection.Open();
                    var result = cmd.ExecuteScalar();
                    if (result == null || result == DBNull.Value) return null;
                    return Convert.ToBoolean(result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAttendanceStatus: {ex.Message}");
                return null;
            }
        }
    }
}
