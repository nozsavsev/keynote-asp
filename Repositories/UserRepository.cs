using keynote_asp.DbContexts;
using keynote_asp.Models.User;

namespace keynote_asp.Repositories
{
    public class UserRepository : GenericRepository<DB_User>
    {
        public UserRepository(KeynoteDbContext context) : base(context)
        {
        }
    }
}
