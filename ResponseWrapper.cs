using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Keynote_asp.Nauth.API_GEN.Models;

namespace keynote_asp
{
    public enum WrResponseStatus
    {
        [JsonPropertyName("Success")]
        Success = 0,

        [JsonPropertyName("InternalError")]
        InternalError,

        [JsonPropertyName("Ok")]
        Ok,

        [JsonPropertyName("Forbidden")]
        Forbidden,

        [JsonPropertyName("Unauthorized")]
        Unauthorized,

        [JsonPropertyName("NotFound")]
        NotFound,

        [JsonPropertyName("BadRequest")]
        BadRequest,

        [JsonPropertyName("Cooldown")]
        Cooldown,

        [JsonPropertyName("ServerDown")]
        ServerDown,

        [JsonPropertyName("EmailNotAvailable")]
        EmailNotAvailable,

        [JsonPropertyName("InvalidEmail")]
        InvalidEmail,

        [JsonPropertyName("InvalidPassword")]
        InvalidPassword,

        [JsonPropertyName("InvalidApplyToken")]
        InvalidApplyToken,
    }

    public class ResponseWrapper<R> where R : class?
    {
        public ResponseWrapper(WrResponseStatus status, [AllowNull] R response = null, List<AuthFailureReasons>? authenticationFailureReasons = null)
        {
            this.Status = status;
            this.Response = response;
            this.AuthenticationFailureReasons = authenticationFailureReasons;
        }

        public ResponseWrapper(string status, [AllowNull] R response = null)
        {
            this.Status = (WrResponseStatus)Enum.Parse(typeof(WrResponseStatus), status);
            this.Response = response;
        }

        public WrResponseStatus Status { get; set; }
        public List<AuthFailureReasons>? AuthenticationFailureReasons { get; set; } = null;
        public R? Response { get; set; }
    }
}