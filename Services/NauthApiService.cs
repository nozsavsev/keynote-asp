using System.Text.Json;
using Keynote_asp.Nauth.API_GEN;
using Keynote_asp.Nauth.API_GEN.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions;

namespace keynote_asp.Services
{

    public class NauthResponseHandler(JsonSerializerOptions jsonSerializerOptions) : NativeResponseHandler
    {
        public async Task<T?> HandleResponse<T>() where T : class, new()
        {
            var http = (HttpResponseMessage)base.Value!;
            var json = await http.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, jsonSerializerOptions);
        }
    }

    public interface INauthApiService
    {
        Task<FullSessionDTOResponseWrapper?> DecodeUserTokenAsync(string token);
        Task<PermissionDTOResponseWrapper?> CreateServicePermissionAsync(CreatePermissionDTO dto);
        Task<ServiceDTOResponseWrapper?> GetCurrentServiceAsync();
        Task<StringResponseWrapper?> DeleteServicePermissionAsync(string permissionId);
        Task<FullSessionDTOResponseWrapper?> UpdateUserPermissionsAsync(
            ServiceUpdateUserPermissionsDTO dto);
    }

    public class NauthApiService : INauthApiService
    {
        private readonly ApiClient _apiClient;
        private readonly ILogger<NauthApiService> _logger;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public NauthApiService(
            ApiClient apiClient,
            ILogger<NauthApiService> logger,
            JsonSerializerOptions jsonSerializerOptions)
        {
            _apiClient = apiClient;
            _logger = logger;
            _jsonSerializerOptions = jsonSerializerOptions;
        }

        public async Task<FullSessionDTOResponseWrapper?> DecodeUserTokenAsync(string token)
        {
            try
            {
                var native = new NauthResponseHandler(_jsonSerializerOptions);

                await _apiClient.Api.Nauth.DecodeUserToken.PostAsync(config =>
                {
                    config.QueryParameters.Token = token;
                    config.Options.Add(new ResponseHandlerOption { ResponseHandler = native });
                });

                var result = await native.HandleResponse<FullSessionDTOResponseWrapper>();
                return result;
            }
            catch (ApiException ex)
            {
                _logger.LogWarning("DecodeUserToken failed with status code {StatusCode}. Message: {Message}", ex.ResponseStatusCode, ex.Message);
                return CreateErrorResponse<FullSessionDTOResponseWrapper>(ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in DecodeUserToken");
                return new FullSessionDTOResponseWrapper { Status = Keynote_asp.Nauth.API_GEN.Models.WrResponseStatus.InternalError };
            }
        }

        public async Task<PermissionDTOResponseWrapper?> CreateServicePermissionAsync(CreatePermissionDTO dto)
        {
            try
            {
                var result = await _apiClient.Api.Nauth.CreateServicePermission.PostAsync(dto);
                return result;
            }
            catch (ApiException ex)
            {
                _logger.LogWarning("CreateServicePermission failed with status code {StatusCode}. Message: {Message}", ex.ResponseStatusCode, ex.Message);
                return CreateErrorResponse<PermissionDTOResponseWrapper>(ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in CreateServicePermission");
                return new PermissionDTOResponseWrapper { Status = Keynote_asp.Nauth.API_GEN.Models.WrResponseStatus.InternalError };
            }
        }

        public async Task<ServiceDTOResponseWrapper?> GetCurrentServiceAsync()
        {
            try
            {
                var result = await _apiClient.Api.Nauth.CurrentService.GetAsync();
                return result;
            }
            catch (ApiException ex)
            {
                _logger.LogWarning("GetCurrentService failed with status code {StatusCode}. Message: {Message}", ex.ResponseStatusCode, ex.Message);
                return CreateErrorResponse<ServiceDTOResponseWrapper>(ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetCurrentService");
                return new ServiceDTOResponseWrapper { Status = Keynote_asp.Nauth.API_GEN.Models.WrResponseStatus.InternalError };
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

        public async Task<StringResponseWrapper?> DeleteServicePermissionAsync(string permissionId)
        {
            try
            {
                var result = await _apiClient.Api.Nauth.DeleteServicePermission.PostAsync(config =>
                {
                    config.QueryParameters.PermissionId = permissionId;
                });
                return result;
            }
            catch (ApiException ex)
            {
                _logger.LogWarning("DeleteServicePermission failed with status code {StatusCode}. Message: {Message}", ex.ResponseStatusCode, ex.Message);
                return CreateErrorResponse<StringResponseWrapper>(ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in DeleteServicePermission");
                return new StringResponseWrapper { Status = Keynote_asp.Nauth.API_GEN.Models.WrResponseStatus.InternalError };
            }
        }

        public async Task<FullSessionDTOResponseWrapper?> UpdateUserPermissionsAsync(ServiceUpdateUserPermissionsDTO dto)
        {
            try
            {
                var result = await _apiClient.Api.Nauth.UpdateUserPermissions.PostAsync(dto);
                return result;
            }
            catch (ApiException ex)
            {
                _logger.LogWarning("UpdateUserPermissions failed with status code {StatusCode}. Message: {Message}", ex.ResponseStatusCode, ex.Message);
                return CreateErrorResponse<FullSessionDTOResponseWrapper>(ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in UpdateUserPermissions");
                return new FullSessionDTOResponseWrapper { Status = Keynote_asp.Nauth.API_GEN.Models.WrResponseStatus.InternalError };
            }
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