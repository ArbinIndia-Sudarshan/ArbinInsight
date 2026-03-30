namespace ArbinInsight.Models.Dashboard
{
    public class DashboardSummaryResponse
    {
        public DateTime GeneratedAtUtc { get; set; }
        public int TotalMachines { get; set; }
        public int RunningMachines { get; set; }
        public int IdleMachines { get; set; }
        public int ErrorMachines { get; set; }
        public int OfflineMachines { get; set; }
        public int TotalPass { get; set; }
        public int TotalFail { get; set; }
    }
}
