using keynote_asp.Models.Keynote;
using System.Collections.Generic;

namespace keynote_asp.Models.User
{
    public class DB_User
    {
        public long Id { get; set; }
        public ICollection<DB_Keynote> keynotes { get; set; } = new List<DB_Keynote>();
    }
}
