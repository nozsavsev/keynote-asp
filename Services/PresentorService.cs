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
    public class PresentorRTService(KeynoteHub hub, KeynoteService keynoteService)
    {
        public TR_Room CreateRoom(TR_Presentor presentor)
        {
            var room = new TR_Room();
            RoomService.AddOrUpdate(room);

            PresentorService.joinRoom(presentor.Identifier, room.Identifier);

            hub.SendRefresh(room.Identifier);

            return room;
        }

        public TR_Room SetKeynote(string roomIdentifier, string keynoteId)
        {
            var room = RoomService.GetById(roomIdentifier);
            room.Keynote = keynoteService.GetByIdAsync(long.Parse(keynoteId)).Result;
            RoomService.AddOrUpdate(room);
            hub.SendRefresh(room.Identifier);

            return room;
        }

        public TR_Room SetPage(string roomIdentifier, int page)
        {
            var room = RoomService.GetById(roomIdentifier);

            if (room.Keynote == null)
            {
                return room;
            }

            room.currentFrame =
                page > (room.Keynote?.TotalFrames ?? 0) ? (room.Keynote?.TotalFrames ?? 0)
                : page < 0 ? 0
                : page;
            RoomService.AddOrUpdate(room);
            hub.SendRefresh(room.Identifier);

            return room;
        }

        public TR_Room SetShowSpectatorQR(string roomIdentifier, bool show)
        {
            var room = RoomService.GetById(roomIdentifier);

            if (room.Keynote == null)
            {
                return room;
            }

            room.ShowSpectatorQR = show;
            RoomService.AddOrUpdate(room);
            hub.SendRefresh(room.Identifier);

            return room;
        }

        public TR_Room GiveTempControl(string roomIdentifier, string spectatorId)
        {
            var room = RoomService.GetById(roomIdentifier);
            room.TempControlSpectatorId = spectatorId;
            RoomService.AddOrUpdate(room);
            hub.SendRefresh(room.Identifier);

            return room;
        }

        public TR_Room TakeTempControl(string roomIdentifier)
        {
            var room = RoomService.GetById(roomIdentifier);
            room.TempControlSpectatorId = null;
            RoomService.AddOrUpdate(room);
            hub.SendRefresh(room.Identifier);

            return room;
        }
    }
}
