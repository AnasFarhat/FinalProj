using PartnersWebApi.Interfaces;
using PartnersWebApi.Models;
using System.Data;
using System.Data.SqlClient;

namespace PartnersWebApi.Repositories
{
    /// <summary>
    /// מימוש מאגר המשובים באמצעות מסד נתונים SQL Server
    /// מטפל בשמירת חוויות מטיילים, ניתוח סנטימנט ושליפת היסטוריית דירוגים
    /// </summary>
    public class SQLFeedbacksRepository : IFeedbacksRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// אתחול ה-Repository ושליפת מחרוזת החיבור מהגדרות המערכת
        /// </summary>
        public SQLFeedbacksRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        /// <summary>
        /// שמירת משוב חדש ועדכון הדירוג המשוקלל בטבלת הרישום לטיול
        /// הפעולה מבוצעת תחת Transaction להבטחת עקביות ושלמות הנתונים (Atomicity)
        /// </summary>
        /// <param name="feedback">אובייקט המשוב הגולמי מהמטייל</param>
        /// <param name="sentiment">תוצאת ניתוח הסנטימנט שחושבה בבקר</param>
        /// <param name="avgRating">דירוג ממוצע משוקלל לטיול</param>
        /// <returns>True אם כל שלבי השמירה והעדכון הצליחו</returns>
        public bool SaveFeedback(Feedback feedback, string sentiment, double avgRating, int sentimentScore, string sentimentSummary)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    SqlCommand cmd = new SqlCommand("SP_SaveFeedback_Nature", connection);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@UserId", feedback.UserId);
                    cmd.Parameters.AddWithValue("@TripId", feedback.TripId);
                    cmd.Parameters.AddWithValue("@GuideRating", feedback.GuideRating);
                    cmd.Parameters.AddWithValue("@TrackRating", feedback.TrackRating);
                    cmd.Parameters.AddWithValue("@FreeText", feedback.FreeText ?? "");
                    cmd.Parameters.AddWithValue("@SentimentStatus", sentiment);
                    cmd.Parameters.AddWithValue("@GoodTags", feedback.GoodTags ?? "");
                    cmd.Parameters.AddWithValue("@BadTags", feedback.BadTags ?? "");
                    cmd.Parameters.AddWithValue("@VoicePath", (object)feedback.VoiceFilePath ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@AvgRating", avgRating);
                    cmd.Parameters.AddWithValue("@SentimentScore", sentimentScore);                       // חדש
                    cmd.Parameters.AddWithValue("@SentimentSummary", (object)sentimentSummary ?? DBNull.Value); // חדש

                    connection.Open();
                    int result = Convert.ToInt32(cmd.ExecuteScalar());

                    return result == 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SaveFeedback (C# Wrapper): {ex.Message}");
                return false;
            }
        }


        /// <summary>
        /// שליפת היסטוריית המשובים המלאה של משתמש ספציפי
        /// מבצע Join עם טבלת הטיולים כדי להציג את שם הטיול לצד המשוב
        /// </summary>
        /// <param name="userId">מזהה המשתמש</param>
        /// <returns>אוסף אובייקטים אנונימיים המעוצבים לתצוגה ב-Frontend</returns>
        public IEnumerable<object> GetHistoryByUserId(int userId)
        {
            var history = new List<object>();
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    // קריאה לפרוצדורה החדשה
                    SqlCommand cmd = new SqlCommand("SP_GetFeedbackHistoryByUserId_Nature", conn);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@userId", userId);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // בניית אובייקט תוצאה המותאם ל-React
                            history.Add(new
                            {
                                FeedbackId = reader["FeedbackId"],
                                TripId = reader["TripId"],
                                TripTitle = reader["TripTitle"].ToString(),
                                GuideRating = reader["GuideRating"],
                                TrackRating = reader["TrackRating"],
                                FreeText = reader["FreeText"].ToString(),
                                CreatedAt = reader["CreatedAt"],
                                VoiceFilePath = reader["VoiceFilePath"] != DBNull.Value ? reader["VoiceFilePath"].ToString() : null
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetHistoryByUserId: {ex.Message}");
                throw;
            }

            return history;
        }
    }
}