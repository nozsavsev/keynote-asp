using keynote_asp.Helpers;
using keynote_asp.Models.User;

namespace keynote_asp.Models.Keynote
{
    public enum KeynoteType
    {
        Pdf,
    }

    public enum KeynoteTransitionType
    {
        none,
    }

    public class DB_Keynote
    {
        public long Id { get; set; } = SnowflakeGlobal.Generate();

        public KeynoteTransitionType TransitionType { get; set; } = KeynoteTransitionType.none;
        public KeynoteType Type { get; set; } = KeynoteType.Pdf;
        public int TotalFrames { get; set; } = 0;
        public string KeynoteUrl { get; set; } = string.Empty;
        public string? MobileKeynoteUrl { get; set; } = string.Empty;
        public string? PresentorNotesUrl { get; set; } = string.Empty;

        public string? Name { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public long UserId { get; set; }
        public DB_User User { get; set; } = null!;
    }
}
