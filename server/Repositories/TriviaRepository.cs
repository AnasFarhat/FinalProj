using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PartnersWebApi.Interfaces;
using PartnersWebApi.Models;

namespace PartnersWebApi.Repositories
{
    /// <summary>
    /// מימוש מאגר נתוני הטריוויה באמצעות SQL Server.
    /// אחראי על זיהוי מיקום המשתמש, שמירת התקדמות במשחק,
    /// עדכון ניקוד, טעינת נקודות עניין ובדיקת השתתפות קודמת.
    /// </summary>
    public class TriviaRepository : ITriviaRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// אתחול ה-Repository באמצעות מחרוזת החיבור למסד הנתונים
        /// מתוך הגדרות המערכת.
        /// </summary>
        public TriviaRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// בודק האם מיקומו הנוכחי של המשתמש נמצא בתוך תחום הזיהוי
        /// של אחת מנקודות העניין במערכת.
        /// </summary>
        /// <param name="location">אובייקט המכיל את קואורדינטות המשתמש.</param>
        /// <returns>
        /// אובייקט Location אם נמצאה התאמה, אחרת Null.
        /// </returns>
        public async Task<Location?> CheckUserGeofenceAsync(UserLocationDto location)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand("sp_CheckGeofence", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@UserLat", location.Latitude);
                    command.Parameters.AddWithValue("@UserLng", location.Longitude);

                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new Location
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Name = reader["Name"].ToString() ?? string.Empty,
                                Latitude = Convert.ToDouble(reader["Latitude"]),
                                Longitude = Convert.ToDouble(reader["Longitude"]),
                                RadiusInMeters = Convert.ToDouble(reader["RadiusInMeters"])
                            };
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// שומר את תוצאות משחק הטריוויה של המשתמש
        /// ומעדכן את הניקוד שנצבר במידת הצורך.
        /// </summary>
        /// <param name="userId">מזהה המשתמש.</param>
        /// <param name="locationId">מזהה נקודת העניין שבה שוחק המשחק.</param>
        /// <param name="pointsEarned">מספר הנקודות שנצברו.</param>
        /// <param name="isCorrect">מציין האם המשתמש הצליח במשחק.</param>
        /// <returns>True אם השמירה הצליחה, אחרת False.</returns>
        public async Task<bool> SaveUserProgressAsync(int userId, int locationId, int pointsEarned, bool isCorrect)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string progressQuery = @"
            INSERT INTO UserTriviaProgress (UserId, LocationId, IsCorrect, AnsweredAt)
            VALUES (@UserId, @LocationId, @IsCorrect, @AnsweredAt);";

                int rowsProgress = 0;
                using (var progressCmd = new SqlCommand(progressQuery, connection))
                {
                    progressCmd.Parameters.AddWithValue("@UserId", userId);
                    progressCmd.Parameters.AddWithValue("@LocationId", locationId);
                    progressCmd.Parameters.AddWithValue("@IsCorrect", isCorrect ? 1 : 0);
                    progressCmd.Parameters.AddWithValue("@AnsweredAt", DateTime.UtcNow);

                    rowsProgress = await progressCmd.ExecuteNonQueryAsync();
                    Console.WriteLine($"[DEBUG SQL] Rows inserted into UserTriviaProgress: {rowsProgress}");
                }

                if (isCorrect && pointsEarned > 0)
                {
                    string updatePointsQuery = "UPDATE Users_Nature SET Points = ISNULL(Points, 0) + @Points WHERE UserId = @UserId;";
                    using (var pointsCmd = new SqlCommand(updatePointsQuery, connection))
                    {
                        pointsCmd.Parameters.AddWithValue("@Points", pointsEarned);
                        pointsCmd.Parameters.AddWithValue("@UserId", userId);

                        int rowsPoints = await pointsCmd.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DEBUG SQL] Rows updated in Users_Nature: {rowsPoints}");
                    }
                }

                return rowsProgress > 0;
            }
        }

        /// <summary>
        /// טוען את נקודות העניין מקובץ CSV אל מסד הנתונים.
        /// הפעולה מאפסת את הטבלה הקיימת ומייבאת את הנתונים מחדש.
        /// </summary>
        /// <returns>True אם טעינת הנתונים הצליחה, אחרת False.</returns>
        public async Task<bool> SeedLocationsFromGovAsync()
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "gov_locations.csv");
            if (!File.Exists(filePath)) return false;

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string cleanQuery = "DELETE FROM Locations; DBCC CHECKIDENT ('Locations', RESEED, 0);";
                    using (var cleanCommand = new SqlCommand(cleanQuery, connection))
                    {
                        await cleanCommand.ExecuteNonQueryAsync();
                    }

                    using (var reader = new StreamReader(filePath, System.Text.Encoding.UTF8))
                    {
                        string? headerLine = await reader.ReadLineAsync();
                        while (!reader.EndOfStream)
                        {
                            string? line = await reader.ReadLineAsync();
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            string[] values = line.Split(',');
                            if (values.Length >= 4)
                            {
                                string name = values[1].Trim();
                                if (double.TryParse(values[2].Trim(), out double lat) && double.TryParse(values[3].Trim(), out double lng))
                                {
                                    double radius = 150.0;
                                    if (values.Length >= 5 && double.TryParse(values[4].Trim(), out double parsedRadius))
                                        radius = parsedRadius;

                                    if (string.IsNullOrWhiteSpace(name)) continue;

                                    using (var command = new SqlCommand("sp_InsertLocationIfNotExist", connection))
                                    {
                                        command.CommandType = CommandType.StoredProcedure;
                                        command.Parameters.AddWithValue("@Name", name);
                                        command.Parameters.AddWithValue("@Lat", lat);
                                        command.Parameters.AddWithValue("@Lng", lng);
                                        command.Parameters.AddWithValue("@Radius", radius);
                                        await command.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                        }
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// בודק האם המשתמש כבר שיחק בנקודת עניין מסוימת
        /// במהלך מספר הימים שהוגדר.
        /// </summary>
        /// <param name="userId">מזהה המשתמש.</param>
        /// <param name="locationId">מזהה נקודת העניין.</param>
        /// <param name="days">מספר הימים לבדיקה.</param>
        /// <returns>
        /// True אם המשתמש כבר שיחק בתקופה זו, אחרת False.
        /// </returns>
        public async Task<bool> HasUserPlayedLocationRecentlyAsync(int userId, int locationId, int days)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                string query = @"
            SELECT COUNT(1) 
            FROM UserTriviaProgress 
            WHERE UserId = @UserId 
              AND LocationId = @LocationId 
              AND AnsweredAt >= DATEADD(day, -@Days, GETUTCDATE());";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@LocationId", locationId);
                    command.Parameters.AddWithValue("@Days", days);

                    await connection.OpenAsync();
                    int count = Convert.ToInt32(await command.ExecuteScalarAsync());

                    return count > 0;
                }
            }
        }
    }
}