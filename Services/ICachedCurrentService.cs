using Keynote_asp.Nauth.API_GEN.Models;

namespace keynote_asp.Services
{
    public interface ICachedCurrentService
    {
        Task<ServiceDTO?> GetCurrentServiceAsync();
    }
}
