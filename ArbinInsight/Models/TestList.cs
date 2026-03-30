using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ArbinInsight.Models
{
    public class TestList
    {
        [Key]
        public int Test_ID { get; set; }
        public Guid? PublisherNodeId { get; set; }
        public int? SourceLocalId { get; set; }
        public int? ChannelId { get; set; }
        [JsonIgnore]
        public Channel? Channel { get; set; }
        public int? TestProfileId { get; set; }
        [JsonIgnore]
        public TestProfile? TestProfile { get; set; }
        public Guid? LastMessageId { get; set; }
        public DateTime? BrokerReceivedAtUtc { get; set; }
        public string? Test_Name { get; set; }
        public string? Barcode { get; set; }
        public string? Result { get; set; }
        public string? Retest { get; set; }
        public long? Start_Date_Time { get; set; }
        public long? End_Date_Time { get; set; }
        public string? User_Name { get; set; }
        public int? Channel_Index { get; set; }
        public string? BIN_Number { get; set; }
        public string? TestProjectName { get; set; }
        public string? TestProfile_Name { get; set; }
        public int? MachineDataId { get; set; }
        [JsonIgnore]
        public MachineData? MachineData { get; set; }
    }
}
