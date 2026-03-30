using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ArbinInsight.Models
{
    public class Limit
    {
        [Key]
        public int LimitID { get; set; }
        public Guid? PublisherNodeId { get; set; }
        public int? SourceLocalId { get; set; }
        public Guid? LastMessageId { get; set; }
        public DateTime? BrokerReceivedAtUtc { get; set; }
        public string LimitName { get; set; }
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public string MeasuredValue { get; set; }
        public string Unit { get; set; }
        public string Tolerance { get; set; }
        public string Result { get; set; }
        public int? TestId { get; set; }
        [JsonIgnore]
        public Test? Test { get; set; }
    }
}
