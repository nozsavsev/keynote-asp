using AutoMapper;
using keynote_asp.Helpers;
using keynote_asp.Models.Transient;
using keynote_asp.Services;
using keynote_asp.Services.Transient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace keynote_asp.SignalRHubs
{
    [AllowAnonymous]
    public abstract class BaseHub(IMapper mapper, SignalRRefreshService refreshService) : Hub
    {
        protected readonly IMapper mapper = mapper;
        protected readonly SignalRRefreshService refreshService = refreshService;

        public override async Task OnConnectedAsync()
        {
            var hasValidSession = await ReconnectExistingSession();
            if (!hasValidSession)
            {
                // Reject connection and notify client to acquire session first
                await Clients.Caller.SendAsync("SessionRequired");
                Context.Abort();
                return;
            }

            await CleanupOldEntities();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await MarkEntityAsDisconnected();
        }

        /// <summary>
        /// Sends refresh signal to all connections in a room group across all hubs
        /// </summary>
        public async Task SendRefresh(string roomIdentifier)
        {
            await refreshService.SendRefreshToAllHubs(roomIdentifier);
        }

        /// <summary>
        /// Checks if a room is empty and deletes it if no active entities remain
        /// </summary>
        protected async Task CheckAndDeleteEmptyRoom(string roomCode, DateTime creationCutoff)
        {
            var room = RoomService.GetByRoomCode(roomCode);
            if (room == null) return;

            // Don't delete recently created rooms (race condition prevention)
            // Assuming room creation time can be inferred from identifier (Snowflake timestamp)
            try
            {
                var roomId = long.Parse(room.Identifier);
                var roomCreationTime = SnowflakeGlobal.GetTimestamp(roomId);
                if (roomCreationTime > creationCutoff) return;
            }
            catch
            {
                // If we can't parse the timestamp, use a conservative approach
                return;
            }

            // Check if room has any connected or recently disconnected entities
            var presentor = room.Presentor;
            var screen = room.Screen;
            var spectators = room.Spectators ?? new List<TR_Spectator>();

            bool hasActiveEntities = false;

            // Check presentor
            if (presentor != null && (presentor.IsConnected ||
                (presentor.DisconnectedAt.HasValue && presentor.DisconnectedAt.Value > DateTime.UtcNow.AddHours(-24))))
            {
                hasActiveEntities = true;
            }

            // Check screen
            if (screen != null && (screen.IsConnected ||
                (screen.DisconnectedAt.HasValue && screen.DisconnectedAt.Value > DateTime.UtcNow.AddHours(-24))))
            {
                hasActiveEntities = true;
            }

            // Check spectators
            if (spectators.Any(s => s.IsConnected ||
                (s.DisconnectedAt.HasValue && s.DisconnectedAt.Value > DateTime.UtcNow.AddHours(-24))))
            {
                hasActiveEntities = true;
            }

            // Delete room if no active entities
            if (!hasActiveEntities)
            {
                RoomService.Remove(room.Identifier);
            }
        }

        /// <summary>
        /// Abstract method for each hub to implement their specific reconnection logic
        /// Returns true if session exists and connection should proceed, false if connection should be rejected
        /// </summary>
        protected abstract Task<bool> ReconnectExistingSession();

        /// <summary>
        /// Abstract method for each hub to implement their specific disconnection logic
        /// </summary>
        protected abstract Task MarkEntityAsDisconnected();

        /// <summary>
        /// Template method for cleaning up old entities - calls virtual methods that can be overridden
        /// </summary>
        protected virtual async Task CleanupOldEntities()
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-24);
            var roomsToCheck = new HashSet<string>();

            // Let each hub add its specific entity cleanup
            await CleanupSpecificEntities(cutoffTime, roomsToCheck);

            // Check rooms for deletion (but not if they were just created)
            var roomCreationCutoff = DateTime.UtcNow.AddMinutes(-5); // 5 minute grace period
            foreach (var roomCode in roomsToCheck)
            {
                await CheckAndDeleteEmptyRoom(roomCode, roomCreationCutoff);
            }
        }

        /// <summary>
        /// Virtual method for each hub to implement their specific entity cleanup
        /// </summary>
        protected virtual async Task CleanupSpecificEntities(DateTime cutoffTime, HashSet<string> roomsToCheck)
        {
            // Default implementation does nothing - each hub overrides with specific cleanup
            await Task.CompletedTask;
        }
    }
}
