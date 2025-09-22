using keynote_asp.Models.User;
using Keynote_asp.Nauth.API_GEN;
using Keynote_asp.Nauth.API_GEN.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Kiota.Abstractions;

namespace keynote_asp.Services
{
    public class CachedCurrentService
    {
        private readonly INauthApiService _nauthApiService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CachedCurrentService> _logger;
        private const string CurrentServiceCacheKey = "CurrentService";

        public CachedCurrentService(INauthApiService nauthApiService, IMemoryCache cache, ILogger<CachedCurrentService> logger)
        {
            _nauthApiService = nauthApiService;
            _cache = cache;
            _logger = logger;
        }

        public async Task<string?> getPermissionIdByKey(string key)
        {

            var service = await GetCurrentServiceAsync();

            if (service == null)
            {
                return string.Empty;
            }

            var permission = service.Permissions!.FirstOrDefault(p => p.Key == key);
            return permission?.Id ?? string.Empty;
        }

        public async Task<PermissionBasicDTO?> getPermission(KeynotePermissions _permission)
        {
            var key = _permission.ToString();

            var service = await GetCurrentServiceAsync();

            var permission = service!.Permissions!.FirstOrDefault(p => p.Key == key);
            return permission;
        }

        public async Task<ServiceDTO?> GetCurrentServiceAsync()
        {
            if (_cache.TryGetValue(CurrentServiceCacheKey, out ServiceDTO? service))
            {
                return service;
            }

            var result = await _nauthApiService.GetCurrentServiceAsync();
            service = result?.Response;

            if (service != null)
            {
                _cache.Set(CurrentServiceCacheKey, service, TimeSpan.FromMinutes(15));
            }
            else
            {
                _logger.LogWarning("GetCurrentServiceAsync failed. Status: {Status}", 
                    result?.Status);
            }

            return service;
        }
    }
}
