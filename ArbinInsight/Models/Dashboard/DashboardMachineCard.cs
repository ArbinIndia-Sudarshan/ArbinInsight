namespace ArbinInsight.Models.Dashboard
{
    public class DashboardMachineCard
    {
        public Guid PublisherNodeId { get; set; }
        public string MachineCode { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int RunningChannels { get; set; }
        public int PassCount { get; set; }
        public int FailCount { get; set; }
        public DateTime? LastUpdateUtc { get; set; }
    }
}
