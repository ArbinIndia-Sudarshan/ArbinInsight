namespace ArbinInsight.Models.Dashboard
{
    public class DashboardTestReportItem
    {
        public string MachineCode { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string SourceConnectionName { get; set; } = string.Empty;
        public string SourceTestKey { get; set; } = string.Empty;
        public string? TestName { get; set; }
        public string? Barcode { get; set; }
        public string? Result { get; set; }
        public int? ChannelIndex { get; set; }
        public string? BinNumber { get; set; }
        public string? TestProfileName { get; set; }
        public DateTime? StartDateTimeUtc { get; set; }
        public DateTime? EndDateTimeUtc { get; set; }
        public int SubTestCount { get; set; }
        public int FailedLimitCount { get; set; }
        public DateTime LastSyncedAtUtc { get; set; }
    }
}
