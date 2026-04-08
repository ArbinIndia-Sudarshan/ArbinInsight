namespace ArbinInsight.Models.Dashboard
{
    public class DashboardUiResponse
    {
        public string Period { get; set; } = "Weekly";
        public DateTime GeneratedAtUtc { get; set; }
        public List<DashboardMachineDto> Machines { get; set; } = new();
        public DashboardPeriodDataDto DashboardPeriodData { get; set; } = new();
    }

    public class DashboardMachineDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = "Idle";
        public string Tone { get; set; } = "idle";
        public string Operator { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string ChannelsLabel { get; set; } = string.Empty;
        public string CurrentLabel { get; set; } = string.Empty;
        public string VoltageLabel { get; set; } = string.Empty;
        public string CapacityLabel { get; set; } = "Charge Capacity";
        public string CapacityValue { get; set; } = string.Empty;
        public int Percent { get; set; }
        public DashboardMachineOverviewMetricsDto Metrics { get; set; } = new();
        public List<DashboardMachineChannelDto> Channels { get; set; } = new();
    }

    public class DashboardMachineOverviewMetricsDto
    {
        public double UptimeHours { get; set; }
        public double DowntimeHours { get; set; }
        public double RunningHours { get; set; }
        public int BatteriesTested { get; set; }
        public int Passed { get; set; }
        public int Failed { get; set; }
        public int UsagePercent { get; set; }
        public int ChannelsInUse { get; set; }
        public int TotalChannels { get; set; }
    }

    public class DashboardMachineChannelDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string TestName { get; set; } = string.Empty;
        public int Slot { get; set; }
        public string Status { get; set; } = "Idle";
        public string Result { get; set; } = "In Progress";
        public int Progress { get; set; }
        public double Voltage { get; set; }
        public double Current { get; set; }
        public double CapacityAh { get; set; }
        public double EnergyWh { get; set; }
        public int DurationMinutes { get; set; }
        public DashboardChannelTestDetailsDto Details { get; set; } = new();
    }

    public class DashboardChannelTestDetailsDto
    {
        public string BatteryId { get; set; } = string.Empty;
        public string Chemistry { get; set; } = string.Empty;
        public int CycleCount { get; set; }
        public double TemperatureC { get; set; }
        public double InternalResistanceMOhm { get; set; }
        public int StateOfHealth { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class DashboardPeriodDataDto
    {
        public List<DashboardSummaryStatDto> SummaryStats { get; set; } = new();
        public List<DashboardMetricCardDto> MetricCards { get; set; } = new();
        public List<DashboardTrendSeriesDto> TrendSeries { get; set; } = new();
    }

    public class DashboardSummaryStatDto
    {
        public int Value { get; set; }
        public string Label { get; set; } = string.Empty;
        public string Color { get; set; } = "blue";
    }

    public class DashboardMetricCardDto
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Tone { get; set; } = "slate";
    }

    public class DashboardTrendSeriesDto
    {
        public string Label { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Fill { get; set; } = string.Empty;
        public List<int> Values { get; set; } = new();
    }
}
