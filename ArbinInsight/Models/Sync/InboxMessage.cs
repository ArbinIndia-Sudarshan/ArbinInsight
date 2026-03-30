using System.ComponentModel.DataAnnotations;

namespace ArbinInsight.Models.Sync
{
    public class InboxMessage
    {
        [Key]
        public long InboxId { get; set; }
        public Guid MessageId { get; set; }
        public Guid PublisherNodeId { get; set; }
        public string MessageType { get; set; }
        public string RoutingKey { get; set; }
        public string PayloadJson { get; set; }
        public DateTime ReceivedAtUtc { get; set; }
        public DateTime? ProcessedAtUtc { get; set; }
        public byte Status { get; set; }
        public string? ErrorText { get; set; }
    }
}
