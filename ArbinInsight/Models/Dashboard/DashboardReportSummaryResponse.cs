namespace ArbinInsight.Models.Dashboard
{
    public class DashboardReportSummaryResponse
    {
        public DateTime GeneratedAtUtc { get; set; }
        public DateTime? FromUtc { get; set; }
        public DateTime? ToUtc { get; set; }
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public int RunningTests { get; set; }
        public int DistinctMachines { get; set; }
        public int DistinctBarcodes { get; set; }
    }
}
