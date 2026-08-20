namespace PartnersWebApi.Services
{
    /// <summary>
    /// שירות רקע האחראי על ניקוי אוטומטי של סשנים,
    /// בקשות אינטראקציה וערוצי שיחה שאינם פעילים.
    /// השירות פועל באופן מחזורי לאורך כל זמן ריצת השרת.
    /// </summary>
    public class StaleSessionCleaner : BackgroundService
    {
        private readonly PresenceStore _store;

        /// <summary>
        /// אתחול שירות הרקע באמצעות מחלקת PresenceStore.
        /// </summary>
        /// <param name="store">
        /// מנהל הנתונים הזמניים של המשתמשים המחוברים.
        /// </param>
        public StaleSessionCleaner(PresenceStore store) => _store = store;

        /// <summary>
        /// מפעיל את תהליך הניקוי התקופתי של הנתונים הזמניים.
        /// בכל שתי דקות מתבצעת בדיקה והסרה של נתונים שאינם פעילים.
        /// </summary>
        /// <param name="ct">
        /// אסימון ביטול המשמש לעצירת שירות הרקע בצורה בטוחה.
        /// </param>
        /// <returns>משימה אסינכרונית המתבצעת כל עוד השירות פעיל.</returns>
        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(2), ct);
                _store.PurgeStale(TimeSpan.FromMinutes(5));
            }
        }
    }
}