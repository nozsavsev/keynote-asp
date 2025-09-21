using Microsoft.Kiota.Abstractions;
using Keynote_asp.Nauth.API_GEN.Models;
using Microsoft.Extensions.Logging;
using Keynote_asp.Nauth.API_GEN;

namespace keynote_asp.Services
{
    public interface INauthApiService
    {
        Task<FullSessionDTOResponseWrapper?> DecodeUserTokenAsync(string token);
        Task<PermissionDTOResponseWrapper?> CreateServicePermissionAsync(CreatePermissionDTO dto);
        Task<ServiceDTOResponseWrapper?> GetCurrentServiceAsync();
    }

    public class NauthApiService : INauthApiService
    {
        private readonly ApiClient _apiClient;
        private readonly ILogger<NauthApiService> _logger;

        public NauthApiService(ApiClient apiClient, ILogger<NauthApiService> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        public async Task<FullSessionDTOResponseWrapper?> DecodeUserTokenAsync(string token)
        {
            try
            {
                return await _apiClient.Api.Nauth.DecodeUserToken.PostAsync(config =>
                {
                    config.QueryParameters.Token = token;
                });
            }
            catch (ApiException ex)
            {
                _logger.LogWarning("DecodeUserToken failed with status code {StatusCode}. Message: {Message}", ex.ResponseStatusCode, ex.Message);

                // Return a response wrapper with error status instead of throwing exception for any HTTP error
                return CreateErrorResponse<FullSessionDTOResponseWrapper>(ex);
            }
        }

        public async Task<PermissionDTOResponseWrapper?> CreateServicePermissionAsync(CreatePermissionDTO dto)
        {
            try
            {
                return await _apiClient.Api.Nauth.CreateServicePermission.PostAsync(dto);
            }
            catch (ApiException ex)
            {
                _logger.LogWarning("CreateServicePermission failed with status code {StatusCode}. Message: {Message}", ex.ResponseStatusCode, ex.Message);

                // Return a response wrapper with error status instead of throwing exception for any HTTP error
                var value = CreateErrorResponse<PermissionDTOResponseWrapper>(ex);
                return value;
            }
        }

        public async Task<ServiceDTOResponseWrapper?> GetCurrentServiceAsync()
        {
            try
            {
                return await _apiClient.Api.Nauth.CurrentService.GetAsync();
            }
            catch (ApiException ex)
            {
                _logger.LogWarning("GetCurrentService failed with status code {StatusCode}. Message: {Message}", ex.ResponseStatusCode, ex.Message);

                // Return a response wrapper with error status instead of throwing exception for any HTTP error
                return CreateErrorResponse<ServiceDTOResponseWrapper>(ex);
            }
        }

        /// <summary>
        /// Creates an error response wrapper from an ApiException for any HTTP status code
        /// </summary>
        private T CreateErrorResponse<T>(ApiException ex) where T : class, new()
        {
            var errorResponse = new T();

            // Use reflection to set common properties that exist on response wrapper types
            var statusProperty = typeof(T).GetProperty("Status");
            if (statusProperty != null)
            {
                // Map HTTP status codes to WrResponseStatus enum values
                var errorStatus = MapHttpStatusToWrResponseStatus((int)ex.ResponseStatusCode);
                statusProperty.SetValue(errorResponse, errorStatus);
            }

            var authFailureReasonsProperty = typeof(T).GetProperty("AuthenticationFailureReasons");
            if (authFailureReasonsProperty != null)
            {
                // Set authentication failure reasons if this is an auth-related error
                if (ex.ResponseStatusCode == 401 || ex.ResponseStatusCode == 403)
                {
                    var failureReasons = new List<Keynote_asp.Nauth.API_GEN.Models.AuthFailureReasons?>();
                    if (ex.ResponseStatusCode == 401)
                    {
                        failureReasons.Add(Keynote_asp.Nauth.API_GEN.Models.AuthFailureReasons.SessionExpired);
                    }
                    else if (ex.ResponseStatusCode == 403)
                    {
                        failureReasons.Add(Keynote_asp.Nauth.API_GEN.Models.AuthFailureReasons.ForeginResource);
                    }
                    authFailureReasonsProperty.SetValue(errorResponse, failureReasons);
                }
            }

            return errorResponse;
        }

        /// <summary>
        /// Maps HTTP status codes to WrResponseStatus enum values
        /// </summary>
        private Keynote_asp.Nauth.API_GEN.Models.WrResponseStatus MapHttpStatusToWrResponseStatus(int statusCode)
        {
            return statusCode switch
            {
                400 => Keynote_asp.Nauth.API_GEN.Models.WrResponseStatus.BadRequest,
                401 => Keynote_asp.Nauth.API_GEN.Models.WrResponseStatus.Unauthorized,
                403 => Keynote_asp.Nauth.API_GEN.Models.WrResponseStatus.Forbidden,
                404 => Keynote_asp.Nauth.API_GEN.Models.WrResponseStatus.NotFound,
                429 => Keynote_asp.Nauth.API_GEN.Models.WrResponseStatus.Cooldown,
                503 => Keynote_asp.Nauth.API_GEN.Models.WrResponseStatus.ServerDown,
                >= 500 => Keynote_asp.Nauth.API_GEN.Models.WrResponseStatus.InternalError,
                _ => Keynote_asp.Nauth.API_GEN.Models.WrResponseStatus.BadRequest
            };
        }
    }
}