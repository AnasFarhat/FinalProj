using PartnersWebApi.Interfaces;
using PartnersWebApi.Models;
using System.Data.SqlClient;

namespace PartnersWebApi.Repository
{
    /// <summary>
    /// מימוש מאגר ההתראות באמצעות SQL Server.
    /// אחראי על ניהול ההתראות במערכת, סימונן כנקראו,
    /// שמירת טוקני FCM ושליפתם לצורך שליחת הודעות Push.
    /// </summary>
    public class SQLNotificationsRepository : INotificationsRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// אתחול ה-Repository באמצעות מחרוזת החיבור למסד הנתונים
        /// מתוך הגדרות המערכת.
        /// </summary>
        public SQLNotificationsRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        /// <summary>
        /// שליפת כל ההתראות של משתמש מסוים.
        /// </summary>
        /// <param name="userId">מזהה המשתמש.</param>
        /// <returns>אוסף ההתראות של המשתמש.</returns>
        public IEnumerable<object> GetByUserId(int userId)
        {
            List<object> list = new List<object>();
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    SqlCommand cmd = new SqlCommand("SP_GetNotificationsByUserId_Nature", conn);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new
                            {
                                Id = reader["Id"],
                                Title = reader["Title"].ToString(),
                                Message = reader["Message"].ToString(),
                                IsRead = reader["IsRead"],
                                CreatedAt = reader["CreatedAt"]
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetByUserId: {ex.Message}");
                throw;
            }
            return list;
        }

        /// <summary>
        /// מסמן התראה כנקראה.
        /// </summary>
        /// <param name="id">מזהה ההתראה.</param>
        /// <returns>True אם הפעולה הצליחה, אחרת False.</returns>
        public bool MarkAsRead(int id)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    SqlCommand cmd = new SqlCommand("SP_MarkNotificationAsRead_Nature", conn);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Id", id);
                    conn.Open();
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MarkAsRead: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// שומר התראה חדשה במסד הנתונים.
        /// ההתראה נשמרת לצורך הצגת היסטוריית ההתראות למשתמשים.
        /// </summary>
        /// <param name="req">אובייקט המכיל את פרטי ההתראה.</param>
        /// <returns>True אם השמירה הצליחה, אחרת False.</returns>
        public bool SendNotification(SendNotificationRequest req)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    SqlCommand cmd = new SqlCommand("SP_SendNotification_Nature", conn);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Title", req.Title);
                    cmd.Parameters.AddWithValue("@Message", req.Message);
                    cmd.Parameters.AddWithValue("@Type", req.Type ?? "General");
                    cmd.Parameters.AddWithValue("@TripId", (object)req.TripId ?? DBNull.Value);
                    conn.Open();
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendNotification: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// שומר או מעדכן את טוקן ה-FCM של המשתמש.
        /// הטוקן משמש לשליחת הודעות Push למכשיר של המשתמש.
        /// </summary>
        /// <param name="userId">מזהה המשתמש.</param>
        /// <param name="fcmToken">טוקן ה-FCM של המכשיר.</param>
        /// <returns>True אם הפעולה הצליחה, אחרת False.</returns>
        public bool SaveFcmToken(int userId, string fcmToken)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    SqlCommand cmd = new SqlCommand(
                        "UPDATE Users SET FcmToken = @Token WHERE UserId = @UserId", conn);
                    cmd.Parameters.AddWithValue("@Token", fcmToken);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    conn.Open();
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SaveFcmToken: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// שליפת טוקני ה-FCM של המשתמשים הרלוונטיים.
        /// ניתן לשלוף טוקנים עבור טיול מסוים או עבור כלל המשתמשים.
        /// </summary>
        /// <param name="tripId">
        /// מזהה הטיול. אם הערך הוא Null, יוחזרו הטוקנים של כל המשתמשים.
        /// </param>
        /// <returns>אוסף טוקני FCM לשליחת הודעות Push.</returns>
        public IEnumerable<string> GetFcmTokens(int? tripId)
        {
            var tokens = new List<string>();
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    string sql = tripId.HasValue
                        ? @"SELECT u.FcmToken
                            FROM Users u
                            INNER JOIN TripRegistrations tr ON tr.UserId = u.UserId
                            WHERE tr.TripId = @TripId
                              AND u.FcmToken IS NOT NULL
                              AND u.FcmToken <> ''"
                        : @"SELECT FcmToken
                            FROM Users
                            WHERE FcmToken IS NOT NULL
                              AND FcmToken <> ''";

                    SqlCommand cmd = new SqlCommand(sql, conn);
                    if (tripId.HasValue)
                        cmd.Parameters.AddWithValue("@TripId", tripId.Value);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            tokens.Add(reader["FcmToken"].ToString()!);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetFcmTokens: {ex.Message}");
                throw;
            }
            return tokens;
        }
    }
}