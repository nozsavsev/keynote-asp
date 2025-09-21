using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

namespace keynote_asp.AuthHandlers
{
    public class ServiceTokenAuthenticationProvider : IAuthenticationProvider
    {
        private readonly string _token;

        public ServiceTokenAuthenticationProvider(IConfiguration configuration)
        {
            _token = configuration["nauth:serviceToken"] ! ;// throw new InvalidOperationException("Nauth service token not configured");
        }

        public Task AuthenticateRequestAsync(RequestInformation request, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!request.Headers.ContainsKey("Authorization"))
            {
                request.Headers.Add("Authorization", $"Bearer {_token}");
            }
            
            return Task.CompletedTask;
        }
    }
}
