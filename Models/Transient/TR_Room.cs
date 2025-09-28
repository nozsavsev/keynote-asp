using keynote_asp.Helpers;
using keynote_asp.Models.Keynote;
using keynote_asp.Services.Transient;

namespace keynote_asp.Models.Transient
{
    public class TR_Room : TR_BaseEntity
    {
        public TR_Room()
        {
            RoomCode = RoomCodeGenerator.Generate();
        }

        public DB_Keynote? Keynote { get; set; } = null;
        public int currentFrame { get; set; } = 0;
        public bool ShowSpectatorQR { get; set; } = false;
        public string? TempControlSpectatorId { get; set; } = null;

        public TR_Presentor? Presentor
        {
            get { return PresentorService.QuerySingle(x => x.Where(y => y.RoomCode == RoomCode)); }
        }

        public TR_Screen? Screen
        {
            get { return ScreenService.QuerySingle(x => x.Where(y => y.RoomCode == RoomCode)); }
        }

        public List<TR_Spectator>? Spectators
        {
            get { return SpectatorService.QueryMany(x => x.Where(y => y.RoomCode == RoomCode)); }
        }
    }

    public class TR_RoomDTO : TR_BaseEntityDTO
    {
        public DB_Keynote? Keynote { get; set; } = null;
        public int currentFrame { get; set; } = 1;
        public bool ShowSpectatorQR { get; set; } = false;
        public string? TempControlSpectatorId { get; set; } = null;
        public TR_PresentorDTO? Presentor { get; set; } = null;
        public TR_ScreenDTO? Screen { get; set; } = null;
        public List<TR_SpectatorDTO>? Spectators { get; set; } = null;
    }
}
