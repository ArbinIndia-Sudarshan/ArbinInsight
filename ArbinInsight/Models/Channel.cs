using System.Text.Json.Serialization;

namespace ArbinInsight.Models
{
    public class Channel
    {
        public int Id { get; set; }
        public Guid? PublisherNodeId { get; set; }
        public int? SourceLocalId { get; set; }
        public Guid? LastMessageId { get; set; }
        public DateTime? BrokerReceivedAtUtc { get; set; }
        public int? MachineDataId { get; set; }
        [JsonIgnore]
        public MachineData? MachineData { get; set; }
        public float ambientTemperature { get; set; }
        public uint ChannelIndex { get; set; }
        public int TestID { get; set; }
        public string BarCode { get; set; }
        public string TestName { get; set; }
        public string ChannelStatus { get; set; }
        public bool IsRunning { get; set; }
        public string Result { get; set; }
        public bool Retest { get; set; }
        public string RetestNumber { get; set; }
        public string UserName { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public int TestProfileId { get; set; }
        public TestProfile testProfile { get; set; }
        public List<CANMessagePair> cANMessagePairList { get; set; }
        public List<SMBMessagePair> sMBMessagePairList { get; set; }
        public bool ManuallyStopFlag { get; set; }
        public bool StopTestsExecuted { get; set; }
        public string BINNumber { get; set; }
    }
}
