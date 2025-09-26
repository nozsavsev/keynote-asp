using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using AutoMapper;
using keynote_asp.Helpers;
using keynote_asp.Models.Transient;
using keynote_asp.Services;
using keynote_asp.Services.Transient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Keynote_asp.Nauth.API_GEN.Models;

namespace keynote_asp.SignalRHubs
{
    [AllowAnonymous]
    public class ScreenHub(IMapper mapper) : BaseHub(mapper)
    {
        //                 ScreenId  TempId
        ConcurrentDictionary<string, string> ScreenWaitingForCode =
            new ConcurrentDictionary<string, string>();

        #region private
        protected override async Task<bool> ReconnectExistingSession()
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext == null) return false;

            // Check for screen session
            if (httpContext.Request.Cookies.TryGetValue("ScreenIdentifier", out string? screenIdentifier)
                && screenIdentifier != null)
            {
                try
                {
                    var screen = ScreenService.GetById(screenIdentifier);
                    if (screen != null)
                    {
                        screen.IsConnected = true;
                        screen.ConnectionId = Context.ConnectionId;
                        screen.DisconnectedAt = null;
                        ScreenService.AddOrUpdate(screen);

                        // Rejoin room if exists
                        if (!string.IsNullOrEmpty(screen.RoomCode))
                        {
                            var room = RoomService.GetByRoomCode(screen.RoomCode);
                            if (room != null)
                            {
                                await Groups.AddToGroupAsync(Context.ConnectionId, room.Identifier);
                            }
                        }

                        return true; // Screen session exists and reconnected successfully
                    }
                }
                catch { /* Screen doesn't exist, ignore */ }
            }

            // Check for spectator session (screens may also handle spectator functionality)
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

                        return true; // Spectator session exists and reconnected successfully
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

            // Check for screen with this connection
            var screen = ScreenService.QuerySingle(q => q.Where(s => s.ConnectionId == connectionId));
            if (screen != null)
            {
                screen.IsConnected = false;
                screen.DisconnectedAt = disconnectedAt;
                ScreenService.AddOrUpdate(screen);
                return;
            }

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
            // Cleanup old screens
            var oldScreens = ScreenService.QueryMany(q => q.Where(s =>
                !s.IsConnected && s.DisconnectedAt.HasValue && s.DisconnectedAt.Value < cutoffTime));
            foreach (var screen in oldScreens)
            {
                if (!string.IsNullOrEmpty(screen.RoomCode))
                    roomsToCheck.Add(screen.RoomCode);
                ScreenService.Remove(screen.Identifier);
            }

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
                    .Request.Cookies.TryGetValue("ScreenIdentifier", out string? ScreenIdentifier)
                && ScreenIdentifier != null
            )
            {
                var screen = ScreenService.GetById(ScreenIdentifier);
                var room = RoomService.GetByRoomCode(screen?.RoomCode);
                return mapper.Map<TR_RoomDTO>(room);
            }

            return null;
        }

        public async Task<TR_ScreenDTO?> Me()
        {
            if (
                Context
                    .GetHttpContext()!
                    .Request.Cookies.TryGetValue("ScreenIdentifier", out string? ScreenIdentifier)
                && ScreenIdentifier != null
            )
            {
                var screen = ScreenService.GetById(ScreenIdentifier);
                return mapper.Map<TR_ScreenDTO>(screen);
            }

            return null;
        }

        public async Task<string> WaitRoomAsScreen()
        {
            if (
                Context
                    .GetHttpContext()!
                    .Request.Cookies.TryGetValue("ScreenIdentifier", out string? ScreenIdentifier)
                && ScreenIdentifier != null
            )
            {
                var tempId = SnowflakeGlobal.Generate().ToString();
                ScreenWaitingForCode[ScreenIdentifier] = tempId;
                await Groups.AddToGroupAsync(Context.ConnectionId, tempId);
                return tempId;
            }

            return "";
        }

        public async Task<TR_RoomDTO?> JoinRoomAsScreen(string roomCode)
        {
            if (
                Context
                    .GetHttpContext()!
                    .Request.Cookies.TryGetValue("ScreenIdentifier", out string? ScreenIdentifier)
                && ScreenIdentifier != null
            )
            {
                var screen = ScreenService.GetById(ScreenIdentifier);

                if (screen != null)
                {
                    var roomIdentifier = RoomService
                        .QuerySingle(q => q.Where(r => r.RoomCode == roomCode))
                        ?.Identifier!;

                    if (roomIdentifier != null)
                    {
                        await Groups.RemoveFromGroupAsync(
                            Context.ConnectionId,
                            ScreenWaitingForCode[ScreenIdentifier]
                        );
                        await Groups.AddToGroupAsync(Context.ConnectionId, roomIdentifier);
                        return mapper.Map<TR_RoomDTO>(RoomService.GetById(roomIdentifier));
                    }
                }
            }

            return null;
        }

        public async Task<TR_RoomDTO?> SetPage(int page)
        {
            if (!Context.GetHttpContext()!.Request.Cookies.TryGetValue("ScreenIdentifier", out string? screenIdentifier)
                || screenIdentifier == null)
                return null;

            var screen = ScreenService.GetById(screenIdentifier);
            if (screen == null) return null;

            var room = RoomService.GetByRoomCode(screen.RoomCode);
            if (room == null || room.Keynote == null) return null;

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
