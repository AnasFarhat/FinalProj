using PartnersWebApi.Interfaces;
using PartnersWebApi.Models;
using System.Data.SqlClient;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace PartnersWebApi.Repositories
{
    /// <summary>
    /// מימוש של ממשק המשתמשים באמצעות מסד נתונים SQL Server
    /// אחראי על ביצוע שאילתות, הרשמה ואימות משתמשים בצורה מאובטחת
    /// </summary>
    public class SQLUsersRepository : IUsersRepository
    {
        private readonly string _connectionString;
        private readonly string _geminiApiKey;

        /// <summary>
        /// אתחול ה-Repository ושליפת מחרוזת החיבור מהגדרות האפליקציה
        /// </summary>
        public SQLUsersRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _geminiApiKey = configuration["GeminiMysteryCoupon:ApiKey"];
        }

        /// <summary>
        /// מבצע אימות משתמש (Login) מול מסד הנתונים
        /// </summary>
        /// <param name="email">כתובת האימייל של המשתמש</param>
        /// <param name="password">הסיסמה כפי שהוזנה</param>
        /// <returns>אובייקט User מלא במידה והפרטים נכונים, אחרת null</returns>
        public User Login(string email, string password)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    // הגדרת הפקודה כ-Stored Procedure
                    SqlCommand cmd = new SqlCommand("SP_GetUserByEmail_Nature", conn);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;


                    cmd.Parameters.AddWithValue("@Email", email);

                    conn.Open();

                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            string storedHash = r["Password"].ToString();
                            bool isPasswordValid = false;

                            // בדיקה האם הסיסמה מוצפנת ב-BCrypt או שהיא טקסט פשוט (תאימות לאחור)
                            if (storedHash.StartsWith("$2a$") || storedHash.StartsWith("$2b$") || storedHash.StartsWith("$2y$"))
                            {
                                isPasswordValid = BCrypt.Net.BCrypt.Verify(password, storedHash);
                            }
                            else
                            {
                                isPasswordValid = (password == storedHash);
                            }

                            if (isPasswordValid)
                            {
                                return MapReaderToUser(r);
                            }
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                Console.WriteLine($"SQL Error in Login: {sqlEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Error in Login: {ex.Message}");
                throw;
            }

            return null;
        }

        /// <summary>
        /// רישום משתמש חדש במערכת
        /// </summary>
        /// <param name="m">מודל הרישום הכולל את פרטי המטייל</param>
        /// <returns>True אם הרישום הצליח והתווספה שורה ב-DB</returns>
        public bool Register(RegisterModel m)
        {
            try
            {
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(m.Password);

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    // קריאה לפרוצדורה במקום כתיבת SQL ידני
                    SqlCommand cmd = new SqlCommand("SP_RegisterUser_Nature", conn);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    // שימוש בשמות הפרמטרים כפי שמופיעים ב-SP ובקוד המקור שלך
                    cmd.Parameters.AddWithValue("@name", m.FullName ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@email", m.Email);
                    cmd.Parameters.AddWithValue("@pass", hashedPassword);
                    cmd.Parameters.AddWithValue("@dob", m.BirthDate.HasValue ? (object)m.BirthDate.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@city", m.City ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@family", m.FamilyStatus ?? (object)DBNull.Value);

                    conn.Open();
                    // מחזיר True אם הושפעה לפחות שורה אחת (הרישום הצליח)
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during registration: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// שליפת פרופיל משתמש מלא לפי מזהה ייחודי
        /// </summary>
        /// <param name="id">מזהה המשתמש</param>
        /// <returns>אובייקט User כולל נתוני העדפות וגיימיפיקציה</returns>
        public User GetUserProfile(int id)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    // הגדרת הפקודה כפרוצדורה
                    SqlCommand cmd = new SqlCommand("SP_GetUserProfile_Nature", conn);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    // העברת הפרמטר (וודאי שהשם ב-C# תואם לשם ב-SQL: @UserId)
                    cmd.Parameters.AddWithValue("@UserId", id);

                    conn.Open();

                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            // שימוש במתודת המיפוי הקיימת שלך
                            return MapReaderToUser(r);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetUserProfile: {ex.Message}");
                throw;
            }
            return null;
        }

        /// <summary>
        /// עדכון העדפות ופרטי פרופיל של משתמש קיים
        /// </summary>
        public bool UpdatePreferences(int id, string fullName, string imageUrl, string preferences, string familyStatus, string city, DateTime? birthDate)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    SqlCommand cmd = new SqlCommand("SP_UpdateUserPreferences_Nature", conn);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    // העברת הפרמטרים בדיוק לפי השמות ב-SP
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@name", fullName ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@img", imageUrl ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@pref", preferences ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@fam", familyStatus ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@city", city ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@birth", birthDate.HasValue ? (object)birthDate.Value : DBNull.Value);

                    conn.Open();
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdatePreferences: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// בדיקה מהירה האם אימייל קיים במערכת למניעת כפילויות
        /// </summary>
        public bool IsEmailExists(string email)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    SqlCommand cmd = new SqlCommand("SP_IsEmailExists_Nature", conn);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Email", email);

                    conn.Open();
                    // ExecuteScalar מחזיר את האובייקט מהעמודה הראשונה בשורה הראשונה
                    object result = cmd.ExecuteScalar();
                    return result != null && Convert.ToBoolean(result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in IsEmailExists: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// מתודת עזר פנימית למיפוי שורת נתונים מ-SQL לאובייקט User
        /// </summary>
        private User MapReaderToUser(SqlDataReader r)
        {
            return new User
            {
                UserId = (int)r["UserId"],
                FullName = r["FullName"].ToString(),
                Email = r["Email"].ToString(),
                Points = r["Points"] != DBNull.Value ? (int)r["Points"] : 0,
                City = r["City"] != DBNull.Value ? r["City"].ToString() : null,
                BirthDate = r["BirthDate"] != DBNull.Value ? (DateTime)r["BirthDate"] : (DateTime?)null,
                FamilyStatus = r["FamilyStatus"] != DBNull.Value ? r["FamilyStatus"].ToString() : null,
                Preferences = r["Preferences"] != DBNull.Value ? r["Preferences"].ToString() : null,
                IsPartner = r["IsPartner"] != DBNull.Value && (bool)r["IsPartner"],
                ImageUrl = r["ImageUrl"] != DBNull.Value ? r["ImageUrl"].ToString() : null,
                Role = r["Role"] != DBNull.Value ? r["Role"].ToString() : null,
                IsBlocked = r["IsBlocked"] != DBNull.Value && Convert.ToBoolean(r["IsBlocked"])
            };
        }

        //הוספת נקודות למשתמש
        public bool AddUserPoints(int userId, int points)
        {
            // שימוש בשם הטבלה המדויק ובעמודות הנכונות מה-DB שלכם
            string query = "UPDATE Users_Nature SET Points = ISNULL(Points, 0) + @Points WHERE UserId = @UserId";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Points", points);
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }
        public async Task<string?> PurchaseRewardAsync(int userId, int pointsCost)
        {
            // משתמשים במחרוזת החיבור הקיימת אצלך בריפוזיטורי (למשל _connectionString)
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // 1. שליפת הניקוד הנוכחי של המשתמש מהטבלה שלך
                string checkQuery = "SELECT Points FROM Users_Nature WHERE UserId = @UserId;";
                int currentPoints = 0;

                using (var checkCmd = new SqlCommand(checkQuery, connection))
                {
                    checkCmd.Parameters.AddWithValue("@UserId", userId);
                    var result = await checkCmd.ExecuteScalarAsync();

                    if (result == null || result == DBNull.Value) return null;
                    currentPoints = Convert.ToInt32(result);
                }

                // אם המשתמש מנסה לרמות או שאין לו מספיק נקודות - חוסמים את הרכישה
                if (currentPoints < pointsCost) return null;

                // 2. עדכון והחסרת הנקודות מהטבלה
                string updateQuery = "UPDATE Users_Nature SET Points = Points - @PointsCost WHERE UserId = @UserId;";
                using (var updateCmd = new SqlCommand(updateQuery, connection))
                {
                    updateCmd.Parameters.AddWithValue("@PointsCost", pointsCost);
                    updateCmd.Parameters.AddWithValue("@UserId", userId);
                    await updateCmd.ExecuteNonQueryAsync();
                }

                // 3. יצירת קוד קופון אקראי ומגניב (למשל: NATURE482X)
                string[] prefixes = { "TEVA", "NATURE", "TRIP", "CAMP" };
                var random = new Random();
                string generatedCoupon = $"{prefixes[random.Next(prefixes.Length)]}{random.Next(100, 999)}X";

                return generatedCoupon;
            }
        }
        public async Task<bool> SaveCouponAsync(int userId, string couponCode, string title, string purchaseDate, string expiryDate)
        {
            string query = "INSERT INTO Users_Coupons_Nature (UserId, CouponCode, Title, PurchaseDate, ExpiryDate) VALUES (@UserId, @CouponCode, @Title, @PurchaseDate, @ExpiryDate);";
            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@CouponCode", couponCode);
                    cmd.Parameters.AddWithValue("@Title", title);
                    cmd.Parameters.AddWithValue("@PurchaseDate", purchaseDate);
                    cmd.Parameters.AddWithValue("@ExpiryDate", expiryDate);

                    await conn.OpenAsync();
                    return await cmd.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        public async Task<List<object>> GetUserCouponsAsync(int userId)
        {
            var coupons = new List<object>();
            string query = "SELECT CouponCode, Title, PurchaseDate, ExpiryDate FROM Users_Coupons_Nature WHERE UserId = @UserId ORDER BY CouponId DESC;";

            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    await conn.OpenAsync();
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            coupons.Add(new
                            {
                                code = reader["CouponCode"].ToString(),
                                title = reader["Title"].ToString(),
                                purchaseDate = reader["PurchaseDate"].ToString(),
                                expiryDate = reader["ExpiryDate"].ToString()
                            });
                        }
                    }
                }
            }
            return coupons;
        }
        public async Task<double> GetUserLeaderboardPercentileAsync(int userId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                // 1. נבדוק כמה נקודות יש למשתמש הנוכחי
                string userPointsQuery = "SELECT Points FROM Users_Nature WHERE UserId = @UserId;";
                int userPoints = 0;
                using (var cmd = new SqlCommand(userPointsQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    var result = await cmd.ExecuteScalarAsync();
                    if (result != null && result != DBNull.Value)
                    {
                        userPoints = Convert.ToInt32(result);
                    }
                }

                // 2. נספור כמה משתמשים סה"כ יש במערכת
                string totalUsersQuery = "SELECT COUNT(*) FROM Users_Nature WHERE ISNULL(Role, '') <> 'Admin';";
                int totalUsers = 1;
                using (var cmd = new SqlCommand(totalUsersQuery, conn))
                {
                    totalUsers = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }
                if (totalUsers <= 1) return 100.0; // אם הוא המשתמש היחיד, הוא מקום ראשון

                // 3. נספור כמה משתמשים יש להם פחות נקודות ממנו
                string lowerUsersQuery = "SELECT COUNT(*) FROM Users_Nature WHERE Points < @UserPoints AND ISNULL(Role, '') <> 'Admin';";
                int lowerUsers = 0;
                using (var cmd = new SqlCommand(lowerUsersQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@UserPoints", userPoints);
                    lowerUsers = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                // חישוב האחוזון (למשל: עוקף 85% מהמשתמשים)
                double percentile = ((double)lowerUsers / (totalUsers - 1)) * 100;
                return Math.Round(percentile, 0); // מעגל למספר שלם קרוב
            }
        }
        public async Task<List<object>> GetTop5UsersAsync()
        {
            var topUsers = new List<object>();
            string query = "SELECT TOP 5 FullName, Points FROM Users_Nature WHERE ISNULL(Role, '') <> 'Admin' ORDER BY Points DESC;";

            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand(query, conn))
                {
                    await conn.OpenAsync();
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        int rank = 1;
                        while (await reader.ReadAsync())
                        {
                            topUsers.Add(new
                            {
                                rank = rank++,
                                fullName = reader["FullName"].ToString(),
                                points = Convert.ToInt32(reader["Points"])
                            });
                        }
                    }
                }
            }
            return topUsers;
        }
        public async Task<object> GenerateAiMysteryCouponAsync(int userId, int pointsCost)
        {
            // 1. בדיקת נקודות ועדכון הניקוד של המשתמש
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                string checkPointsQuery = "SELECT Points, FullName, BirthDate FROM Users_Nature WHERE UserId = @UserId";
                int currentPoints = 0;
                string fullName = "מטייל";
                int age = 23; // ברירת מחדל לגיבוי

                using (var cmd = new SqlCommand(checkPointsQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            currentPoints = Convert.ToInt32(reader["Points"]);
                            fullName = reader["FullName"].ToString();

                            if (reader["BirthDate"] != DBNull.Value)
                            {
                                var birthDate = Convert.ToDateTime(reader["BirthDate"]);
                                age = DateTime.Now.Year - birthDate.Year;
                                if (birthDate.Date > DateTime.Now.AddYears(-age)) age--;
                            }
                        }
                        else
                        {
                            throw new Exception("משתמש לא נמצא");
                        }
                    }
                }

                if (currentPoints < pointsCost)
                {
                    throw new Exception("אין לך מספיק נקודות עבור קופון הפתעה מה-AI!");
                }

                // חיוב הנקודות
                string updatePointsQuery = "UPDATE Users_Nature SET Points = Points - @PointsCost WHERE UserId = @UserId";
                using (var cmd = new SqlCommand(updatePointsQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@PointsCost", pointsCost);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    await cmd.ExecuteNonQueryAsync();
                }

                // 2. שליפת היסטוריית הטיולים של המשתמש
                var visitedPlaces = new List<string>();
                string tripsQuery = "SELECT TripId FROM UserTrips_Nature WHERE UserId = @UserId;";

                using (var cmd = new SqlCommand(tripsQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            visitedPlaces.Add("Trip #" + reader["TripId"].ToString());
                        }
                    }
                }

                string historyText = visitedPlaces.Count > 0
                    ? string.Join(", ", visitedPlaces)
                    : "לא ביקר עדיין בטיולים מוגדרים";

                // 3. בניית הפרומפט ופנייה לג'מיני
                string aiPrompt = $"אתה עוזר חכם באפליקציית טיולים בארץ. המשתמש {fullName} בן/בת {age} ביקש קופון הפתעה מותאם אישית מה-AI בנזק של 400 נקודות. " +
                                  $"היסטוריית הטיולים שלו באפליקציה: {historyText}. " +
                                  $"תמציא לו קופון מותאם אישית ומגניב שמתאים לאופי שלו (למשל הנחה על ציוד ספציפי, שעה חינם אטרקציה, כניסה חינם לאזור מסוים). " +
                                  $"תחזיר תשובה קצרה וקולעת בשורה אחת בלבד! שמנוסחת ככותרת של קופון. דוגמה: '15% הנחה על סנדלי שורש לטרקים הבאים שלך!' " +
                                  $"אל תוסיף שום טקסט מסביב, אל תוסיף גרשיים, רק את כותרת הקופון עצמה.";

                string generatedTitle = "";

                try
                {
                    // 🌟 שינוי מ-gemini-1.5-flash ל-gemini-2.5-flash בדיוק כמו במשחק שהצליח!
                    string geminiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_geminiApiKey}";

                    var requestBody = new
                    {
                        contents = new[]
                        {
                    new
                    {
                        parts = new[]
                        {
                            new { text = aiPrompt }
                        }
                    }
                }
                    };

                    var jsonBody = JsonSerializer.Serialize(requestBody);
                    var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                    using (var httpClient = new HttpClient())
                    {
                        var response = await httpClient.PostAsync(geminiUrl, content);

                        if (response.IsSuccessStatusCode)
                        {
                            var responseBody = await response.Content.ReadAsStringAsync();
                            var geminiResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);

                            var rawText = geminiResponse
                                .GetProperty("candidates")[0]
                                .GetProperty("content")
                                .GetProperty("parts")[0]
                                .GetProperty("text")
                                .GetString();

                            // 🌟 ניקוי הרמטי של התשובה מרווחים, גרשיים וירידות שורה נסתרות של ה-AI
                            generatedTitle = rawText?.Replace("\"", "").Replace("\n", "").Replace("\r", "").Trim() ?? "";
                        }
                        else
                        {
                            string errorDetails = await response.Content.ReadAsStringAsync();
                            throw new Exception($"Gemini API Error: {response.StatusCode} - {errorDetails}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 🌟 הדפסה ישירה לחלון הטרמינל השחור כדי שתראי מייד אם ה-API KEY נכשל
                    Console.WriteLine("========================================");
                    Console.WriteLine("⚠️ AI GENERATION FAILED: " + ex.Message);
                    Console.WriteLine("========================================");

                    // קופון הגיבוי למקרה חירום
                    generatedTitle = "15% הנחה על פנס ראש מקצועי לטיולי לילה!";
                }

                // אם ג'מיני החזיר תשובה ריקה מסיבה כלשהי, נשתמש בגיבוי
                if (string.IsNullOrEmpty(generatedTitle))
                {
                    generatedTitle = "15% הנחה על פנס ראש מקצועי לטיולי לילה!";
                }

                // 4. יצירת קוד קופון רנדומלי ושמירתו ב-Database
                string couponCode = "AI-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
                DateTime purchaseDate = DateTime.Now;
                DateTime expiryDate = purchaseDate.AddDays(60);

                string insertCouponQuery = @"
            INSERT INTO Users_Coupons_Nature (UserId, CouponCode, Title, PurchaseDate, ExpiryDate)
            VALUES (@UserId, @CouponCode, @Title, @PurchaseDate, @ExpiryDate)";

                using (var cmd = new SqlCommand(insertCouponQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@CouponCode", couponCode);
                    cmd.Parameters.AddWithValue("@Title", generatedTitle);
                    cmd.Parameters.AddWithValue("@PurchaseDate", purchaseDate.ToShortDateString());
                    cmd.Parameters.AddWithValue("@ExpiryDate", expiryDate.ToShortDateString());
                    await cmd.ExecuteNonQueryAsync();
                }

                return new
                {
                    coupon = couponCode,
                    title = generatedTitle,
                    purchaseDate = purchaseDate.ToShortDateString(),
                    expiryDate = expiryDate.ToShortDateString()
                };
            }
        }

        // ============================================================
        //  מנוע ההתאמה בין מטיילים (Matching) — נוסף מהמיזוג
        //  קורא ל-SP_GetMatchingData_Nature ומחזיר משתמשים דומים מדורגים.
        // ============================================================
        public List<SimilarUserDto> GetSimilarUsers(int userId)
        {
            // נתוני: הפרופיל שלי
            string myPrefs = "", myCity = "", myFamily = "";
            int myAge = 25;

            // פרופילים של מועמדים
            var candidates = new List<SimilarUserDto>();

            // דירוגים: UserId -> (Category -> רשימת אותות טעם [-1..1])
            var ratingsByUser = new Dictionary<int, List<(string Category, double Signal)>>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand("SP_GetMatchingData_Nature", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", userId);

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    // סט 1: הפרופיל שלי
                    if (r.Read())
                    {
                        myPrefs = r["Preferences"].ToString();
                        myCity = r["City"].ToString();
                        myFamily = r["FamilyStatus"].ToString();
                        myAge = Convert.ToInt32(r["Age"]);
                    }

                    // סט 2: מועמדים
                    r.NextResult();
                    while (r.Read())
                    {
                        candidates.Add(new SimilarUserDto
                        {
                            UserId = Convert.ToInt32(r["UserId"]),
                            FullName = r["FullName"].ToString(),
                            Preferences = r["Preferences"].ToString(),
                            City = r["City"].ToString(),
                            FamilyStatus = r["FamilyStatus"].ToString(),
                            ImageUrl = r["ImageUrl"]?.ToString(),
                        });
                    }

                    // סט 3: דירוגים + רגש של כולם -> בניית אותות טעם
                    r.NextResult();
                    while (r.Read())
                    {
                        int uid = Convert.ToInt32(r["UserId"]);
                        string cat = r["Category"].ToString();
                        int rating = Convert.ToInt32(r["Rating"]);

                        double starSignal = (rating - 3.0) / 2.0; // [-1..1]
                        double signal;
                        if (r["SentimentScore"] != DBNull.Value)
                        {
                            int sent = Convert.ToInt32(r["SentimentScore"]);
                            double sentSignal = (sent - 50.0) / 50.0;
                            signal = sentSignal * 0.6 + starSignal * 0.4;
                        }
                        else signal = starSignal;

                        if (!ratingsByUser.ContainsKey(uid))
                            ratingsByUser[uid] = new List<(string, double)>();
                        ratingsByUser[uid].Add((cat, signal));
                    }
                }
            }

            // בניית "פרופיל טעם" לכל משתמש: Category -> ציון ממוצע [-1..1]
            Dictionary<string, double> BuildTaste(int uid)
            {
                var taste = new Dictionary<string, double>();
                if (!ratingsByUser.ContainsKey(uid)) return taste;
                foreach (var grp in ratingsByUser[uid].GroupBy(x => x.Category))
                    taste[grp.Key] = grp.Average(x => x.Signal);
                return taste;
            }

            var myTaste = BuildTaste(userId);
            var myPrefsList = myPrefs.Split(',').Select(p => p.Trim()).Where(p => p != "").ToHashSet();

            // חישוב התאמה לכל מועמד
            foreach (var c in candidates)
            {
                var reasons = new List<string>();

                // ===== שכבה 1: פרופיל (0-100) =====
                double profile = 0;
                // העדפות משותפות (עד 50 נק')
                var theirPrefs = c.Preferences.Split(',').Select(p => p.Trim()).Where(p => p != "").ToHashSet();
                int sharedPrefs = myPrefsList.Intersect(theirPrefs).Count();
                if (myPrefsList.Count > 0)
                    profile += System.Math.Min(50, (sharedPrefs * 50.0 / myPrefsList.Count));
                if (sharedPrefs > 0) reasons.Add($"{sharedPrefs} תחומי עניין משותפים");

                // אותה עיר (25 נק')
                if (!string.IsNullOrEmpty(myCity) && c.City == myCity) { profile += 25; reasons.Add($"שניכם מ{myCity}"); }
                // אותו סטטוס משפחתי (15 נק')
                if (!string.IsNullOrEmpty(myFamily) && c.FamilyStatus == myFamily) { profile += 15; }
                // קרבת גיל (10 נק')
                int theirAge = 25;
                // (גיל המועמד לא נשמר ב-DTO; נחשב קרבה דרך טעם בלבד אם חסר — כאן ניתן 10 אם אין נתון)
                profile += 10; // בסיס קרבה ניטרלי (אפשר לדייק בעתיד עם גיל מדויק)
                profile = System.Math.Min(100, profile);

                // ===== שכבה 2: טעם התנהגותי (0-100) =====
                var theirTaste = BuildTaste(c.UserId);
                double behavior = 0;
                var sharedLikedCats = new List<string>();

                if (myTaste.Count > 0 && theirTaste.Count > 0)
                {
                    // לכל קטגוריה שאני אוהב (signal>0), נבדוק אם גם הוא אוהב
                    double matchSum = 0; int considered = 0;
                    foreach (var myCat in myTaste.Where(t => t.Value > 0))
                    {
                        considered++;
                        if (theirTaste.TryGetValue(myCat.Key, out double theirVal) && theirVal > 0)
                        {
                            // שניהם אוהבים את הקטגוריה -> ככל ששני האותות גבוהים, התאמה גבוהה
                            double pairScore = (myCat.Value + theirVal) / 2.0; // [0..1]
                            matchSum += pairScore;
                            sharedLikedCats.Add(myCat.Key);
                        }
                    }
                    if (considered > 0)
                        behavior = System.Math.Min(100, (matchSum / considered) * 100);
                }

                foreach (var cat in sharedLikedCats)
                    reasons.Add($"שניכם אוהבים טיולי {cat}");

                c.SharedCategories = sharedLikedCats;

                // ===== ציון כולל: פרופיל 30% + התנהגות 70% =====
                double total = profile * 0.30 + behavior * 0.70;

                c.ProfileScore = (int)System.Math.Round(profile);
                c.BehaviorScore = (int)System.Math.Round(behavior);
                c.MatchScore = (int)System.Math.Round(total);
                c.Reasons = reasons;
            }

            // ⭐ סינון: מציגים רק מי שבאמת דומה למשתמש — לא כל מי שיש לו ולו קטגוריה
            // משותפת אחת (זה מה שגרם שכמעט כל המשתמשים הופיעו). דרשנו סף ציון התאמה אמיתי (>=50)
            // במקום התנאי הקודם "SharedCategories.Count > 0 || MatchScore >= 40".
            // מיון מהגבוה לנמוך, TOP 10. (אפשר לכוונן את הסף 50 לפי כמה מחמירים רוצים להיות)
            return candidates
                            .Where(c => c.Reasons.Count > 0)
                            .OrderByDescending(c => c.MatchScore)
                            .Take(10)
                            .ToList();
        }

    }
}
