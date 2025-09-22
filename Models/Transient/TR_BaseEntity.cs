using keynote_asp.Helpers;

namespace keynote_asp.Models.Transient
{
    public class TR_BaseEntity
    {
        public string Identifier { get; set; } = SnowflakeGlobal.Generate().ToString();
        public string RoomCode { get; set; } = string.Empty;
        public bool IsConnected { get; set; } = false;
        public string ConnectionId { get; set; } = string.Empty;
        public DateTime? DisconnectedAt { get; set; } = null;
    }

    public class TR_BaseEntityDTO
    {
        public string Identifier { get; set; } = SnowflakeGlobal.Generate().ToString();
        public string RoomCode { get; set; } = string.Empty;
        public bool IsConnected { get; set; } = false;
        public string ConnectionId { get; set; } = string.Empty;
        public DateTime? DisconnectedAt { get; set; } = null;
    }
}
