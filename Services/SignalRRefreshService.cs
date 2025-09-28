using AutoMapper;
using keynote_asp.Models.Transient;
using keynote_asp.Services.Transient;
using keynote_asp.SignalRHubs;
using Microsoft.AspNetCore.SignalR;

namespace keynote_asp.Services
{
    /// <summary>
    /// Centralized service for coordinating refresh messages across all SignalR hubs
    /// This solves the issue where SignalR groups are scoped to individual hubs
    /// </summary>
    public class SignalRRefreshService
    {
        private readonly IHubContext<PresentorHub> _presentorHubContext;
        private readonly IHubContext<ScreenHub> _screenHubContext;
        private readonly IHubContext<SpectatorHub> _spectatorHubContext;
        private readonly IMapper _mapper;

        public SignalRRefreshService(
            IHubContext<PresentorHub> presentorHubContext,
            IHubContext<ScreenHub> screenHubContext,
            IHubContext<SpectatorHub> spectatorHubContext,
            IMapper mapper)
        {
            _presentorHubContext = presentorHubContext;
            _screenHubContext = screenHubContext;
            _spectatorHubContext = spectatorHubContext;
            _mapper = mapper;
        }

        /// <summary>
        /// Sends refresh signal to all connections in a room group across all hubs
        /// </summary>
        /// <param name="roomIdentifier">The room identifier to send refresh to</param>
        public async Task SendRefreshToAllHubs(string roomIdentifier)
        {
            Console.WriteLine($"[SignalRRefreshService] SendRefreshToAllHubs called for room: {roomIdentifier}");

            var room = RoomService.GetById(roomIdentifier);
            if (room == null)
            {
                Console.WriteLine($"[SignalRRefreshService] Room not found for identifier: {roomIdentifier}");
                return;
            }

            Console.WriteLine($"[SignalRRefreshService] Room found: {room.RoomCode}, Screen: {room.Screen?.Identifier ?? "null"}, Presentor: {room.Presentor?.Identifier ?? "null"}");

            // Debug: Check what's in the services
            var allScreens = ScreenService.QueryMany(q => q.Where(s => s.RoomCode == room.RoomCode));
            var allPresentors = PresentorService.QueryMany(q => q.Where(p => p.RoomCode == room.RoomCode));
            var allSpectators = SpectatorService.QueryMany(q => q.Where(s => s.RoomCode == room.RoomCode));

            Console.WriteLine($"[SignalRRefreshService] Debug - All screens: {allScreens.Count}, All presentors: {allPresentors.Count}, All spectators: {allSpectators.Count}");
            // Manually create the DTO to ensure all properties are properly populated
            TR_RoomDTO roomDto;
            try
            {
                roomDto = new TR_RoomDTO
                {
                    Identifier = room.Identifier,
                    RoomCode = room.RoomCode,
                    Keynote = room.Keynote,
                    currentFrame = room.currentFrame,
                    ShowSpectatorQR = room.ShowSpectatorQR,
                    TempControlSpectatorId = room.TempControlSpectatorId,
                    Presentor = room.Presentor != null ? _mapper.Map<TR_PresentorDTO>(room.Presentor) : null,
                    Screen = room.Screen != null ? _mapper.Map<TR_ScreenDTO>(room.Screen) : null,
                    Spectators = room.Spectators != null ? _mapper.Map<List<TR_SpectatorDTO>>(room.Spectators) : null
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SignalRRefreshService] Error mapping room to DTO: {ex.Message}");
                Console.WriteLine($"[SignalRRefreshService] Stack trace: {ex.StackTrace}");
                throw;
            }

            Console.WriteLine($"[SignalRRefreshService] Final DTO - Screen: {roomDto.Screen?.Identifier ?? "null"}, Presentor: {roomDto.Presentor?.Identifier ?? "null"}, Spectators: {roomDto.Spectators?.Count ?? 0}");

            // Send refresh to all three hubs simultaneously
            var tasks = new[]
            {
                _presentorHubContext.Clients.Group(roomIdentifier).SendAsync("Refresh", roomDto),
                _screenHubContext.Clients.Group(roomIdentifier).SendAsync("Refresh", roomDto),
                _spectatorHubContext.Clients.Group(roomIdentifier).SendAsync("Refresh", roomDto)
            };

            Console.WriteLine($"[SignalRRefreshService] Sending refresh to all hubs for room: {roomIdentifier}");
            await Task.WhenAll(tasks);
            Console.WriteLine($"[SignalRRefreshService] Refresh sent to all hubs");
        }

        /// <summary>
        /// Sends refresh signal to all connections in a room group across all hubs
        /// Uses room code instead of room identifier
        /// </summary>
        /// <param name="roomCode">The room code to send refresh to</param>
        public async Task SendRefreshToAllHubsByRoomCode(string roomCode)
        {
            var room = RoomService.GetByRoomCode(roomCode);
            if (room == null) return;

            await SendRefreshToAllHubs(room.Identifier);
        }

        public async Task AddToGroupOnScreenHub(string connectionId, string group)
        {
            Console.WriteLine($"[SignalRRefreshService] Adding connection {connectionId} to group {group} on ScreenHub");
            await _screenHubContext.Groups.AddToGroupAsync(connectionId, group);
            Console.WriteLine($"[SignalRRefreshService] Successfully added connection {connectionId} to group {group} on ScreenHub");
        }

        /// <summary>
        /// Sends a specific message to all connections in a room group across all hubs
        /// </summary>
        /// <param name="roomIdentifier">The room identifier to send message to</param>
        /// <param name="method">The SignalR method name</param>
        /// <param name="args">The arguments to send</param>
        public async Task SendToAllHubs(string roomIdentifier, string method, params object[] args)
        {
            // Send message to all three hubs simultaneously
            var tasks = new[]
            {
                _presentorHubContext.Clients.Group(roomIdentifier).SendAsync(method, args),
                _screenHubContext.Clients.Group(roomIdentifier).SendAsync(method, args),
                _spectatorHubContext.Clients.Group(roomIdentifier).SendAsync(method, args)
            };

            await Task.WhenAll(tasks);
        }

        public async Task SendOnScreenHub(string roomIdentifier, string method, params object[] args)
        {
            await _screenHubContext.Clients.Group(roomIdentifier).SendAsync(method, args);
        }

        /// <summary>
        /// Sends a specific message to all connections in a room group across all hubs
        /// Uses room code instead of room identifier
        /// </summary>
        /// <param name="roomCode">The room code to send message to</param>
        /// <param name="method">The SignalR method name</param>
        /// <param name="args">The arguments to send</param>
        public async Task SendToAllHubsByRoomCode(string roomCode, string method, params object[] args)
        {
            var room = RoomService.GetByRoomCode(roomCode);
            if (room == null) return;

            await SendToAllHubs(room.Identifier, method, args);
        }
    }
}
