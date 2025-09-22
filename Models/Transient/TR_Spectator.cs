using keynote_asp.Helpers;

namespace keynote_asp.Models.Transient
{
    public class TR_Spectator : TR_BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public bool IsHandRaised { get; set; } = false;
    }

    public class TR_SpectatorDTO : TR_BaseEntityDTO
    {
        public string Name { get; set; } = string.Empty;
        public bool IsHandRaised { get; set; } = false;
    }
}
