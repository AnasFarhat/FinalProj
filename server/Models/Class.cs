using System.Collections.Concurrent;

namespace PartnersWebApi.Models
{
    /// <summary>
    /// מייצג משתמש המחובר בזמן אמת למערכת,
    /// כולל פרטי החיבור, המיקום הנוכחי וסטטוס הזמינות שלו.
    /// </summary>
    public class UserSession
    {
        public string UserId { get; set; }
        public string ConnectionId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Status { get; set; } = "Available to chat";
        public bool SharingLocation { get; set; } = true;
        public DateTimeOffset LastSeen { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// מנהל את כל המשתמשים המחוברים בזמן אמת.
    /// אחראי על שמירת הסשנים הפעילים, שיתוף מיקומים,
    /// חסימות בין משתמשים וניקוי חיבורים שאינם פעילים.
    /// </summary>
    public class PresenceStore
    {
        private readonly ConcurrentDictionary<string, UserSession> _sessions = new();
        private readonly ConcurrentDictionary<string, string> _pendingRequests = new();
        private readonly ConcurrentDictionary<string, HashSet<string>> _blockList = new();

        /// <summary>
        /// מוסיף משתמש חדש לרשימת המשתמשים הפעילים או מעדכן את פרטי המשתמש הקיים.
        /// </summary>
        /// <param name="session">אובייקט המכיל את פרטי המשתמש המחובר.</param>
        public void AddOrUpdate(UserSession session) =>
            _sessions[session.ConnectionId] = session;

        /// <summary>
        /// מסיר משתמש מרשימת המשתמשים הפעילים לפי מזהה החיבור.
        /// </summary>
        /// <param name="connectionId">מזהה החיבור של המשתמש.</param>
        public void Remove(string connectionId) =>
            _sessions.TryRemove(connectionId, out _);

        /// <summary>
        /// מחזיר את פרטי המשתמש המחובר לפי מזהה החיבור.
        /// </summary>
        /// <param name="connectionId">מזהה החיבור.</param>
        /// <returns>אובייקט המשתמש אם נמצא, אחרת Null.</returns>
        public UserSession? Get(string connectionId) =>
            _sessions.TryGetValue(connectionId, out var s) ? s : null;

        /// <summary>
        /// מחזיר את כל המשתמשים הפעילים שבחרו לשתף את מיקומם.
        /// </summary>
        /// <returns>אוסף המשתמשים המשתפים מיקום.</returns>
        public IEnumerable<UserSession> GetAllSharing() =>
            _sessions.Values.Where(s => s.SharingLocation);

        /// <summary>
        /// מסיר חיבורים ישנים שלא היו פעילים מעבר לפרק הזמן שהוגדר.
        /// </summary>
        /// <param name="threshold">משך הזמן המקסימלי לחיבור פעיל.</param>
        public void PurgeStale(TimeSpan threshold)
        {
            var cutoff = DateTimeOffset.UtcNow - threshold;
            foreach (var key in _sessions.Keys)
                if (_sessions.TryGetValue(key, out var s) && s.LastSeen < cutoff)
                    _sessions.TryRemove(key, out _);
        }

        /// <summary>
        /// בודק האם משתמש נחסם על ידי משתמש אחר.
        /// </summary>
        /// <param name="requesterId">מזהה המשתמש המבקש.</param>
        /// <param name="targetId">מזהה המשתמש שאליו מתבצעת הפנייה.</param>
        /// <returns>True אם המשתמש חסום, אחרת False.</returns>
        public bool IsBlocked(string requesterId, string targetId) =>
            _blockList.TryGetValue(targetId, out var blocked) && blocked.Contains(requesterId);

        /// <summary>
        /// מוסיף משתמש לרשימת החסומים של משתמש אחר.
        /// </summary>
        /// <param name="userId">מזהה המשתמש המבצע את החסימה.</param>
        /// <param name="targetId">מזהה המשתמש שנחסם.</param>
        public void Block(string userId, string targetId) =>
            _blockList.GetOrAdd(userId, _ => new HashSet<string>()).Add(targetId);
    }
}