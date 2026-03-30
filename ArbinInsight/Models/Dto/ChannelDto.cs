namespace ArbinInsight.Models.Dto
{
    public class ChannelDto
    {
        public int Id { get; set; }
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
        public TestProfileDto testProfile { get; set; }
        public List<CANMessagePairDto> cANMessagePairList { get; set; }
        public List<SMBMessagePairDto> sMBMessagePairList { get; set; }
        public bool ManuallyStopFlag { get; set; }
        public bool StopTestsExecuted { get; set; }
        public string BINNumber { get; set; }
    }
}
