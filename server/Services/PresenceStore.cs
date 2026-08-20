using System.Collections.Concurrent;

namespace PartnersWebApi.Services
{
    /// <summary>
    /// מייצג בקשת אינטראקציה ממתינה בין שני משתמשים.
    /// כולל את מזהה החיבור של המשתמש השולח ואת זמן יצירת הבקשה.
    /// </summary>
    public record PendingRequest(string RequesterConnectionId, DateTimeOffset CreatedAt);

    /// <summary>
    /// מייצג ערוץ תקשורת פעיל בין שני משתמשים.
    /// משמש לניהול ואימות החברות בערוץ בזמן אמת.
    /// </summary>
    public record ActiveChannel(string UserIdA, string UserIdB, DateTimeOffset CreatedAt);

    /// <summary>
    /// מייצג משתמש המחובר למערכת בזמן אמת.
    /// כולל פרטי חיבור, מיקום, סטטוס וזמינות לשיתוף מיקום.
    /// </summary>
    public class UserSession
    {
        public string UserId { get; set; } = string.Empty;
        public string ConnectionId { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Status { get; set; } = "זמין לשיחה";
        public bool SharingLocation { get; set; } = false;
        public DateTimeOffset LastSeen { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// מנהל את כלל המידע הזמני של המשתמשים המחוברים למערכת בזמן אמת.
    /// אחראי על ניהול סשנים, בקשות אינטראקציה, ערוצי שיחה,
    /// שיתוף מיקום, חסימות והגבלת קצב בקשות.
    /// </summary>
    public class PresenceStore
    {
        private readonly ConcurrentDictionary<string, UserSession> _sessions = new();
        private readonly ConcurrentDictionary<string, PendingRequest> _pendingRequests = new();
        private readonly ConcurrentDictionary<string, ActiveChannel> _activeChannels = new();
        private readonly ConcurrentDictionary<string, (int Count, DateTimeOffset WindowStart)> _rateLimits = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _blockList = new();

        /// <summary>
        /// מוסיף משתמש חדש לרשימת המשתמשים הפעילים
        /// או מעדכן את פרטי המשתמש הקיים.
        /// </summary>
        /// <param name="session">פרטי המשתמש המחובר.</param>
        public void AddOrUpdate(UserSession session) =>
            _sessions[session.ConnectionId] = session;

        /// <summary>
        /// מסיר משתמש מרשימת המשתמשים הפעילים.
        /// </summary>
        /// <param name="connectionId">מזהה החיבור של המשתמש.</param>
        public void Remove(string connectionId) =>
            _sessions.TryRemove(connectionId, out _);

        /// <summary>
        /// מחזיר את פרטי המשתמש לפי מזהה החיבור.
        /// </summary>
        /// <param name="connectionId">מזהה החיבור.</param>
        /// <returns>אובייקט המשתמש אם נמצא, אחרת Null.</returns>
        public UserSession? Get(string connectionId) =>
            _sessions.TryGetValue(connectionId, out var s) ? s : null;

        /// <summary>
        /// מחזיר את פרטי המשתמש לפי מזהה המשתמש.
        /// </summary>
        /// <param name="userId">מזהה המשתמש.</param>
        /// <returns>אובייקט המשתמש אם נמצא, אחרת Null.</returns>
        public UserSession? GetByUserId(string userId) =>
            _sessions.Values.FirstOrDefault(s => s.UserId == userId);

        /// <summary>
        /// מחזיר את כל המשתמשים הפעילים שבחרו לשתף את מיקומם.
        /// </summary>
        /// <returns>אוסף המשתמשים המשתפים מיקום.</returns>
        public IEnumerable<UserSession> GetAllSharing() =>
            _sessions.Values.Where(s =>
                s.SharingLocation &&
                !(s.Latitude == 0 && s.Longitude == 0));

        /// <summary>
        /// מסיר מהמערכת סשנים, בקשות וערוצי שיחה שאינם פעילים.
        /// </summary>
        /// <param name="threshold">משך הזמן המרבי לסשן פעיל.</param>
        public void PurgeStale(TimeSpan threshold)
        {
            var cutoff = DateTimeOffset.UtcNow - threshold;

            foreach (var key in _sessions.Keys)
                if (_sessions.TryGetValue(key, out var s) && s.LastSeen < cutoff)
                    _sessions.TryRemove(key, out _);

            var requestCutoff = DateTimeOffset.UtcNow - TimeSpan.FromMinutes(5);
            foreach (var key in _pendingRequests.Keys)
                if (_pendingRequests.TryGetValue(key, out var r) && r.CreatedAt < requestCutoff)
                    _pendingRequests.TryRemove(key, out _);

            var channelCutoff = DateTimeOffset.UtcNow - TimeSpan.FromMinutes(35);
            foreach (var key in _activeChannels.Keys)
                if (_activeChannels.TryGetValue(key, out var c) && c.CreatedAt < channelCutoff)
                    _activeChannels.TryRemove(key, out _);
        }

        /// <summary>
        /// מוסיף בקשת אינטראקציה חדשה בין שני משתמשים.
        /// </summary>
        /// <param name="requesterUserId">מזהה המשתמש השולח.</param>
        /// <param name="targetUserId">מזהה המשתמש המקבל.</param>
        /// <param name="requesterConnectionId">מזהה החיבור של המשתמש השולח.</param>
        public void AddPending(string requesterUserId, string targetUserId, string requesterConnectionId)
        {
            var key = $"{requesterUserId}:{targetUserId}";
            _pendingRequests[key] = new PendingRequest(requesterConnectionId, DateTimeOffset.UtcNow);
        }

        /// <summary>
        /// מחפש בקשת אינטראקציה ממתינה ומסיר אותה אם נמצאה.
        /// </summary>
        /// <param name="requesterUserId">מזהה המשתמש השולח.</param>
        /// <param name="targetUserId">מזהה המשתמש המקבל.</param>
        /// <param name="requesterConnectionId">מזהה החיבור של המשתמש השולח.</param>
        /// <returns>True אם נמצאה בקשה ממתינה, אחרת False.</returns>
        public bool TryConsumePending(string requesterUserId, string targetUserId,
            out string requesterConnectionId)
        {
            var key = $"{requesterUserId}:{targetUserId}";
            if (_pendingRequests.TryRemove(key, out var r))
            {
                requesterConnectionId = r.RequesterConnectionId;
                return true;
            }

            requesterConnectionId = string.Empty;
            return false;
        }

        /// <summary>
        /// רושם ערוץ שיחה חדש בין שני משתמשים.
        /// </summary>
        /// <param name="channelId">מזהה הערוץ.</param>
        /// <param name="userIdA">מזהה המשתמש הראשון.</param>
        /// <param name="userIdB">מזהה המשתמש השני.</param>
        public void RegisterChannel(string channelId, string userIdA, string userIdB) =>
            _activeChannels[channelId] = new ActiveChannel(userIdA, userIdB, DateTimeOffset.UtcNow);

        /// <summary>
        /// מסיר ערוץ שיחה פעיל.
        /// </summary>
        /// <param name="channelId">מזהה הערוץ.</param>
        public void RemoveChannel(string channelId) =>
            _activeChannels.TryRemove(channelId, out _);

        /// <summary>
        /// בודק האם משתמש שייך לערוץ שיחה מסוים.
        /// </summary>
        /// <param name="channelId">מזהה הערוץ.</param>
        /// <param name="userId">מזהה המשתמש.</param>
        /// <returns>True אם המשתמש חבר בערוץ, אחרת False.</returns>
        public bool IsChannelMember(string channelId, string userId) =>
            _activeChannels.TryGetValue(channelId, out var ch) &&
            (ch.UserIdA == userId || ch.UserIdB == userId);

        /// <summary>
        /// בודק האם המשתמש חרג ממגבלת קצב שליחת בקשות.
        /// </summary>
        /// <param name="userId">מזהה המשתמש.</param>
        /// <param name="maxPerMinute">מספר הבקשות המרבי המותר בדקה.</param>
        /// <returns>True אם המשתמש עדיין במסגרת המגבלה, אחרת False.</returns>
        public bool TryConsumeRateLimit(string userId, int maxPerMinute = 5)
        {
            var now = DateTimeOffset.UtcNow;

            var entry = _rateLimits.AddOrUpdate(
                userId,
                _ => (1, now),
                (_, existing) =>
                {
                    if ((now - existing.WindowStart).TotalSeconds >= 60)
                        return (1, now);

                    return (existing.Count + 1, existing.WindowStart);
                });

            return entry.Count <= maxPerMinute;
        }

        /// <summary>
        /// בודק האם משתמש חסום על ידי משתמש אחר.
        /// </summary>
        /// <param name="requesterId">מזהה המשתמש המבקש.</param>
        /// <param name="targetId">מזהה המשתמש הנבדק.</param>
        /// <returns>True אם המשתמש חסום, אחרת False.</returns>
        public bool IsBlocked(string requesterId, string targetId) =>
            _blockList.TryGetValue(targetId, out var blocked) &&
            blocked.ContainsKey(requesterId);

        /// <summary>
        /// מוסיף משתמש לרשימת החסומים של משתמש אחר.
        /// </summary>
        /// <param name="userId">מזהה המשתמש החוסם.</param>
        /// <param name="targetId">מזהה המשתמש שנחסם.</param>
        public void Block(string userId, string targetId)
        {
            var set = _blockList.GetOrAdd(userId, _ => new ConcurrentDictionary<string, byte>());
            set.TryAdd(targetId, 0);
        }
    }
}