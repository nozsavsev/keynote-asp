
namespace keynote_asp.Services
{
    public interface IGenericService<T> where T : class
    {
        Task<T?> GetByIdAsync(long id, bool tracking = false);
        Task DeleteByidAsync(long id);
    }
}
