using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PartnersWebApi.Interfaces;
using PartnersWebApi.Models;

namespace PartnersWebApi.Repositories
{
    /// <summary>
    /// מימוש מבוסס SQL Server של ניהול ההודעות והשיחות.
    /// כל הפעולות מתבצעות דרך Stored Procedures ייעודיים במסד הנתונים.
    /// </summary>
    public class SQLMessagesRepository : IMessagesRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// אתחול המחלקה וטעינת מחרוזת ההתחברות מקובץ ההגדרות.
        /// </summary>
        /// <param name="config">אובייקט ההגדרות שממנו נטענת מחרוזת ההתחברות.</param>
        public SQLMessagesRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        /// <summary>
        /// שליחת הודעה פרטית ממשתמש אחד למשתמש אחר דרך ה-SP: SP_SendMessage_Nature.
        /// </summary>
        /// <param name="senderId">מזהה המשתמש השולח.</param>
        /// <param name="receiverId">מזהה המשתמש המקבל.</param>
        /// <param name="content">תוכן ההודעה.</param>
        /// <returns>ערך העמודה Result שמחזיר ה-SP (למשל "ok").</returns>
        public string SendMessage(int senderId, int receiverId, string content)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand("SP_SendMessage_Nature", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@SenderId", senderId);
                cmd.Parameters.AddWithValue("@ReceiverId", receiverId);
                cmd.Parameters.AddWithValue("@Content", content);

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
        /// שליפת היסטוריית השיחה בין שני משתמשים דרך ה-SP: SP_GetConversation_Nature.
        /// </summary>
        /// <param name="myId">מזהה המשתמש הנוכחי.</param>
        /// <param name="otherId">מזהה המשתמש השני.</param>
        /// <returns>רשימת ההודעות בשיחה, ממופה לאובייקטי MessageDto.</returns>
        public List<MessageDto> GetConversation(int myId, int otherId)
        {
            List<MessageDto> list = new List<MessageDto>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand("SP_GetConversation_Nature", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@MyId", myId);
                cmd.Parameters.AddWithValue("@OtherId", otherId);

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new MessageDto
                        {
                            Id = Convert.ToInt32(r["Id"]),
                            SenderId = Convert.ToInt32(r["SenderId"]),
                            ReceiverId = Convert.ToInt32(r["ReceiverId"]),
                            Content = r["Content"]?.ToString(),
                            CreatedAt = Convert.ToDateTime(r["CreatedAt"]),
                            IsRead = Convert.ToBoolean(r["IsRead"])
                        });
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// שליפת רשימת כל השיחות של משתמש מסוים דרך ה-SP: SP_GetMyChats_Nature.
        /// </summary>
        /// <param name="userId">מזהה המשתמש.</param>
        /// <returns>רשימת השיחות של המשתמש, ממופה לאובייקטי ChatListItemDto.</returns>
        public List<ChatListItemDto> GetMyChats(int userId)
        {
            List<ChatListItemDto> list = new List<ChatListItemDto>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand("SP_GetMyChats_Nature", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", userId);

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new ChatListItemDto
                        {
                            UserId = Convert.ToInt32(r["UserId"]),
                            FullName = r["FullName"]?.ToString(),
                            ImageUrl = r["ImageUrl"]?.ToString(),
                            LastMessage = r["LastMessage"] == DBNull.Value ? null : r["LastMessage"].ToString(),
                            LastTime = r["LastTime"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["LastTime"]),
                            UnreadCount = Convert.ToInt32(r["UnreadCount"])
                        });
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// מחיקת הודעה בודדת דרך ה-SP: SP_DeleteMessage_Nature.
        /// בדיקת ההרשאה (שרק השולח מוחק) מתבצעת בתוך ה-SP במסד הנתונים.
        /// </summary>
        /// <param name="myId">מזהה המשתמש הנוכחי (המבקש למחוק).</param>
        /// <param name="messageId">מזהה ההודעה למחיקה.</param>
        /// <returns>ערך העמודה Result: "ok" / "forbidden" / "notfound".</returns>
        public string DeleteMessage(int myId, int messageId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand("SP_DeleteMessage_Nature", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@MyId", myId);
                cmd.Parameters.AddWithValue("@MessageId", messageId);

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (r.Read())
                        return r["Result"]?.ToString();
                }
            }
            return "error";
        }
    }
}
