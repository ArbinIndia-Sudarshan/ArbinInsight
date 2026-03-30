using System.ComponentModel.DataAnnotations;

namespace ArbinInsight.Models.Sync
{
    public class DeadLetterMessage
    {
        [Key]
        public long DeadLetterId { get; set; }
        public Guid MessageId { get; set; }
        public Guid PublisherNodeId { get; set; }
        public string MessageType { get; set; }
        public string RoutingKey { get; set; }
        public string PayloadJson { get; set; }
        public DateTime FailedAtUtc { get; set; }
        public int RetryCount { get; set; }
        public string? ErrorText { get; set; }
    }
}
