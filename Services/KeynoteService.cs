using keynote_asp.Dtos;
using keynote_asp.Helpers;
using keynote_asp.Models.Keynote;
using keynote_asp.Repositories;

namespace keynote_asp.Services
{
    public class KeynoteService : GenericService<DB_Keynote>
    {
        public KeynoteService(KeynoteRepository repository) : base(repository)
        {
        }

        internal async Task<DB_Keynote> AddAsync(DB_Keynote keynote)
        {
            keynote.Id = SnowflakeGlobal.Generate();

            return await _repository.AddAsync(keynote);
        }
    }
}
