using keynote_asp.DbContexts;
using keynote_asp.Models.Keynote;

namespace keynote_asp.Repositories
{
    public class KeynoteRepository : GenericRepository<DB_Keynote>
    {
        public KeynoteRepository(KeynoteDbContext context) : base(context)
        {
        }
    }
}
