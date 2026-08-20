using Microsoft.Extensions.Configuration;
using PartnersWebApi.Interfaces;
using PartnersWebApi.Models;
using System.Data;
using System.Data.SqlClient;

namespace PartnersWebApi.Repositories
{
    /// <summary>
    /// מימוש מאגר נתוני הניהול (Dashboard) באמצעות SQL Server
    /// אחראי על הפקת דוחות BI, ניתוח מדדי יעילות, ניהול משתמשים ובקרת תוכן קהילתי
    /// </summary>
    public class DashboardRepository : IDashboardRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// אתחול ה-Repository ושליפת מחרוזת החיבור מהגדרות המערכת
        /// </summary>
        public DashboardRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        /// <summary>
        /// שליפת מדדי ביצוע מרכזיים (KPIs) הכוללים כמות משתמשים וממוצע שביעות רצון
        /// </summary>
        /// <returns>אובייקט אנונימי המכיל נתונים מסוכמים ויעדים ארגוניים</returns>
        public object GetGeneralKPIs()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    SqlCommand cmd = new SqlCommand("SP_GetGeneralKPIs_Nature", conn);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new
                            {
                                TotalUsers = reader["TotalUsers"],
                                AvgSatisfaction = reader["AvgSatisfaction"] != DBNull.Value ? reader["AvgSatisfaction"] : 0,
                                ActiveHikers = reader["ActiveHikers"],
                                // נתונים ניהוליים שנשמרים ברמת הקוד
                                RetentionTarget = "25% Increase",
                                SatisfactionGoal = 4.5
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetGeneralKPIs: {ex.Message}");
                throw;
            }
            return null;
        }
        /// <summary>
        /// סיכום התפלגות הסנטימנט מכלל המשובים במערכת
        /// </summary>
        /// <returns>רשימת תוצאות הכוללת סיווג (חיובי/שלילי) וכמות מופעים</returns>
        public object GetSentimentSummary()
        {
            var results = new List<object>();
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    SqlCommand cmd = new SqlCommand("SP_GetSentimentSummary_Nature", conn);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new
                            {
                                Label = reader["SentimentStatus"].ToString(),
                                Value = Convert.ToInt32(reader["Count"])
                            });
                        }
                    }
                }
                return results;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetSentimentSummary: {ex.Message}");
                throw;
            }
        }



        /// <summary>
        /// שליפת רשימת משתמשים כולל סטטוס חסימה לניהול אדמיניסטרטיבי
        /// </summary>
        public IEnumerable<object> GetUsersList()
        {
            var users = new List<object>();
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    SqlCommand cmd = new SqlCommand("SP_GetUsersList_Nature", conn);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            users.Add(new
                            {
                                UserId = reader["UserId"],
                                FullName = reader["FullName"].ToString(),
                                Email = reader["Email"].ToString(),
                                IsBlocked = Convert.ToBoolean(reader["IsBlocked"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetUsersList: {ex.Message}");
                throw;
            }
            return users;
        }

        /// <summary>
        /// שינוי מצב חסימה של משתמש (חסימה או שחרור) באמצעות CASE ב-SQL
        /// </summary>
        public bool ToggleUserBlock(int userId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    SqlCommand cmd = new SqlCommand("SP_ToggleUserBlock_Nature", conn);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@UserId", userId);

                    conn.Open();
                    // מחזיר true אם שורה אחת לפחות עודכנה
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ToggleUserBlock: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// שליפת דיווחים על תוכן פוגעני וקיבוצם לפי פוסטים (Grouping)
        /// מאפשר למנהל לראות פירוט של מי דיווח, מתי ומהי סיבת הדיווח
        /// </summary>
        public IEnumerable<GroupedReportDto> GetGroupedReports()
        {
            var reportsDict = new Dictionary<int, GroupedReportDto>();
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    SqlCommand cmd = new SqlCommand("SP_GetGroupedReports_Nature", conn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int postId = (int)reader["PostId"];
                            if (!reportsDict.ContainsKey(postId))
                            {
                                reportsDict[postId] = new GroupedReportDto
                                {
                                    PostId = postId,
                                    PostContent = reader["PostContent"].ToString(),
                                    PostAuthor = reader["PostAuthor"].ToString(),
                                    IsHidden = reader["IsHidden"] != DBNull.Value && Convert.ToBoolean(reader["IsHidden"]),
                                    TotalReports = 0,
                                    ReportDetails = new List<ReportDetailDto>()
                                };
                            }

                            reportsDict[postId].TotalReports++;
                            reportsDict[postId].ReportDetails.Add(new ReportDetailDto
                            {
                                ReporterName = reader["ReporterName"].ToString(),
                                ReasonCategory = reader["ReasonCategory"].ToString(),
                                CustomReason = reader["CustomReason"] != DBNull.Value ? reader["CustomReason"].ToString() : null,
                                CreatedAt = (DateTime)reader["CreatedAt"]
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetGroupedReports: {ex.Message}");
                throw;
            }
            return reportsDict.Values.ToList();
        }
        /// <summary>
        /// עדכון נראות פוסט בקהילה (הסתרה בעקבות דיווחים או חשיפה מחדש)
        /// </summary>
        public bool TogglePostVisibility(int postId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    // קריאה לפרוצדורה החדשה
                    SqlCommand cmd = new SqlCommand("SP_TogglePostVisibility_Nature", conn);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@PostId", postId);

                    conn.Open();
                    // ExecuteNonQuery מחזיר את מספר השורות שהושפעו
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in TogglePostVisibility: {ex.Message}");
                return false;
            }
        }



        /// <summary>
        /// סקירת RSVP לכל הטיולים (עבר ועתיד).
        /// חדש: מבחין בין "טרם נשלחה בקשת אישור" (יותר מ-3 ימים לטיול) לבין
        /// אחוז אישורים אמיתי (הבקשה נשלחה — 3 ימים או פחות / הטיול עבר).
        /// כך אחוז 0% לא מוצג כשלילי כשעדיין לא נשלחה בקשה בכלל.
        /// </summary>
        public object GetAttendanceOverview()
        {
            // חלון שליחת בקשת האישור: הבקשה יוצאת 3 ימים לפני הטיול
            const int RSVP_WINDOW_DAYS = 3;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    SqlCommand cmd = new SqlCommand("SP_GetAttendanceStats_Nature", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        // הסיכום נספר רק על טיולים שהבקשה כבר נשלחה עבורם
                        int totalRegistered = 0, totalConfirmed = 0;
                        var tripList = new List<dynamic>();

                        while (reader.Read())
                        {
                            int tripId = Convert.ToInt32(reader["TripId"]);
                            string title = reader["Title"].ToString();
                            DateTime tripDate = Convert.ToDateTime(reader["TripDate"]);
                            int daysUntil = Convert.ToInt32(reader["DaysUntilTrip"]);

                            int reg = Convert.ToInt32(reader["RegisteredCount"]);
                            int confirmed = Convert.ToInt32(reader["ConfirmedCount"]);
                            int declined = Convert.ToInt32(reader["DeclinedCount"]);
                            int pending = Convert.ToInt32(reader["PendingCount"]);

                            bool isPast = daysUntil < 0;
                            // האם בקשת האישור כבר נשלחה? (טיול עבר, או נותרו 3 ימים או פחות)
                            bool rsvpSent = isPast || daysUntil <= RSVP_WINDOW_DAYS;

                            // אחוז אישורים — משמעותי רק אם הבקשה נשלחה
                            double rate = (rsvpSent && reg > 0)
                                ? Math.Round((double)confirmed / reg * 100, 1)
                                : 0;

                            // סטטוס לתצוגה
                            string status;
                            if (!rsvpSent)
                                status = "not_sent";                 // טרם נשלחה בקשת אישור
                            else if (reg == 0)
                                status = "no_registrations";         // אין נרשמים בכלל
                            else if (rate >= 70) status = "good";
                            else if (rate >= 40) status = "warning";
                            else status = "danger";

                            // צובר לסיכום רק טיולים שהבקשה נשלחה עבורם (אחרת נעוות את הממוצע)
                            if (rsvpSent)
                            {
                                totalRegistered += reg;
                                totalConfirmed += confirmed;
                            }

                            tripList.Add(new
                            {
                                TripId = tripId,
                                TripTitle = title,
                                TripDate = tripDate.ToString("yyyy-MM-dd"),   // ⭐ תאריך הטיול
                                DaysUntilTrip = daysUntil,                    // ⭐ ימים שנותרו (שלילי = עבר)
                                IsPast = isPast,
                                RsvpSent = rsvpSent,                          // ⭐ האם נשלחה בקשת אישור
                                Registered = reg,
                                Confirmed = confirmed,                        // ⭐ אישרו הגעה
                                Declined = declined,                          // ⭐ הודיעו שלא מגיעים
                                Pending = pending,                            // ⭐ טרם השיבו
                                Arrived = confirmed,                          // תאימות לאחור עם ה-UI הקיים
                                NoShow = reg - confirmed,
                                Rate = rate,
                                Status = status
                            });
                        }

                        return new
                        {
                            Summary = new
                            {
                                TotalRegistered = totalRegistered,
                                TotalArrived = totalConfirmed,
                                TotalConfirmed = totalConfirmed,
                                TotalNoShow = totalRegistered - totalConfirmed,
                                OverallRate = totalRegistered > 0
                                                  ? Math.Round((double)totalConfirmed / totalRegistered * 100, 1)
                                                  : 0
                            },
                            Trips = tripList
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAttendanceOverview: {ex.Message}");
                throw;
            }
        }
    }
}