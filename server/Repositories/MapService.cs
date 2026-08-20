using Dapper;
using System.Data.SqlClient;
using PartnersWebApi.Interfaces;
using PartnersWebApi.Models;

namespace PartnersWebApi.Repositories
{
    /// <summary>
    /// מימוש שירות המפה באמצעות SQL Server.
    /// אחראי על ניהול מסלולים, שמירת נקודות דרך ועדכון מיקומי המשתמשים בזמן אמת.
    /// </summary>
    public class MapService : IMapService
    {
        private readonly string _connString;

        /// <summary>
        /// אתחול השירות באמצעות מחרוזת החיבור למסד הנתונים
        /// מתוך הגדרות המערכת.
        /// </summary>
        public MapService(IConfiguration config) => _connString = config.GetConnectionString("DefaultConnection");

        /// <summary>
        /// שליפת כל המסלולים השמורים של משתמש מסוים,
        /// כולל כל נקודות הדרך המשויכות לכל מסלול.
        /// </summary>
        /// <param name="userId">מזהה המשתמש.</param>
        /// <returns>רשימת המסלולים של המשתמש.</returns>
        public async Task<List<PartnersWebApi.Models.Route>> GetRoutesAsync(int userId)
        {
            using var db = new SqlConnection(_connString);
            var sql = @"SELECT * FROM Routes WHERE UserId = @userId;
                        SELECT * FROM Waypoints WHERE RouteId IN (SELECT Id FROM Routes WHERE UserId = @userId) ORDER BY Sequence;";

            using var multi = await db.QueryMultipleAsync(sql, new { userId });
            var routes = (await multi.ReadAsync<PartnersWebApi.Models.Route>()).ToList();
            var waypoints = (await multi.ReadAsync<Waypoint>()).ToList();

            foreach (var r in routes)
                r.Waypoints = waypoints.Where(w => w.RouteId == r.Id).ToList();

            return routes;
        }

        /// <summary>
        /// מעדכן את מיקומו הנוכחי של המשתמש.
        /// אם למשתמש עדיין אין רשומת מיקום, תיווצר רשומה חדשה.
        /// </summary>
        /// <param name="userId">מזהה המשתמש.</param>
        /// <param name="lat">קו הרוחב (Latitude).</param>
        /// <param name="lng">קו האורך (Longitude).</param>
        /// <returns>משימה אסינכרונית המייצגת את פעולת העדכון.</returns>
        public async Task UpdateLocationAsync(int userId, double lat, double lng)
        {
            using var db = new SqlConnection(_connString);
            await db.ExecuteAsync(@"
            UPDATE LiveLocations SET Lat = @lat, Lng = @lng, UpdatedAt = GETDATE() WHERE UserId = @userId
            IF @@ROWCOUNT = 0 
            INSERT INTO LiveLocations (UserId, Lat, Lng, UpdatedAt) VALUES (@userId, @lat, @lng, GETDATE())",
            new { userId, lat, lng });
        }

        /// <summary>
        /// שומר מסלול חדש עבור משתמש,
        /// כולל כל נקודות הדרך המשויכות אליו במסגרת טרנזקציה אחת.
        /// </summary>
        /// <param name="userId">מזהה המשתמש.</param>
        /// <param name="dto">אובייקט המכיל את נתוני המסלול לשמירה.</param>
        /// <returns>משימה אסינכרונית המייצגת את פעולת השמירה.</returns>
        public async Task SaveRouteAsync(int userId, RouteDto dto)
        {
            using var db = new SqlConnection(_connString);
            db.Open();
            using var trans = db.BeginTransaction();

            try
            {
                // 1. Insert the main route - בלי Id (SQL מייצר אותו) ובלי Guid ל-ShareToken
                // השתמשתי ב-SCOPE_IDENTITY() כדי לקבל את ה-ID החדש שנוצר
                var sqlRoute = @"
            INSERT INTO Routes (UserId, Name, Profile, DistanceKm, ShareToken) 
            VALUES (@userId, @Name, @Profile, @DistanceKm, @ShareToken);
            SELECT CAST(SCOPE_IDENTITY() as int);";

                var routeId = await db.QuerySingleAsync<int>(sqlRoute, new
                {
                    userId,
                    dto.Name,
                    dto.Profile,
                    dto.DistanceKm,
                    ShareToken = Guid.NewGuid().ToString()
                }, trans);

                // 2. Insert all waypoints using the new routeId
                foreach (var (w, i) in dto.Waypoints.Select((w, i) => (w, i)))
                {
                    await db.ExecuteAsync(@"
                INSERT INTO Waypoints (RouteId, Lat, Lng, Label, Sequence) 
                VALUES (@routeId, @Lat, @Lng, @Label, @Sequence)",
                        new { routeId, Lat = w.Lat, Lng = w.Lng, Label = w.Label, Sequence = i },
                        trans);
                }

                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        /// <summary>
        /// שליפת כל המשתמשים הפעילים ששיתפו את מיקומם לאחרונה.
        /// משמש להצגת משתמשים בזמן אמת על גבי המפה.
        /// </summary>
        /// <returns>רשימת המשתמשים הפעילים והמיקומים שלהם.</returns>
        public async Task<List<LiveLocation>> GetActiveUsersAsync()
        {
            using var db = new SqlConnection(_connString);

            var sql = @"
        SELECT UserId, Lat, Lng, UpdatedAt 
        FROM LiveLocations 
        WHERE UpdatedAt > DATEADD(minute, -2, GETDATE())";

            var users = await db.QueryAsync<LiveLocation>(sql);
            return users.ToList();
        }

        /// <summary>
        /// מוחק מסלול שמור של משתמש,
        /// כולל כל נקודות הדרך המשויכות אליו,
        /// במסגרת טרנזקציה אחת לשמירה על שלמות הנתונים.
        /// </summary>
        /// <param name="userId">מזהה המשתמש.</param>
        /// <param name="routeId">מזהה המסלול למחיקה.</param>
        /// <returns>
        /// מחזיר true אם המסלול נמחק,
        /// אחרת false.
        /// </returns>
        public async Task<bool> DeleteRouteAsync(int userId, int routeId)
        {
            using var db = new SqlConnection(_connString);
            db.Open();
            using var trans = db.BeginTransaction();

            try
            {
                // מוחק קודם את נקודות הדרך
                await db.ExecuteAsync(
                    @"DELETE FROM Waypoints
              WHERE RouteId = @routeId",
                    new { routeId },
                    trans);

                // מוחק את המסלול רק אם הוא שייך למשתמש
                int rows = await db.ExecuteAsync(
                    @"DELETE FROM Routes
              WHERE Id = @routeId
              AND UserId = @userId",
                    new { routeId, userId },
                    trans);

                trans.Commit();

                return rows > 0;
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

    }
}