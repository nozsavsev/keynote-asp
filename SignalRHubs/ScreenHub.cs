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
    public class ScreenHub(IMapper mapper, SignalRRefreshService refreshService) : BaseHub(mapper, refreshService)
    {

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
                        Console.WriteLine($"[ScreenHub] Reconnecting screen {screenIdentifier}, RoomCode: {screen.RoomCode}");

                        // Update screen connection state
                        screen.IsConnected = true;
                        screen.ConnectionId = Context.ConnectionId;
                        screen.DisconnectedAt = null;
                        ScreenService.AddOrUpdate(screen);
                        Console.WriteLine($"[ScreenHub] Updated screen connection state - ConnectionId: {Context.ConnectionId}, IsConnected: true");

                        // Add to screen's own group
                        await Groups.AddToGroupAsync(Context.ConnectionId, screen.Identifier);
                        Console.WriteLine($"[ScreenHub] Added screen to its own group: {screen.Identifier}");

                        // Rejoin room if exists
                        if (!string.IsNullOrEmpty(screen.RoomCode))
                        {
                            var room = RoomService.GetByRoomCode(screen.RoomCode);
                            if (room != null)
                            {
                                Console.WriteLine($"[ScreenHub] Adding screen to room group: {room.Identifier}");
                                await Groups.AddToGroupAsync(Context.ConnectionId, room.Identifier);
                                Console.WriteLine($"[ScreenHub] Successfully added screen to room group: {room.Identifier}");
                                
                                // Send refresh to notify others that screen reconnected
                                await SendRefresh(room.Identifier);
                            }
                            else
                            {
                                Console.WriteLine($"[ScreenHub] Room not found for RoomCode: {screen.RoomCode}");
                                // Clear invalid room code
                                screen.RoomCode = string.Empty;
                                ScreenService.AddOrUpdate(screen);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[ScreenHub] Screen has no RoomCode, not joining any room group");
                        }

                        return true; // Screen session exists and reconnected successfully
                    }
                }
                catch { /* Screen doesn't exist, ignore */ }
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
                var room = RoomService.GetByRoomCode(screen?.RoomCode ?? string.Empty);
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
                var final = mapper.Map<TR_ScreenDTO>(screen);
                Console.WriteLine($"ScreenRoomCode is: {final.RoomCode} ");
                return final;
            }

            return null;
        }

        public async Task LeaveRoom()
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
                    Console.WriteLine($"[ScreenHub] Screen {ScreenIdentifier} leaving room {screen.RoomCode}");
                    
                    var roomIdentifier = RoomService
                        .QuerySingle(q => q.Where(r => r.RoomCode == screen.RoomCode))
                        ?.Identifier!;

                    if (roomIdentifier != null)
                    {
                        // Remove from room group
                        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomIdentifier);
                        
                        // Add back to screen's own group
                        await Groups.AddToGroupAsync(Context.ConnectionId, ScreenIdentifier);
                        
                        // Update screen state
                        screen.RoomCode = string.Empty;
                        ScreenService.AddOrUpdate(screen);
                        
                        // Send refresh to notify others that screen left
                        Console.WriteLine($"[ScreenHub] Sending refresh after screen leave");
                        await SendRefresh(roomIdentifier);
                        return;
                    }
                    else
                    {
                        Console.WriteLine($"[ScreenHub] Room not found for RoomCode: {screen.RoomCode}");
                        // Clear invalid room code
                        screen.RoomCode = string.Empty;
                        ScreenService.AddOrUpdate(screen);
                    }
                }
            }

            return;
        }

        public async Task<TR_RoomDTO?> JoinRoomAsScreen(string roomCode)
        {
            try
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
                        var room = RoomService.GetByRoomCode(roomCode);
                        if (room != null)
                        {
                            screen.RoomCode = roomCode;
                            ScreenService.AddOrUpdate(screen);
                            
                            // Add to room group
                            await Groups.AddToGroupAsync(Context.ConnectionId, room.Identifier);
                            
                            await SendRefresh(room.Identifier);
                            
                            var updatedRoom = RoomService.GetById(room.Identifier);
                            
                            return mapper.Map<TR_RoomDTO>(updatedRoom);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ScreenHub] Error in JoinRoomAsScreen: {ex.Message}");
                Console.WriteLine($"[ScreenHub] Stack trace: {ex.StackTrace}");
                throw;
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
            await SendRefresh(room.Identifier);

            return mapper.Map<TR_RoomDTO>(room);
        }
    }
}
