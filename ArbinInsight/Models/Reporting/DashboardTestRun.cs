using System.ComponentModel.DataAnnotations;

namespace ArbinInsight.Models.Reporting
{
    public class DashboardTestRun
    {
        [Key]
        public long Id { get; set; }
        public Guid PublisherNodeId { get; set; }
        public string SourceConnectionName { get; set; } = string.Empty;
        public string SourceTestKey { get; set; } = string.Empty;
        public string? TestName { get; set; }
        public string? Barcode { get; set; }
        public string? Result { get; set; }
        public string? Retest { get; set; }
        public DateTime? StartDateTimeUtc { get; set; }
        public DateTime? EndDateTimeUtc { get; set; }
        public long? StartDateTimeRaw { get; set; }
        public long? EndDateTimeRaw { get; set; }
        public string? UserName { get; set; }
        public int? ChannelIndex { get; set; }
        public string? BinNumber { get; set; }
        public string? TestProjectName { get; set; }
        public string? TestProfileName { get; set; }
        public string PayloadJson { get; set; } = string.Empty;
        public Guid LastMessageId { get; set; }
        public DateTime LastSyncedAtUtc { get; set; }
        public List<DashboardSubTestRun> SubTests { get; set; } = new();
        public List<DashboardLimitRecord> Limits { get; set; } = new();
    }
}
