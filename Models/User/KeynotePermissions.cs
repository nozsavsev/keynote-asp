using System.Text.Json.Serialization;

namespace keynote_asp.Models.User
{
    public enum KeynotePermissions
    {
        [JsonPropertyName("PrUploadFiles")]
        PrUploadFiles,

        [JsonPropertyName("PrAdminManageKeynotes")]
        PrAdminManageKeynotes,
    }
}
