using keynote_asp.Repositories;

namespace keynote_asp.Services
{
    public class GenericService<T> where T : class
    {
        protected readonly GenericRepository<T> _repository;

        public GenericService(GenericRepository<T> repository)
        {
            _repository = repository;
        }

        public virtual async Task<T?> GetByIdAsync(long id, bool tracking = false)
        {
            return await _repository.GetByIdAsync(id, tracking: tracking);
        }

        public virtual async Task DeleteByidAsync(long id)
        {
            await _repository.DeleteByIdAsync(id);
        }

    }
}
