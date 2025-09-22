using keynote_asp.Models.User;
using keynote_asp.Repositories;

namespace keynote_asp.Services
{
    public class UserService : GenericService<DB_User>
    {
        public UserService(UserRepository repository) : base(repository)
        {
        }
    }
}
