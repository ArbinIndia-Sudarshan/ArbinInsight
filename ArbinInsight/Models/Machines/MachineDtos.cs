namespace ArbinInsight.Models.Machines
{
    public class MachineDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Tone { get; set; } = string.Empty;
        public string? Operator { get; set; }
        public string? IpAddress { get; set; }
        public string ChannelsLabel { get; set; } = string.Empty;
        public string CurrentLabel { get; set; } = string.Empty;
        public string VoltageLabel { get; set; } = string.Empty;
        public string CapacityLabel { get; set; } = "Charge Capacity";
        public string CapacityValue { get; set; } = string.Empty;
        public int Percent { get; set; }
        public MachineMetricsDto Metrics { get; set; } = new();
        public List<MachineChannelDto> Channels { get; set; } = new();
    }

    public class MachineMetricsDto
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

    public class MachineChannelDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string TestName { get; set; } = string.Empty;
        public int Slot { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public int Progress { get; set; }
        public double Voltage { get; set; }
        public double Current { get; set; }
        public double CapacityAh { get; set; }
        public double EnergyWh { get; set; }
        public int DurationMinutes { get; set; }
        public MachineChannelDetailsDto Details { get; set; } = new();
    }

    public class MachineChannelDetailsDto
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
}
