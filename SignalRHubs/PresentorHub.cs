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
    public class PresentorHub(IMapper mapper) : BaseHub(mapper)
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
                        await Groups.AddToGroupAsync(Context.ConnectionId, room.Identifier);
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

        public async Task<TR_PresentorDTO?> SetPresentorName(string name)
        {
            var presentor = await ValidateNauthUserAndGetPresentor();
            if (presentor == null) return null;

            presentor.Name = name;

            PresentorService.AddOrUpdate(presentor);

            var room = RoomService.GetByRoomCode(presentor.RoomCode);
            if (room != null)
                SendRefresh(room.Identifier);

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

            await Groups.AddToGroupAsync(Context.ConnectionId, room.Identifier);
            SendRefresh(room.Identifier);

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
            SendRefresh(room.Identifier);

            return mapper.Map<TR_RoomDTO>(room);
        }

        public async Task<TR_RoomDTO?> SetPage(int page)
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

        public async Task<TR_RoomDTO?> SetShowSpectatorQR(bool show)
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

        public async Task<TR_RoomDTO?> GiveTempControl(string spectatorId)
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

        public async Task<TR_RoomDTO?> TakeTempControl()
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

        public async void SendRoomCodeToScreen(string roomCode, string tempId)
        {
            Clients.Group(tempId)?.SendAsync("RoomCode", tempId);
        }
    }
}
