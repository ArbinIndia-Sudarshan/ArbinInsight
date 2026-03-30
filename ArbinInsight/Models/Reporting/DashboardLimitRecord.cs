using System.ComponentModel.DataAnnotations;

namespace ArbinInsight.Models.Reporting
{
    public class DashboardLimitRecord
    {
        [Key]
        public long Id { get; set; }
        public long? DashboardTestRunId { get; set; }
        public DashboardTestRun? DashboardTestRun { get; set; }
        public long? DashboardSubTestRunId { get; set; }
        public DashboardSubTestRun? DashboardSubTestRun { get; set; }
        public string SourceConnectionName { get; set; } = string.Empty;
        public string SourceLimitKey { get; set; } = string.Empty;
        public string? ParentSourceTestKey { get; set; }
        public string? ParentSourceSubTestKey { get; set; }
        public string? LimitName { get; set; }
        public double? MinValue { get; set; }
        public double? MaxValue { get; set; }
        public string? MeasuredValue { get; set; }
        public string? Unit { get; set; }
        public string? Tolerance { get; set; }
        public string? Result { get; set; }
        public string PayloadJson { get; set; } = string.Empty;
        public Guid LastMessageId { get; set; }
        public DateTime LastSyncedAtUtc { get; set; }
    }
}
