using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PartnersWebApi.Services;

namespace PartnersWebApi.Hubs
{
    [Authorize]
    public class LocationHub : Hub
    {
        private const double RadiusKm = 1.0;

        private readonly PresenceStore _store;
        private readonly ILogger<LocationHub> _log;
        private readonly IHubContext<LocationHub> _hubContext;

        public LocationHub(
            PresenceStore store,
            ILogger<LocationHub> log,
            IHubContext<LocationHub> hubContext)
        {
            _store = store;
            _log = log;
            _hubContext = hubContext;
        }

        // ── Lifecycle ────────────────────────────────────────────────────────

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier!;
            _store.AddOrUpdate(new UserSession
            {
                UserId = userId,
                ConnectionId = Context.ConnectionId,
            });
            _log.LogInformation("Connected: {UserId} ({ConnId})", userId, Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? ex)
        {
            var session = _store.Get(Context.ConnectionId);
            if (session is not null)
            {
                _store.Remove(Context.ConnectionId);
                await Clients.Others.SendAsync("UserLeft", session.UserId);
            }
            _log.LogInformation("Disconnected: {ConnId}, error={Error}",
                Context.ConnectionId, ex?.Message);
            await base.OnDisconnectedAsync(ex);
        }

        // ── Location & Presence ──────────────────────────────────────────────

        public async Task UpdateLocation(double lat, double lng, string status, bool sharing)
        {
            var session = _store.Get(Context.ConnectionId);
            if (session is null) return;

            session.Latitude = lat;
            session.Longitude = lng;
            session.Status = status;
            session.SharingLocation = sharing;
            session.LastSeen = DateTimeOffset.UtcNow;

            _store.AddOrUpdate(session);

            if (!sharing || (lat == 0 && lng == 0))
            {
                session.SharingLocation = false;
                _store.AddOrUpdate(session);
                await Clients.Others.SendAsync("UserLeft", session.UserId);
                return;
            }

            var nearby = GetNearbyUsers(session, RadiusKm).ToList();

            var payload = new
            {
                session.UserId,
                session.Latitude,
                session.Longitude,
                session.Status,
            };

            var connectionIds = nearby
                .Where(u => !_store.IsBlocked(session.UserId, u.UserId))
                .Select(u => u.ConnectionId)
                .ToList();

            if (connectionIds.Count > 0)
                await Clients.Clients(connectionIds).SendAsync("LocationUpdate", payload);

            var nearbySnapshot = nearby.Select(u => new
            {
                u.UserId,
                u.Latitude,
                u.Longitude,
                u.Status,
            });
            await Clients.Caller.SendAsync("NearbyUsers", nearbySnapshot);
        }

        public async Task SetSharingEnabled(bool enabled)
        {
            var session = _store.Get(Context.ConnectionId);
            if (session is null) return;
            session.SharingLocation = enabled;
            session.LastSeen = DateTimeOffset.UtcNow;
            _store.AddOrUpdate(session);
            if (!enabled)
                await Clients.Others.SendAsync("UserLeft", session.UserId);
        }

        // ── Interaction Request Flow ─────────────────────────────────────────

        public async Task SendInteractionRequest(string targetUserId)
        {
            var requester = _store.Get(Context.ConnectionId);
            if (requester is null) return;

            if (!_store.TryConsumeRateLimit(requester.UserId))
            {
                await Clients.Caller.SendAsync("Error", "שלחת יותר מדי בקשות. נסה שוב בעוד דקה.");
                return;
            }

            if (_store.IsBlocked(requester.UserId, targetUserId))
            {
                await Clients.Caller.SendAsync("Error", "אינך יכול ליצור קשר עם משתמש זה.");
                return;
            }

            var target = _store.GetAllSharing()
                .FirstOrDefault(u => u.UserId == targetUserId);

            if (target is null)
            {
                await Clients.Caller.SendAsync("Error", "המשתמש אינו זמין כרגע.");
                return;
            }
            _store.AddPending(requester.UserId, targetUserId, requester.ConnectionId);

            await Clients.Client(target.ConnectionId).SendAsync("InteractionRequest", new
            {
                FromUserId = requester.UserId,
                requester.Status,
            });
        }

        public async Task RespondToRequest(string requesterUserId, bool accepted)
        {
            var responder = _store.Get(Context.ConnectionId);
            if (responder is null) return;


            if (!_store.TryConsumePending(requesterUserId, responder.UserId,
                    out var requesterConnId))
            {
                await Clients.Caller.SendAsync("Error", "הבקשה פגה תוקף.");
                return;
            }

            if (!accepted)
            {
                await Clients.Client(requesterConnId).SendAsync("RequestDeclined", responder.UserId);
                return;
            }

            var channelId = $"chat:{Guid.NewGuid():N}";
            await Groups.AddToGroupAsync(requesterConnId, channelId);
            await Groups.AddToGroupAsync(Context.ConnectionId, channelId);

 
            var requesterSession = _store.GetByUserId(requesterUserId);
            _store.RegisterChannel(
                channelId,
                requesterUserId,
                responder.UserId);

            await Clients.Group(channelId).SendAsync("ChannelReady", new { channelId });


            var capturedChannelId = channelId;
            var capturedRequesterConn = requesterConnId;
            var capturedResponderConn = Context.ConnectionId;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(30));

                    await _hubContext.Groups.RemoveFromGroupAsync(capturedRequesterConn, capturedChannelId);
                    await _hubContext.Groups.RemoveFromGroupAsync(capturedResponderConn, capturedChannelId);
                    await _hubContext.Clients.Group(capturedChannelId)
                        .SendAsync("ChannelExpired", capturedChannelId);

                    _store.RemoveChannel(capturedChannelId);
                }
                catch (Exception ex)
                {
                    _log.LogWarning("ExpireChannel error for {ChannelId}: {Msg}",
                        capturedChannelId, ex.Message);
                }
            });
        }

        // ── Chat ─────────────────────────────────────────────────────────────

        public async Task SendChatMessage(string channelId, string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length > 500) return;

            var session = _store.Get(Context.ConnectionId);
            if (session is null) return;

            // validate that the sender is actually a member of this channel.
            // Without this check, any connected user who guesses the channelId GUID
            // can inject messages into someone else's private conversation.
            if (!_store.IsChannelMember(channelId, session.UserId))
            {
                _log.LogWarning("Unauthorized SendChatMessage: {UserId} → {ChannelId}",
                    session.UserId, channelId);
                return;
            }

            await Clients.Group(channelId).SendAsync("ChatMessage", new
            {
                session.UserId,
                Text = text,
                Timestamp = DateTimeOffset.UtcNow,
            });
        }

        public async Task LeaveChannel(string channelId)
        {
            var session = _store.Get(Context.ConnectionId);
            if (session is null) return;

            // Only members can leave their own channel
            if (!_store.IsChannelMember(channelId, session.UserId)) return;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, channelId);
            await Clients.Group(channelId).SendAsync("UserLeftChannel", Context.UserIdentifier);
            _store.RemoveChannel(channelId);
        }

        // ── Block ────────────────────────────────────────────────────────────

        public async Task BlockUser(string targetUserId)
        {
            var blocker = _store.Get(Context.ConnectionId);
            if (blocker is null) return;

            _store.Block(blocker.UserId, targetUserId);

            // Immediately remove the blocked user from the caller's map.
            // Without this, the dot stays visible until the next GPS update.
            await Clients.Caller.SendAsync("UserLeft", targetUserId);
            await Clients.Caller.SendAsync("UserBlocked", targetUserId);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private IEnumerable<UserSession> GetNearbyUsers(UserSession origin, double radiusKm) =>
            _store.GetAllSharing()
                  .Where(u => u.ConnectionId != origin.ConnectionId &&
                              Haversine(origin.Latitude, origin.Longitude,
                                        u.Latitude, u.Longitude) <= radiusKm);

        private static double Haversine(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371;
            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLon = (lon2 - lon1) * Math.PI / 180;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }
    }
}