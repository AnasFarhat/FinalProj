using PartnersWebApi.Interfaces;
using PartnersWebApi.Models;
using System.Data;
using System.Data.SqlClient;

namespace PartnersWebApi.Repositories
{
    /// <summary>
    /// מימוש מאגר הקהילה באמצעות מסד נתונים SQL Server
    /// מנהל את מערכת הפוסטים, התגובות, הלייקים ומנגנוני הדיווח והמחיקה המדורגת
    /// </summary>
    public class SQLCommunityRepository : ICommunityRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// אתחול ה-Repository ושליפת מחרוזת החיבור מהגדרות המערכת
        /// </summary>
        public SQLCommunityRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        /// <summary>
        /// שליפת פיד הקהילה המלא
        /// משתמש במילונים (Dictionaries) לייעול השליפה ומניעת בעיית N+1 Queries בטעינת תגובות ותמונות
        /// </summary>
        /// <param name="userId">מזהה המשתמש הצופה לצורך זיהוי לייקים אישיים</param>
        /// <returns>אוסף פוסטים הכולל מדיה ותגובות משולבות</returns>
        public IEnumerable<object> GetCommunityPosts(int userId)
        {
            var posts = new List<object>();
            var commentsDict = new Dictionary<int, List<object>>();
            var imagesDict = new Dictionary<int, List<string>>();

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    SqlCommand cmd = new SqlCommand("SP_GetCommunityFeed_Nature", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        // טבלה 1: תגובות
                        while (reader.Read())
                        {
                            int postId = (int)reader["PostId"];
                            if (!commentsDict.ContainsKey(postId)) commentsDict[postId] = new List<object>();
                            commentsDict[postId].Add(new
                            {
                                CommentId = (int)reader["CommentId"],
                                AuthorName = reader["AuthorName"].ToString(),
                                AuthorPic = reader["AuthorPic"] != DBNull.Value ? reader["AuthorPic"].ToString() : null,
                                CommentText = reader["CommentText"].ToString(),
                                CreatedAt = reader["CreatedAt"]
                            });
                        }

                        // טבלה 2: תמונות
                        reader.NextResult();
                        while (reader.Read())
                        {
                            int postId = (int)reader["PostId"];
                            if (!imagesDict.ContainsKey(postId)) imagesDict[postId] = new List<string>();
                            imagesDict[postId].Add(reader["ImageUrl"].ToString());
                        }

                        // טבלה 3: פוסטים
                        reader.NextResult();
                        while (reader.Read())
                        {
                            int postId = (int)reader["PostId"];
                            posts.Add(new
                            {
                                PostId = postId,
                                Content = reader["Content"].ToString(),
                                CreatedAt = reader["CreatedAt"],
                                AuthorName = reader["FullName"].ToString(),
                                AuthorPic = reader["ImageUrl"] != DBNull.Value ? reader["ImageUrl"].ToString() : null,
                                LikesCount = reader["LikesCount"],
                                IsLikedByCurrentUser = (bool)reader["IsLikedByCurrentUser"],
                                TripTitle = reader["TripTitle"] != DBNull.Value ? reader["TripTitle"].ToString() : null,
                                mediaUrls = imagesDict.GetValueOrDefault(postId, new List<string>()),
                                Comments = commentsDict.GetValueOrDefault(postId, new List<object>())
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetCommunityPosts: {ex.Message}");
                throw;
            }
            return posts;
        }

        /// <summary>
        /// יצירת פוסט חדש כולל שמירת תמונות מרובות בטרנזקציה אחת
        /// </summary>
        public int CreatePost(CreatePostRequest model)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    SqlCommand cmd = new SqlCommand("SP_CreatePostWithImages_Nature", conn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@UserId", model.UserId);
                    cmd.Parameters.AddWithValue("@Content", (object)model.Content ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@TripId", (object)model.TripId ?? DBNull.Value);

                    // חיבור רשימת התמונות למחרוזת אחת מופרדת בפסיקים
                    string imageUrls = model.ImageUrls != null ? string.Join(",", model.ImageUrls) : "";
                    cmd.Parameters.AddWithValue("@ImageUrlsConcat", imageUrls);

                    conn.Open();
                    object result = cmd.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreatePost: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// ביצוע Toggle ללייק (הוספה/הסרה) תוך אימות קיום קודם
        /// </summary>
        public string ToggleLike(LikeInput like)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    // קריאה לפרוצדורה החדשה
                    SqlCommand cmd = new SqlCommand("SP_ToggleLike_Nature", conn);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@PostId", like.PostId);
                    cmd.Parameters.AddWithValue("@UserId", like.UserId);

                    conn.Open();

                    // שימוש ב-ExecuteScalar כדי לקבל את מחרוזת התוצאה מה-SP
                    object result = cmd.ExecuteScalar();
                    return result?.ToString() ?? "Error";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ToggleLike: {ex.Message}");
                throw;
            }
        }
        /// <summary>
        /// מחיקת פוסט וכל המידע הנלווה אליו בטרנזקציה מדורגת (Cascade)
        /// כולל בדיקת בעלות למניעת מחיקה לא מורשית
        /// </summary>
        public int DeletePost(int postId, int userId, bool isAdmin )
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    SqlCommand cmd = new SqlCommand("SP_DeletePost_Nature", conn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@PostId", postId);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@IsAdmin", isAdmin);
                    // יצירת פרמטר לקבלת ה-Return Value מהפרוצדורה
                    SqlParameter returnValue = new SqlParameter();
                    returnValue.Direction = ParameterDirection.ReturnValue;
                    cmd.Parameters.Add(returnValue);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    // חובה לחלץ את הערך לתוך משתנה ולבדוק אותו ב-Debug
                    var val = returnValue.Value;
                    if (val == null || val == DBNull.Value) return 500;

                    int statusCode = (int)val;
                    Console.WriteLine($"DEBUG: SQL returned {statusCode}");
                    return statusCode;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeletePost: {ex.Message}");
                return 500;
            }
        }
        /// <summary>
        /// הוספת תגובה חדשה לפוסט
        /// </summary>
        public bool AddComment(CommentInput comment)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    SqlCommand cmd = new SqlCommand("SP_AddComment_Nature", conn);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@PostId", comment.PostId);
                    cmd.Parameters.AddWithValue("@UserId", comment.UserId);
                    cmd.Parameters.AddWithValue("@Content", comment.Content ?? "");

                    conn.Open();
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddComment: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// יצירת דיווח על תוכן פוגעני לצורך בדיקת מנהל
        /// </summary>
        public bool ReportContent(CreateReportRequest report)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    SqlCommand cmd = new SqlCommand("SP_ReportContent_Nature", conn);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    // הזרקת פרמטרים עם בדיקת null
                    cmd.Parameters.AddWithValue("@PostId", (object)report.PostId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CommentId", (object)report.CommentId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UserId", report.UserId);
                    cmd.Parameters.AddWithValue("@ReasonCategory", report.ReasonCategory ?? "");
                    cmd.Parameters.AddWithValue("@CustomReason", (object)report.CustomReason ?? DBNull.Value);

                    conn.Open();
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ReportContent: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// מחיקת תגובה מהמסד לפי מזהה
        /// </summary>
        public bool DeleteComment(int commentId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    // שימוש בפרוצדורה החדשה
                    SqlCommand cmd = new SqlCommand("SP_DeleteComment_Nature", conn);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@CommentId", commentId);

                    conn.Open();
                    // ExecuteNonQuery מחזיר את מספר השורות שהושפעו
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteComment: {ex.Message}");
                return false;
            }
        }
    }
}