using ArbinInsight.Models;

namespace ArbinInsight.Models
{
    public class MachineData
    {
        public int Id { get; set; }
        public Guid? PublisherNodeId { get; set; }
        public int? SourceLocalId { get; set; }
        public Guid? LastMessageId { get; set; }
        public DateTime? BrokerReceivedAtUtc { get; set; }
        public int MachineId { get; set; }
        public string MachineName { get; set; } = string.Empty;
        public double Capacity { get; set; }
        public string Status { get; set; } = string.Empty; //Connection status, e.g., "Connected", "Disconnected"
        public List<Channel> Channels { get; set; }
        public List<TestList> TestLists { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }
}
