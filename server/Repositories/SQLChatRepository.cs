using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PartnersWebApi.Interfaces;
using PartnersWebApi.Models;

namespace PartnersWebApi.Repositories
{
    /// <summary>
    /// מימוש מאגר נתוני הצ'אט באמצעות SQL Server.
    /// אחראי על שמירת היסטוריית שיחות, שליפת סשנים ושליפת נתוני טיולים
    /// לצורך התאמה אישית של תשובות הבוט.
    /// </summary>
    public class SQLChatRepository : IChatRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// אתחול ה-Repository באמצעות מחרוזת החיבור למסד הנתונים
        /// מתוך הגדרות המערכת.
        /// </summary>
        public SQLChatRepository(IConfiguration config) => _connectionString = config.GetConnectionString("DefaultConnection");

        /// <summary>
        /// שומר הודעת משתמש ואת תגובת הבוט במסד הנתונים.
        /// הפעולה מתבצעת באמצעות Stored Procedure לצורך שמירת היסטוריית השיחה.
        /// </summary>
        /// <param name="userId">מזהה המשתמש.</param>
        /// <param name="message">הודעת המשתמש.</param>
        /// <param name="response">תגובת הבוט.</param>
        /// <param name="intent">הכוונה (Intent) שזוהתה עבור ההודעה.</param>
        /// <param name="sessionId">מזהה סשן השיחה.</param>
        public void SaveChat(int userId, string message, string response, string intent, string sessionId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("SP_SaveChat_Nature", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@Message", message);
                cmd.Parameters.AddWithValue("@Response", response);
                cmd.Parameters.AddWithValue("@Intent", intent);
                cmd.Parameters.AddWithValue("@SessionId", (object)sessionId ?? DBNull.Value);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// שליפת כל הטיולים של משתמש מסוים.
        /// הנתונים משמשים את הבוט לצורך מתן תשובות והמלצות מותאמות אישית.
        /// </summary>
        /// <param name="userId">מזהה המשתמש.</param>
        /// <returns>רשימת טיולים הכוללת את כל הנתונים הרלוונטיים.</returns>
        public List<Dictionary<string, object>> GetUserTripsDetailed(int userId)
        {
            var trips = new List<Dictionary<string, object>>();
            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("SP_GetUserTrips_Nature", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", userId);

                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < r.FieldCount; i++)
                            row[r.GetName(i)] = r.IsDBNull(i) ? null : r.GetValue(i);
                        trips.Add(row);
                    }
                }
            }
            return trips;
        }

        /// <summary>
        /// שליפת כל סשני הצ'אט של משתמש מסוים.
        /// כל סשן מייצג שיחה נפרדת עם הבוט.
        /// </summary>
        /// <param name="userId">מזהה המשתמש.</param>
        /// <returns>רשימת סשני השיחה של המשתמש.</returns>
        public async Task<List<object>> GetUserSessionsAsync(int userId)
        {
            var sessions = new List<object>();
            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("SP_GetUserSessions_Nature", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", userId);

                await conn.OpenAsync();
                using (var r = await cmd.ExecuteReaderAsync())
                {
                    while (await r.ReadAsync())
                        sessions.Add(new
                        {
                            SessionId = r["SessionId"].ToString(),
                            StartDate = Convert.ToDateTime(r["StartDate"]).ToString("dd/MM/yyyy HH:mm")
                        });
                }
            }
            return sessions;
        }

        /// <summary>
        /// שליפת היסטוריית ההודעות של סשן מסוים.
        /// מאפשר הצגת רצף השיחה בין המשתמש לבוט.
        /// </summary>
        /// <param name="userId">מזהה המשתמש.</param>
        /// <param name="sessionId">מזהה סשן השיחה.</param>
        /// <returns>רשימת ההודעות שהוחלפו במהלך הסשן.</returns>
        public async Task<List<object>> GetChatHistoryBySessionAsync(int userId, string sessionId)
        {
            var history = new List<object>();
            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("SP_GetChatHistoryBySession_Nature", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@SessionId", sessionId);

                await conn.OpenAsync();
                using (var r = await cmd.ExecuteReaderAsync())
                {
                    while (await r.ReadAsync())
                        history.Add(new
                        {
                            id = r["Id"].ToString(),
                            user = r["Message"].ToString(),
                            bot = r["Response"].ToString(),
                            time = Convert.ToDateTime(r["CreatedAt"]).ToString("HH:mm")
                        });
                }
            }
            return history;
        }
    }
}