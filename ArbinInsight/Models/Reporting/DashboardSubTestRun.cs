using System.ComponentModel.DataAnnotations;

namespace ArbinInsight.Models.Reporting
{
    public class DashboardSubTestRun
    {
        [Key]
        public long Id { get; set; }
        public long DashboardTestRunId { get; set; }
        public DashboardTestRun DashboardTestRun { get; set; } = null!;
        public string SourceConnectionName { get; set; } = string.Empty;
        public string SourceSubTestKey { get; set; } = string.Empty;
        public string? ParentSourceTestKey { get; set; }
        public string? TestName { get; set; }
        public string? Result { get; set; }
        public string? TestStatus { get; set; }
        public bool? Enable { get; set; }
        public bool? StopOnFail { get; set; }
        public string? ScheduleName { get; set; }
        public DateTime? StartDateTimeUtc { get; set; }
        public DateTime? EndDateTimeUtc { get; set; }
        public string PayloadJson { get; set; } = string.Empty;
        public Guid LastMessageId { get; set; }
        public DateTime LastSyncedAtUtc { get; set; }
        public List<DashboardLimitRecord> Limits { get; set; } = new();
    }
}
