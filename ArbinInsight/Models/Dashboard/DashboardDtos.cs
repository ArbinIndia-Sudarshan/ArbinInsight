namespace ArbinInsight.Models.Dashboard
{
    public class NetworkDashboardResponse
    {
        public DashboardTimeFilter TimeFilter { get; set; }
        public DateTime GeneratedAtUtc { get; set; }
        public DateTime RangeStartUtc { get; set; }
        public DateTime RangeEndUtc { get; set; }
        public int TotalMachines { get; set; }
        public int TotalBatteriesTested { get; set; }
        public int PassedCount { get; set; }
        public int FailedCount { get; set; }
        public int UnsafeCount { get; set; }
        public int InProgressCount { get; set; }
        public int AbortedCount { get; set; }
        public List<StatusMetricDto> MachineStatuses { get; set; } = new();
        public List<ResultMetricDto> BatteryResults { get; set; } = new();
        public ChannelCapacityDto ChannelCapacity { get; set; } = new();
        public AvailabilitySummaryDto Availability { get; set; } = new();
        public List<TrendPointDto> BatteriesTrend { get; set; } = new();
        public List<TrendPointDto> ResultTrend { get; set; } = new();
        public List<TrendPointDto> UptimeDowntimeTrend { get; set; } = new();
        public List<TrendPointDto> ChannelUtilizationTrend { get; set; } = new();
        public List<MachineDashboardListItemDto> Machines { get; set; } = new();
    }

    public class MachineDashboardResponse
    {
        public DashboardTimeFilter TimeFilter { get; set; }
        public DateTime GeneratedAtUtc { get; set; }
        public DateTime RangeStartUtc { get; set; }
        public DateTime RangeEndUtc { get; set; }
        public int MachineId { get; set; }
        public string MachineName { get; set; } = string.Empty;
        public string? HostName { get; set; }
        public string? IpAddress { get; set; }
        public string? Username { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? RawStatus { get; set; }
        public string? SoftwareVersion { get; set; }
        public DateTime? LastSeenUtc { get; set; }
        public double UptimeHours { get; set; }
        public double DowntimeHours { get; set; }
        public double RunningHours { get; set; }
        public int TotalBatteriesTested { get; set; }
        public int PassedCount { get; set; }
        public int FailedCount { get; set; }
        public int UnsafeCount { get; set; }
        public int InProgressCount { get; set; }
        public int AbortedCount { get; set; }
        public ChannelCapacityDto ChannelCapacity { get; set; } = new();
        public List<MachineChannelDto> Channels { get; set; } = new();
        public List<RecentTestDto> RecentTests { get; set; } = new();
        public List<RecentEventDto> RecentEvents { get; set; } = new();
        public List<TrendPointDto> BatteriesTrend { get; set; } = new();
        public List<TrendPointDto> ResultTrend { get; set; } = new();
        public List<TrendPointDto> UptimeDowntimeTrend { get; set; } = new();
        public List<TrendPointDto> ChannelUtilizationTrend { get; set; } = new();
    }

    public class MachineDashboardListItemDto
    {
        public int MachineId { get; set; }
        public string MachineName { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public string? Username { get; set; }
        public string Status { get; set; } = string.Empty;
        public int TotalChannels { get; set; }
        public int ActiveChannels { get; set; }
        public double ChannelUsagePercentage { get; set; }
        public int BatteriesTested { get; set; }
        public int PassedCount { get; set; }
        public int FailedCount { get; set; }
        public int UnsafeCount { get; set; }
        public double UptimeHours { get; set; }
        public double DowntimeHours { get; set; }
        public double RunningHours { get; set; }
        public DateTime? LastSeenUtc { get; set; }
    }

    public class MachineChannelDto
    {
        public int ChannelId { get; set; }
        public uint ChannelIndex { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsRunning { get; set; }
        public string? Result { get; set; }
        public string? Barcode { get; set; }
        public string? TestName { get; set; }
        public string? Username { get; set; }
        public string? TestProfileName { get; set; }
        public string? TestObjectName { get; set; }
        public float AmbientTemperature { get; set; }
        public DateTime? StartDateTimeUtc { get; set; }
        public DateTime? EndDateTimeUtc { get; set; }
        public bool ManuallyStopFlag { get; set; }
        public bool StopTestsExecuted { get; set; }
        public string? BinNumber { get; set; }
    }

    public class RecentTestDto
    {
        public int TestId { get; set; }
        public int? ChannelIndex { get; set; }
        public string? TestName { get; set; }
        public string? Barcode { get; set; }
        public string? Result { get; set; }
        public string? Username { get; set; }
        public DateTime? StartedAtUtc { get; set; }
        public DateTime? EndedAtUtc { get; set; }
        public string? TestProjectName { get; set; }
        public string? TestProfileName { get; set; }
        public string? BinNumber { get; set; }
    }

    public class RecentEventDto
    {
        public string EventType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime OccurredAtUtc { get; set; }
    }

    public class StatusMetricDto
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class ResultMetricDto
    {
        public string Result { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class ChannelCapacityDto
    {
        public int TotalChannels { get; set; }
        public int ActiveChannels { get; set; }
        public int FreeChannels { get; set; }
        public double UsagePercentage { get; set; }
    }

    public class AvailabilitySummaryDto
    {
        public double UptimeHours { get; set; }
        public double DowntimeHours { get; set; }
        public double RunningHours { get; set; }
    }

    public class TrendPointDto
    {
        public string Label { get; set; } = string.Empty;
        public DateTime BucketStartUtc { get; set; }
        public int Total { get; set; }
        public int Passed { get; set; }
        public int Failed { get; set; }
        public int Unsafe { get; set; }
        public int InProgress { get; set; }
        public int Aborted { get; set; }
        public int Running { get; set; }
        public int Idle { get; set; }
        public int Down { get; set; }
        public int Offline { get; set; }
        public int Maintenance { get; set; }
        public int Alarm { get; set; }
        public double UptimeHours { get; set; }
        public double DowntimeHours { get; set; }
        public int TotalChannels { get; set; }
        public int ActiveChannels { get; set; }
        public double ChannelUsagePercentage { get; set; }
    }
}
