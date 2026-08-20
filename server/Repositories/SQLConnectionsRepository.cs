using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PartnersWebApi.Interfaces;
using PartnersWebApi.Models;

namespace PartnersWebApi.Repositories
{
    /// <summary>
    /// מימוש מאגר נתוני הקשרים באמצעות SQL Server.
    /// אחראי על ניהול בקשות חברות, אישור או דחיית בקשות,
    /// שליפת קשרים קיימים ובדיקת סטטוס הקשר בין משתמשים.
    /// </summary>
    public class SQLConnectionsRepository : IConnectionsRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// אתחול ה-Repository באמצעות מחרוזת החיבור למסד הנתונים
        /// מתוך הגדרות המערכת.
        /// </summary>
        public SQLConnectionsRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        /// <summary>
        /// שולח בקשת חברות ממשתמש אחד למשתמש אחר.
        /// הפעולה מתבצעת באמצעות Stored Procedure המחזיר את סטטוס הבקשה.
        /// </summary>
        /// <param name="senderId">מזהה המשתמש השולח.</param>
        /// <param name="receiverId">מזהה המשתמש המקבל.</param>
        /// <param name="message">הודעה אופציונלית המצורפת לבקשה.</param>
        /// <returns>סטטוס הפעולה כפי שהוחזר ממסד הנתונים.</returns>
        public string SendRequest(int senderId, int receiverId, string message)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand("SP_SendConnectionRequest_Nature", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@SenderId", senderId);
                cmd.Parameters.AddWithValue("@ReceiverId", receiverId);
                cmd.Parameters.AddWithValue("@Message", (object)message ?? DBNull.Value);

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (r.Read())
                        return r["Result"]?.ToString();
                }
            }
            return "error";
        }

        /// <summary>
        /// מאשר או דוחה בקשת חברות שהתקבלה.
        /// </summary>
        /// <param name="requestId">מזהה בקשת החברות.</param>
        /// <param name="receiverId">מזהה המשתמש המקבל.</param>
        /// <param name="accept">מציין האם לאשר או לדחות את הבקשה.</param>
        /// <returns>True אם הפעולה הצליחה, אחרת False.</returns>
        public bool RespondRequest(int requestId, int receiverId, bool accept)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand("SP_RespondConnectionRequest_Nature", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@RequestId", requestId);
                cmd.Parameters.AddWithValue("@ReceiverId", receiverId);
                cmd.Parameters.AddWithValue("@Accept", accept);

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (r.Read())
                        return r["Result"]?.ToString() == "ok";
                }
            }
            return false;
        }

        /// <summary>
        /// שליפת כל בקשות החברות הממתינות עבור משתמש מסוים.
        /// </summary>
        /// <param name="userId">מזהה המשתמש.</param>
        /// <returns>רשימת בקשות החברות שהתקבלו.</returns>
        public List<ReceivedRequestDto> GetReceivedRequests(int userId)
        {
            List<ReceivedRequestDto> list = new List<ReceivedRequestDto>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand("SP_GetReceivedRequests_Nature", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", userId);

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new ReceivedRequestDto
                        {
                            RequestId = Convert.ToInt32(r["RequestId"]),
                            SenderId = Convert.ToInt32(r["SenderId"]),
                            FullName = r["FullName"]?.ToString(),
                            City = r["City"]?.ToString(),
                            ImageUrl = r["ImageUrl"]?.ToString(),
                            Preferences = r["Preferences"]?.ToString(),
                            Message = r["Message"]?.ToString(),
                            CreatedAt = Convert.ToDateTime(r["CreatedAt"])
                        });
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// בודק את סטטוס הקשר בין שני משתמשים.
        /// </summary>
        /// <param name="myId">מזהה המשתמש הנוכחי.</param>
        /// <param name="otherId">מזהה המשתמש השני.</param>
        /// <returns>סטטוס הקשר בין המשתמשים.</returns>
        public string GetConnectionStatus(int myId, int otherId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand("SP_GetConnectionStatus_Nature", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@MyId", myId);
                cmd.Parameters.AddWithValue("@OtherId", otherId);

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (r.Read())
                        return r["Status"]?.ToString();
                }
            }
            return "none";
        }

        /// <summary>
        /// שליפת כל הקשרים המאושרים של משתמש מסוים.
        /// </summary>
        /// <param name="userId">מזהה המשתמש.</param>
        /// <returns>רשימת החברים של המשתמש.</returns>
        public List<ConnectionDto> GetMyConnections(int userId)
        {
            List<ConnectionDto> list = new List<ConnectionDto>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand("SP_GetMyConnections_Nature", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", userId);

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new ConnectionDto
                        {
                            UserId = Convert.ToInt32(r["UserId"]),
                            FullName = r["FullName"]?.ToString(),
                            City = r["City"]?.ToString(),
                            ImageUrl = r["ImageUrl"]?.ToString(),
                            ConnectedAt = r["ConnectedAt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["ConnectedAt"])
                        });
                    }
                }
            }
            return list;
        }
    }
}