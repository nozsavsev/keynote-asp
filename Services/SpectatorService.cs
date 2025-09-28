using keynote_asp.Dtos;
using keynote_asp.Helpers;
using keynote_asp.Models.Keynote;
using keynote_asp.Models.Transient;
using keynote_asp.Repositories;
using keynote_asp.Services;
using keynote_asp.Services.Transient;
using keynote_asp.SignalRHubs;

namespace keynote_asp.Services
{
    public class SpectatorRTService(BaseHub hub, KeynoteService keynoteService)
    {
        public TR_Spectator SetHandRaised(TR_Spectator spectator)
        {
            var room = RoomService.GetByRoomCode(spectator.RoomCode);
            spectator = SpectatorService.GetById(spectator.Identifier);

            spectator.IsHandRaised = true;
            SpectatorService.AddOrUpdate(spectator);

            hub.SendRefresh(room.Identifier).Wait();

            return spectator;
        }
    }
}
