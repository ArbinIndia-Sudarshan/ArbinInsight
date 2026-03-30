using System.Text.Json.Serialization;

namespace ArbinInsight.Models
{
    public class CANMessagePair
    {
        public int Id { get; set; }
        public Guid? PublisherNodeId { get; set; }
        public int? SourceLocalId { get; set; }
        public Guid? LastMessageId { get; set; }
        public DateTime? BrokerReceivedAtUtc { get; set; }
        public string VariableName { get; set; }
        public string Nickname { get; set; }
        public int? ChannelId { get; set; }
        [JsonIgnore]
        public Channel? Channel { get; set; }
    }
}
