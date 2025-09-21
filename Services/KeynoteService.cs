using keynote_asp.Models.Keynote;
using keynote_asp.Repositories;

namespace keynote_asp.Services
{
    public class KeynoteService : GenericService<DB_Keynote>, IKeynoteService
    {
        public KeynoteService(KeynoteRepository repository) : base(repository)
        {
        }
    }
}
