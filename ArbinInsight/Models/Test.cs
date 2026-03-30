using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ArbinInsight.Models
{
    public class Test
    {
        [Key]
        public int Id { get; set; }
        public Guid? PublisherNodeId { get; set; }
        public int? SourceLocalId { get; set; }
        public int? ChannelId { get; set; }
        [JsonIgnore]
        public Channel? Channel { get; set; }
        public Guid? LastMessageId { get; set; }
        public DateTime? BrokerReceivedAtUtc { get; set; }
        public int TestID { get; set; }
        public bool Enable { get; set; }
        public bool StopOnFail { get; set; }
        public string TestName { get; set; }
        public string ScheduleName { get; set; }
        public string TestStatus { get; set; }
        public string Result { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public int? TestProfileId { get; set; }
        public List<Limit> Limits { get; set; }
    }
}
