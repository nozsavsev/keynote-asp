using keynote_asp.Models.User;

namespace keynote_asp.Models.Keynote
{
    public class DB_Keynote
    {
        public long Id { get; set; }
        public string name { get; set; } = string.Empty;
        public long UserId { get; set; }
        public DB_User User { get; set; } = null!;
    }
}
