using PartnersWebApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace PartnersWebApi.Interfaces
{
    /// <summary>
    /// ממשק המגדיר את הפעולות לניהול נתוני מפה, מיקומים ומסלולים של משתמשים במערכת.
    /// </summary>
    public interface IMapService
    {
        /// <summary>
        /// שליפת רשימת המשתמשים הפעילים המציגים את מיקומם בזמן אמת.
        /// </summary>
        /// <returns>רשימת מיקומים עדכניים של משתמשים פעילים.</returns>
        Task<List<LiveLocation>> GetActiveUsersAsync();

        /// <summary>
        /// שליפת כל המסלולים השמורים של משתמש מסוים.
        /// </summary>
        /// <param name="userId">מזהה המשתמש.</param>
        /// <returns>רשימת המסלולים המשויכים למשתמש.</returns>
        Task<List<PartnersWebApi.Models.Route>> GetRoutesAsync(int userId);

        /// <summary>
        /// שמירת מסלול חדש עבור משתמש.
        /// </summary>
        /// <param name="userId">מזהה המשתמש.</param>
        /// <param name="dto">אובייקט המכיל את נתוני המסלול לשמירה.</param>
        /// <returns>משימה אסינכרונית המייצגת את פעולת השמירה.</returns>
        Task SaveRouteAsync(int userId, RouteDto dto);

        /// <summary>
        /// עדכון המיקום הנוכחי של משתמש במערכת.
        /// </summary>
        /// <param name="userId">מזהה המשתמש.</param>
        /// <param name="lat">קו הרוחב (Latitude).</param>
        /// <param name="lng">קו האורך (Longitude).</param>
        /// <returns>משימה אסינכרונית המייצגת את פעולת העדכון.</returns>
        Task UpdateLocationAsync(int userId, double lat, double lng);
        /// <summary>
        /// מוחק מסלול שמור של משתמש,
        /// כולל כל נקודות הדרך המשויכות אליו.
        /// </summary>
        /// <param name="userId">מזהה המשתמש.</param>
        /// <param name="routeId">מזהה המסלול.</param>
        /// <returns>
        /// true אם המחיקה הצליחה,
        /// אחרת false.
        /// </returns>
        Task<bool> DeleteRouteAsync(int userId, int routeId);
    }
}