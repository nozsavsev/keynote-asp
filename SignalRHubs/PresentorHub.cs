using AutoMapper;
using keynote_asp.Helpers;
using keynote_asp.Models.Transient;
using keynote_asp.Services;
using keynote_asp.Services.Transient;
using Keynote_asp.Nauth.API_GEN.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace keynote_asp.SignalRHubs
{
    [AllowAnonymous]
    public class PresentorHub(IMapper mapper, SignalRRefreshService refreshService) : BaseHub(mapper, refreshService)
    {

        #region private
        protected override async Task<bool> ReconnectExistingSession()
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext == null) return false;

            // Check for presentor (nauth user)
            var nauthUser = httpContext.GetNauthUser();
            var nauthSession = httpContext.GetNauthSession();
            if (nauthUser != null && nauthSession != null)
            {
                var presentor = PresentorService.GetOrCreate(nauthSession.Id!);
                presentor.nauthUser = nauthUser;
                presentor.Name = nauthUser.Name ?? nauthUser.Email!;
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
                        Console.WriteLine($"[PresentorHub] Reconnecting presentor to room group: {room.Identifier}");
                        await Groups.AddToGroupAsync(Context.ConnectionId, room.Identifier);

                        // Send refresh to notify others that presentor reconnected
                        await SendRefresh(room.Identifier);
                    }
                    else
                    {
                        Console.WriteLine($"[PresentorHub] Room not found for RoomCode: {presentor.RoomCode}");
                        // Clear invalid room code
                        presentor.RoomCode = string.Empty;
                        PresentorService.AddOrUpdate(presentor);
                    }
                }

                return true; // Presentor session exists and reconnected successfully
            }

            return false; // No valid nauth session found
        }

        protected override async Task MarkEntityAsDisconnected()
        {
            var connectionId = Context.ConnectionId;
            var disconnectedAt = DateTime.UtcNow;

            // Check for presentor with this connection
            var presentor = PresentorService.QuerySingle(q => q.Where(p => p.ConnectionId == connectionId));
            if (presentor != null)
            {
                presentor.IsConnected = false;
                presentor.DisconnectedAt = disconnectedAt;
                PresentorService.AddOrUpdate(presentor);
                return;
            }
        }

        protected override async Task CleanupSpecificEntities(DateTime cutoffTime, HashSet<string> roomsToCheck)
        {
            // Cleanup old presentors
            var oldPresentors = PresentorService.QueryMany(q => q.Where(p =>
                !p.IsConnected && p.DisconnectedAt.HasValue && p.DisconnectedAt.Value < cutoffTime));
            foreach (var presentor in oldPresentors)
            {
                if (!string.IsNullOrEmpty(presentor.RoomCode))
                    roomsToCheck.Add(presentor.RoomCode);
                PresentorService.Remove(presentor.Identifier);
            }
        }

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
            presentor.ConnectionId = Context.ConnectionId;
            PresentorService.AddOrUpdate(presentor);

            return presentor;
        }
        #endregion


        public async Task<TR_RoomDTO?> GetCurrentRoom()
        {
            var presentor = await ValidateNauthUserAndGetPresentor();
            if (presentor == null) return null;

            var room = RoomService.GetByRoomCode(presentor.RoomCode);
            if (room == null) return null;

            return mapper.Map<TR_RoomDTO>(room);
        }

        public async Task<TR_PresentorDTO?> Me()
        {
            var presentor = await ValidateNauthUserAndGetPresentor();
            if (presentor == null) return null;

            return mapper.Map<TR_PresentorDTO>(presentor);
        }

        public async Task<TR_RoomDTO?> RemoveSpectator(string identifier)
        {
            var presentor = await ValidateNauthUserAndGetPresentor();
            if (presentor == null) return null;

            var spectator = SpectatorService.GetById(identifier);

            var room = RoomService.GetByRoomCode(presentor.RoomCode);

            if (spectator != null && spectator.RoomCode == presentor.RoomCode && room != null)
            {
                await Groups.RemoveFromGroupAsync(spectator.ConnectionId, room.Identifier);
                spectator.RoomCode = string.Empty;
                SpectatorService.AddOrUpdate(spectator);
            }


            return mapper.Map<TR_RoomDTO>(mapper.Map<TR_RoomDTO>(room));
        }

        public async Task<TR_RoomDTO?> RemoveScreen(string identifier)
        {
            var presentor = await ValidateNauthUserAndGetPresentor();
            if (presentor == null) return null;

            var screen = ScreenService.GetById(identifier);

            var room = RoomService.GetByRoomCode(presentor.RoomCode);

            if (screen != null && screen.RoomCode == presentor.RoomCode && room != null)
            {
                screen.RoomCode = string.Empty;
                await SendRefresh(room.Identifier);
                await Groups.RemoveFromGroupAsync(screen.ConnectionId, room.Identifier);
                ScreenService.AddOrUpdate(screen);
            }


            return mapper.Map<TR_RoomDTO>(mapper.Map<TR_RoomDTO>(room));
        }

        public async Task<TR_PresentorDTO?> SetPresentorName(string name)
        {
            var presentor = await ValidateNauthUserAndGetPresentor();
            if (presentor == null) return null;

            presentor.Name = name;

            PresentorService.AddOrUpdate(presentor);

            var room = RoomService.GetByRoomCode(presentor.RoomCode);
            if (room != null)
                await SendRefresh(room.Identifier);

            return mapper.Map<TR_PresentorDTO>(presentor);
        }


        public async Task<TR_RoomDTO?> CreateRoom()
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

            presentor = PresentorService.GetById(presentor.Identifier);

            await Groups.AddToGroupAsync(Context.ConnectionId, room.Identifier);
            await SendRefresh(room.Identifier);

            return mapper.Map<TR_RoomDTO>(room);
        }

        public async Task<TR_RoomDTO?> SetKeynote(string keynoteId)
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
            await SendRefresh(room.Identifier);

            return mapper.Map<TR_RoomDTO>(room);
        }

        public async Task<TR_RoomDTO?> SetPage(int page)
        {
            var presentor = await ValidateNauthUserAndGetPresentor();
            if (presentor == null) return null;

            var room = RoomService.GetByRoomCode(presentor.RoomCode);
            if (room == null || room.Keynote == null) return null;

            room.currentFrame =
                page > (room.Keynote?.TotalFrames + 1 ?? 0) ? (room.Keynote?.TotalFrames + 1 ?? 0)
                : page < 0 ? 0
                : page;
            RoomService.AddOrUpdate(room);
            await SendRefresh(room.Identifier);

            return mapper.Map<TR_RoomDTO>(room);
        }

        public async Task<TR_RoomDTO?> SetShowSpectatorQR(bool show)
        {
            var presentor = await ValidateNauthUserAndGetPresentor();
            if (presentor == null) return null;

            var room = RoomService.GetByRoomCode(presentor.RoomCode);
            if (room == null || room.Keynote == null) return null;

            room.ShowSpectatorQR = show;
            RoomService.AddOrUpdate(room);
            await SendRefresh(room.Identifier);

            return mapper.Map<TR_RoomDTO>(room);
        }

        public async Task<TR_RoomDTO?> GiveTempControl(string spectatorId)
        {
            var presentor = await ValidateNauthUserAndGetPresentor();
            if (presentor == null) return null;

            var room = RoomService.GetByRoomCode(presentor.RoomCode);
            if (room == null) return null;

            room.TempControlSpectatorId = spectatorId;
            RoomService.AddOrUpdate(room);
            await SendRefresh(room.Identifier);

            return mapper.Map<TR_RoomDTO>(room);
        }

        public async Task<TR_RoomDTO?> TakeTempControl()
        {
            var presentor = await ValidateNauthUserAndGetPresentor();
            if (presentor == null) return null;

            var room = RoomService.GetByRoomCode(presentor.RoomCode);
            if (room == null) return null;

            room.TempControlSpectatorId = null;
            RoomService.AddOrUpdate(room);
            await SendRefresh(room.Identifier);

            return mapper.Map<TR_RoomDTO>(room);
        }

        public async Task<TR_RoomDTO?> SendRoomCodeToScreen(string roomCode, string ScreenIdentifier)
        {
            var presentor = await ValidateNauthUserAndGetPresentor();
            if (presentor == null) return null;

            var screen = ScreenService.GetById(ScreenIdentifier);

            if (screen != null)
            {
                var room = RoomService.GetByRoomCode(roomCode);

                if (room != null)
                {
                    room = RoomService.GetById(room.Identifier)!;
                    await refreshService.SendOnScreenHub(screen.Identifier, "RoomCode", room.RoomCode);

                    return mapper.Map<TR_RoomDTO>(room);
                }
            }

            return null;
        }
    }
}
