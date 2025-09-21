using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using keynote_asp.Models.User;
using keynote_asp.Repositories;
using Keynote_asp.Nauth.API_GEN;
using Keynote_asp.Nauth.API_GEN.Models;
using Microsoft.Kiota.Abstractions;
using keynote_asp.Services;

namespace keynote_asp.AuthHandlers
{
    public class NauthAuthenticationOptions : AuthenticationSchemeOptions
    {
        public string CookieKey { get; set; } = "auth_token";
    }

    public class NauthAuthenticationHandler : AuthenticationHandler<NauthAuthenticationOptions>
    {
        private readonly INauthApiService _nauthApiService;
        private readonly UserRepository _userRepository;
        private readonly ILogger<NauthAuthenticationHandler> _logger;

        public NauthAuthenticationHandler(
            IOptionsMonitor<NauthAuthenticationOptions> options,
            ILoggerFactory logger_factory,
            UrlEncoder encoder,
            INauthApiService nauthApiService,
            UserRepository userRepository,
            ILogger<NauthAuthenticationHandler> logger)
            : base(options, logger_factory, encoder)
        {
            _nauthApiService = nauthApiService;
            _userRepository = userRepository;
            _logger = logger;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            string token = null;

            // Try to get token from Authorization header
            string authorization = Request.Headers["Authorization"];
            if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = authorization.Substring("Bearer ".Length).Trim();
            }
            // If no header, try cookie
            else if (Request.Cookies.TryGetValue(Options.CookieKey, out var cookieToken))
            {
                token = cookieToken;
            }

            if (string.IsNullOrEmpty(token))
            {
                return AuthenticateResult.NoResult();
            }

            try
            {
                var decodeResult = await _nauthApiService.DecodeUserTokenAsync(token);

                if (decodeResult?.Response?.User == null || decodeResult?.Response == null)
                {
                    _logger.LogWarning("Nauth API returned invalid session for token. Status: {Status}, FailureReasons: {FailureReasons}", 
                        decodeResult?.Status, decodeResult?.AuthenticationFailureReasons);
                    Context.Items["AuthFailureReason"] = AuthFailureReasons.SessionExpired;
                    return AuthenticateResult.Fail("Invalid session");
                }
                
                var nauthUser = decodeResult.Response.User;
                var nauthSession = decodeResult.Response;

                // Store nauth user and session in HttpContext
                Context.Items["NauthUser"] = nauthUser;
                Context.Items["NauthSession"] = nauthSession;

                // Get or create keynote user
                var keynoteUserId = int.Parse(nauthUser.Id!);
                var keynoteUser = await _userRepository.GetByIdAsync(keynoteUserId);
                
                if (keynoteUser == null)
                {
                    keynoteUser = await _userRepository.AddAsync(new DB_User { Id = keynoteUserId });
                }

                // Store keynote user in context
                Context.Items["KeynoteUser"] = keynoteUser;

                // Create claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, nauthUser.Id!),
                    new Claim(ClaimTypes.Name, nauthUser.Name ?? nauthUser.Id!),
                    new Claim("SessionId", nauthSession.Id ?? "")
                };

                // Add permission claims
                if (nauthUser.Permissions != null)
                {
                    foreach (var userPermission in nauthUser.Permissions)
                    {
                        if (userPermission.Permission?.Key != null)
                        {
                            claims.Add(new Claim("permission", userPermission.Permission.Key));
                        }
                    }
                }

                // Add user status claims for authorization
                if (nauthUser.IsEmailVerified.HasValue)
                    claims.Add(new Claim("EmailVerified", nauthUser.IsEmailVerified.Value.ToString()));
                if (nauthUser.IsEnabled.HasValue)
                    claims.Add(new Claim("Enabled", nauthUser.IsEnabled.Value.ToString()));
                if (nauthSession.Is2FAConfirmed.HasValue)
                    claims.Add(new Claim("2FAConfirmed", nauthSession.Is2FAConfirmed.Value.ToString()));

                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);

                return AuthenticateResult.Success(ticket);
            }
            catch (Exception ex) // This will now only catch unexpected errors, not API error responses
            {
                _logger.LogError(ex, "Authentication failed due to an unexpected exception");
                return AuthenticateResult.Fail($"Authentication failed: {ex.Message}");
            }
        }
    }
}
