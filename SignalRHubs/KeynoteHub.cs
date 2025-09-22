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
    public class KeynoteHub(IMapper mapper) : Hub
    {
        //                 ScreenId  TempId
        ConcurrentDictionary<string, string> ScreenWaitingForCode =
            new ConcurrentDictionary<string, string>();

        public override async Task OnConnectedAsync()
        {
            await ReconnectExistingSessions();
            await CleanupOldEntities();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await MarkEntityAsDisconnected();
        }

        private async Task ReconnectExistingSessions()
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext == null) return;

            // Check for presentor (nauth user)
            var nauthUser = httpContext.GetNauthUser();
            var nauthSession = httpContext.GetNauthSession();
            if (nauthUser != null && nauthSession != null)
            {
                var presentor = PresentorService.GetOrCreate(nauthSession.Id!);
                presentor.nauthUser = nauthUser;
                presentor.IsConnected = true;
                presentor.ConnectionId = Context.ConnectionId;
                presentor.DisconnectedAt = null;
                PresentorService.AddOrUpdate(presentor);

                // Rejoin room if exists
                if (!string.IsNullOrEmpty(presentor.RoomCode))
                {
                    var room = RoomService.GetByRoomCode(presentor.RoomCode);
                    if (room != null)
                    {
                        await Groups.AddToGroupAsync(Context.ConnectionId, room.Identifier);
                    }
                }
                return;
            }

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
                    }
                }
                catch { /* Screen doesn't exist, ignore */ }
                return;
            }

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
                    }
                }
                catch { /* Spectator doesn't exist, ignore */ }
            }
        }

        private async Task MarkEntityAsDisconnected()
        {
            var connectionId = Context.ConnectionId;
            var disconnectedAt = DateTime.UtcNow;

            // Check all entity types for this connection
            var presentor = PresentorService.QuerySingle(q => q.Where(p => p.ConnectionId == connectionId));
            if (presentor != null)
            {
                presentor.IsConnected = false;
                presentor.DisconnectedAt = disconnectedAt;
                PresentorService.AddOrUpdate(presentor);
                return;
            }

            var screen = ScreenService.QuerySingle(q => q.Where(s => s.ConnectionId == connectionId));
            if (screen != null)
            {
                screen.IsConnected = false;
                screen.DisconnectedAt = disconnectedAt;
                ScreenService.AddOrUpdate(screen);
                return;
            }

            var spectator = SpectatorService.QuerySingle(q => q.Where(s => s.ConnectionId == connectionId));
            if (spectator != null)
            {
                spectator.IsConnected = false;
                spectator.DisconnectedAt = disconnectedAt;
                SpectatorService.AddOrUpdate(spectator);
            }
        }

        private async Task CleanupOldEntities()
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-24);
            var roomsToCheck = new HashSet<string>();

            // Cleanup old presentors
            var oldPresentors = PresentorService.QueryMany(q => q.Where(p => 
                !p.IsConnected && p.DisconnectedAt.HasValue && p.DisconnectedAt.Value < cutoffTime));
            foreach (var presentor in oldPresentors)
            {
                if (!string.IsNullOrEmpty(presentor.RoomCode))
                    roomsToCheck.Add(presentor.RoomCode);
                PresentorService.Remove(presentor.Identifier);
            }

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

            // Check rooms for deletion (but not if they were just created)
            var roomCreationCutoff = DateTime.UtcNow.AddMinutes(-5); // 5 minute grace period
            foreach (var roomCode in roomsToCheck)
            {
                await CheckAndDeleteEmptyRoom(roomCode, roomCreationCutoff);
            }
        }

        private async Task CheckAndDeleteEmptyRoom(string roomCode, DateTime creationCutoff)
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

        public async void SendRoomCodeToScreen(string roomCode, string tempId)
        {
            Clients.Group(tempId)?.SendAsync("RoomCode", tempId);
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

        public async Task<TR_RoomDTO?> JoinRoomAsSpectator(string roomCode)
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

        public async void SendRefresh(string roomIdentifier)
        {
            Clients.Group(roomIdentifier)?.SendAsync("Refresh");
        }

        // Helper method to validate nauth user and get presentor
        private async Task<TR_Presentor?> ValidateNauthUserAndGetPresentor()
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext == null) return null;

            var nauthUser = httpContext.GetNauthUser();
            var nauthSession = httpContext.GetNauthSession();

            if (nauthUser == null || nauthSession == null) return null;

            // Get or create presentor with nauth session ID as identifier
            var presentor = PresentorService.GetOrCreate(nauthSession.Id!);
            presentor.nauthUser = nauthUser;
            PresentorService.AddOrUpdate(presentor);

            return presentor;
        }

        // Presentor methods - all require nauth authentication
        public async Task<TR_RoomDTO?> Presentor_CreateRoom()
        {
            var presentor = await ValidateNauthUserAndGetPresentor();
            if (presentor == null) return null;

            // Check if presentor already has a room
            var existingRoom = RoomService.GetByRoomCode(presentor.RoomCode);
            if (existingRoom != null)
            {
                // Return existing room
                await Groups.AddToGroupAsync(Context.ConnectionId, existingRoom.Identifier);
                return mapper.Map<TR_RoomDTO>(existingRoom);
            }

            // Create new room
            var room = new TR_Room();
            RoomService.AddOrUpdate(room);

            PresentorService.joinRoom(presentor.Identifier, room.RoomCode);

            await Groups.AddToGroupAsync(Context.ConnectionId, room.Identifier);
            SendRefresh(room.Identifier);

            return mapper.Map<TR_RoomDTO>(room);
        }

        public async Task<TR_RoomDTO?> Presentor_SetKeynote(string keynoteId)
        {
            var presentor = await ValidateNauthUserAndGetPresentor();
            if (presentor == null) return null;

            var room = RoomService.GetByRoomCode(presentor.RoomCode);
            if (room == null) return null;

            // Inject KeynoteService - we'll need to modify the constructor
            // For now, we'll need to get it through DI
            var keynoteService = Context.GetHttpContext()?.RequestServices.GetService<KeynoteService>();
            if (keynoteService == null) return null;

            room.Keynote = await keynoteService.GetByIdAsync(long.Parse(keynoteId));
            RoomService.AddOrUpdate(room);
            SendRefresh(room.Identifier);

            return mapper.Map<TR_RoomDTO>(room);
        }

        public async Task<TR_RoomDTO?> Presentor_SetPage(int page)
        {
            var presentor = await ValidateNauthUserAndGetPresentor();
            if (presentor == null) return null;

            var room = RoomService.GetByRoomCode(presentor.RoomCode);
            if (room == null || room.Keynote == null) return null;

            room.currentFrame =
                page > (room.Keynote?.TotalFrames ?? 0) ? (room.Keynote?.TotalFrames ?? 0)
                : page < 0 ? 0
                : page;
            RoomService.AddOrUpdate(room);
            SendRefresh(room.Identifier);

            return mapper.Map<TR_RoomDTO>(room);
        }

        public async Task<TR_RoomDTO?> Presentor_SetShowSpectatorQR(bool show)
        {
            var presentor = await ValidateNauthUserAndGetPresentor();
            if (presentor == null) return null;

            var room = RoomService.GetByRoomCode(presentor.RoomCode);
            if (room == null || room.Keynote == null) return null;

            room.ShowSpectatorQR = show;
            RoomService.AddOrUpdate(room);
            SendRefresh(room.Identifier);

            return mapper.Map<TR_RoomDTO>(room);
        }

        public async Task<TR_RoomDTO?> Presentor_GiveTempControl(string spectatorId)
        {
            var presentor = await ValidateNauthUserAndGetPresentor();
            if (presentor == null) return null;

            var room = RoomService.GetByRoomCode(presentor.RoomCode);
            if (room == null) return null;

            room.TempControlSpectatorId = spectatorId;
            RoomService.AddOrUpdate(room);
            SendRefresh(room.Identifier);

            return mapper.Map<TR_RoomDTO>(room);
        }

        public async Task<TR_RoomDTO?> Presentor_TakeTempControl()
        {
            var presentor = await ValidateNauthUserAndGetPresentor();
            if (presentor == null) return null;

            var room = RoomService.GetByRoomCode(presentor.RoomCode);
            if (room == null) return null;

            room.TempControlSpectatorId = null;
            RoomService.AddOrUpdate(room);
            SendRefresh(room.Identifier);

            return mapper.Map<TR_RoomDTO>(room);
        }

        // Spectator method - requires temporary control
        public async Task<TR_RoomDTO?> Spectator_SetPage(int page)
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

        // Screen method - can always control frame
        public async Task<TR_RoomDTO?> Screen_SetPage(int page)
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
