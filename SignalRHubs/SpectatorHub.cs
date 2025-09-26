using AutoMapper;
using keynote_asp.Helpers;
using keynote_asp.Models.Transient;
using keynote_asp.Services.Transient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace keynote_asp.SignalRHubs
{
    [AllowAnonymous]
    public class SpectatorHub(IMapper mapper) : BaseHub(mapper)
    {
        #region private
        protected override async Task<bool> ReconnectExistingSession()
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext == null) return false;

            // Check for spectator session
            if (httpContext.Request.Cookies.TryGetValue("SpectatorIdentifier", out string? spectatorIdentifier)
                && spectatorIdentifier != null)
            {
                try
                {
                    var spectator = SpectatorService.GetById(spectatorIdentifier);
                    if (spectator != null)
                    {
                        spectator.IsConnected = true;
                        spectator.ConnectionId = Context.ConnectionId;
                        spectator.DisconnectedAt = null;
                        SpectatorService.AddOrUpdate(spectator);

                        // Rejoin room if exists
                        if (!string.IsNullOrEmpty(spectator.RoomCode))
                        {
                            var room = RoomService.GetByRoomCode(spectator.RoomCode);
                            if (room != null)
                            {
                                await Groups.AddToGroupAsync(Context.ConnectionId, room.Identifier);
                            }
                        }

                        return true; // Session exists and reconnected successfully
                    }
                }
                catch { /* Spectator doesn't exist, ignore */ }
            }

            return false; // No valid session found
        }

        protected override async Task MarkEntityAsDisconnected()
        {
            var connectionId = Context.ConnectionId;
            var disconnectedAt = DateTime.UtcNow;

            // Check for spectator with this connection
            var spectator = SpectatorService.QuerySingle(q => q.Where(s => s.ConnectionId == connectionId));
            if (spectator != null)
            {
                spectator.IsConnected = false;
                spectator.DisconnectedAt = disconnectedAt;
                SpectatorService.AddOrUpdate(spectator);
            }
        }

        protected override async Task CleanupSpecificEntities(DateTime cutoffTime, HashSet<string> roomsToCheck)
        {
            // Cleanup old spectators
            var oldSpectators = SpectatorService.QueryMany(q => q.Where(s =>
                !s.IsConnected && s.DisconnectedAt.HasValue && s.DisconnectedAt.Value < cutoffTime));
            foreach (var spectator in oldSpectators)
            {
                if (!string.IsNullOrEmpty(spectator.RoomCode))
                    roomsToCheck.Add(spectator.RoomCode);
                SpectatorService.Remove(spectator.Identifier);
            }
        }
        #endregion



        public async Task<TR_RoomDTO?> GetCurrentRoom()
        {
            if (
                Context
                    .GetHttpContext()!
                    .Request.Cookies.TryGetValue("SpectatorIdentifier", out string? SpectatorIdentifier)
                && SpectatorIdentifier != null
            )
            {
                var spectator = SpectatorService.GetById(SpectatorIdentifier);
                var room = RoomService.GetByRoomCode(spectator?.RoomCode);
                return mapper.Map<TR_RoomDTO>(room);
            }

            return null;
        }

        public async Task<TR_SpectatorDTO?> Me()
        {
            if (
                Context
                    .GetHttpContext()!
                    .Request.Cookies.TryGetValue("SpectatorIdentifier", out string? SpectatorIdentifier)
                && SpectatorIdentifier != null
            )
            {
                var spectator = SpectatorService.GetById(SpectatorIdentifier);
                return mapper.Map<TR_SpectatorDTO>(spectator);
            }

            return null;
        }

        public async Task<TR_SpectatorDTO?> SetName(string name)
        {
            if (
                Context
                    .GetHttpContext()!
                    .Request.Cookies.TryGetValue(
                        "SpectatorIdentifier",
                        out string? SpectatorIdentifier
                    )
                && SpectatorIdentifier != null
            )
            {
                var spectator = SpectatorService.GetById(SpectatorIdentifier);
                if (spectator != null)
                {

                    spectator.Name = name;

                    SpectatorService.AddOrUpdate(spectator);


                    var room = RoomService.GetByRoomCode(spectator.RoomCode);
                    if (room != null)
                    {
                        SendRefresh(room.Identifier);
                        return mapper.Map<TR_SpectatorDTO>(spectator);
                    }
                }
            }

            return null;
        }

        public async Task<TR_RoomDTO?> JoinRoom(string roomCode)
        {
            if (
                Context
                    .GetHttpContext()!
                    .Request.Cookies.TryGetValue(
                        "SpectatorIdentifier",
                        out string? SpectatorIdentifier
                    )
                && SpectatorIdentifier != null
            )
            {
                var spectator = SpectatorService.GetById(SpectatorIdentifier);

                if (spectator != null)
                {
                    var roomIdentifier = RoomService
                        .QuerySingle(q => q.Where(r => r.RoomCode == roomCode))
                        ?.Identifier!;
                    if (roomIdentifier != null)
                    {
                        await Groups.AddToGroupAsync(Context.ConnectionId, roomIdentifier);
                        return mapper.Map<TR_RoomDTO>(RoomService.GetById(roomIdentifier));
                    }
                }
            }

            return null;
        }
        public async Task<TR_RoomDTO?> SetPage(int page)
        {
            if (!Context.GetHttpContext()!.Request.Cookies.TryGetValue("SpectatorIdentifier", out string? spectatorIdentifier)
                || spectatorIdentifier == null)
                return null;

            var spectator = SpectatorService.GetById(spectatorIdentifier);
            if (spectator == null) return null;

            var room = RoomService.GetByRoomCode(spectator.RoomCode);
            if (room == null || room.Keynote == null) return null;

            // Check if this spectator has temporary control
            if (room.TempControlSpectatorId != spectator.Identifier) return null;

            room.currentFrame =
                page > (room.Keynote?.TotalFrames ?? 0) ? (room.Keynote?.TotalFrames ?? 0)
                : page < 0 ? 0
                : page;
            RoomService.AddOrUpdate(room);
            SendRefresh(room.Identifier);

            return mapper.Map<TR_RoomDTO>(room);
        }
    }
}
