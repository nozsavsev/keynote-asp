using keynote_asp.Models.Keynote;

namespace keynote_asp.Dtos
{
    public class KeynoteDTO
    {
        public string Id { get; set; } = null!;

        public KeynoteTransitionType TransitionType { get; set; } = KeynoteTransitionType.none;
        public KeynoteType Type { get; set; } = KeynoteType.Pdf;
        public int TotalFrames { get; set; } = 0;
        public string KeynoteUrl { get; set; } = string.Empty;
        public string? MobileKeynoteUrl { get; set; } = string.Empty;
        public string? PresentorNotesUrl { get; set; } = string.Empty;

        public string? Name { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string UserId { get; set; } = null!;
    }

    public class CreateKeynoteDTO
    {
        //add 3 files field
        public KeynoteTransitionType TransitionType { get; set; } = KeynoteTransitionType.none;
        public KeynoteType Type { get; set; } = KeynoteType.Pdf;
        public int TotalFrames { get; set; } = 0;
        public string? Name { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
    }
}
