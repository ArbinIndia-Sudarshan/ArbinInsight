using ArbinInsight.Models.Dashboard;
using ArbinInsight.Models.Machines;
using MachineOverviewChannelDto = ArbinInsight.Models.Machines.MachineChannelDto;

namespace ArbinInsight.Services
{
    public class DashboardUiService : IDashboardUiService
    {
        private readonly IMachineOverviewService _machineOverviewService;
        private readonly IDashboardService _dashboardService;

        public DashboardUiService(IMachineOverviewService machineOverviewService, IDashboardService dashboardService)
        {
            _machineOverviewService = machineOverviewService;
            _dashboardService = dashboardService;
        }

        public async Task<DashboardUiResponse> GetDashboardAsync(DashboardTimeFilter timeFilter, CancellationToken cancellationToken = default)
        {
            var machines = await _machineOverviewService.GetMachinesAsync(cancellationToken);
            var networkDashboard = await _dashboardService.GetNetworkDashboardAsync(timeFilter, cancellationToken);

            return new DashboardUiResponse
            {
                Period = timeFilter.ToString(),
                GeneratedAtUtc = networkDashboard.GeneratedAtUtc,
                Machines = machines.Select(MapMachine).ToList(),
                DashboardPeriodData = new DashboardPeriodDataDto
                {
                    SummaryStats =
                    [
                        new DashboardSummaryStatDto
                        {
                            Value = networkDashboard.TotalBatteriesTested,
                            Label = "Batteries Tested",
                            Color = "blue"
                        },
                        new DashboardSummaryStatDto
                        {
                            Value = networkDashboard.PassedCount,
                            Label = "Passed",
                            Color = "green"
                        },
                        new DashboardSummaryStatDto
                        {
                            Value = networkDashboard.FailedCount + networkDashboard.UnsafeCount + networkDashboard.AbortedCount,
                            Label = "Failed",
                            Color = "red"
                        }
                    ],
                    MetricCards =
                    [
                        new DashboardMetricCardDto
                        {
                            Title = "Machines Online",
                            Value = networkDashboard.MachineStatuses
                                .Where(x => x.Status is "Running" or "Idle")
                                .Sum(x => x.Count)
                                .ToString(),
                            Tone = "green"
                        },
                        new DashboardMetricCardDto
                        {
                            Title = "Channel Utilization",
                            Value = $"{networkDashboard.ChannelCapacity.ActiveChannels}/{networkDashboard.ChannelCapacity.TotalChannels}",
                            Tone = "blue"
                        },
                        new DashboardMetricCardDto
                        {
                            Title = "Uptime",
                            Value = $"{networkDashboard.Availability.UptimeHours:0.##} h",
                            Tone = "green"
                        },
                        new DashboardMetricCardDto
                        {
                            Title = "Downtime",
                            Value = $"{networkDashboard.Availability.DowntimeHours:0.##} h",
                            Tone = "red"
                        },
                        new DashboardMetricCardDto
                        {
                            Title = "Running Hours",
                            Value = $"{networkDashboard.Availability.RunningHours:0.##} h",
                            Tone = "orange"
                        }
                    ],
                    TrendSeries =
                    [
                        new DashboardTrendSeriesDto
                        {
                            Label = "Batteries Tested",
                            Color = "#2563eb",
                            Fill = "rgba(37, 99, 235, 0.18)",
                            Values = networkDashboard.BatteriesTrend.Select(x => x.Total).ToList()
                        },
                        new DashboardTrendSeriesDto
                        {
                            Label = "Passed",
                            Color = "#16a34a",
                            Fill = "rgba(22, 163, 74, 0.18)",
                            Values = networkDashboard.ResultTrend.Select(x => x.Passed).ToList()
                        },
                        new DashboardTrendSeriesDto
                        {
                            Label = "Failed",
                            Color = "#dc2626",
                            Fill = "rgba(220, 38, 38, 0.18)",
                            Values = networkDashboard.ResultTrend.Select(x => x.Failed + x.Unsafe + x.Aborted).ToList()
                        }
                    ]
                }
            };
        }

        private static DashboardMachineDto MapMachine(MachineDto machine)
        {
            return new DashboardMachineDto
            {
                Id = machine.Id,
                Name = machine.Name,
                Status = NormalizeMachineStatus(machine.Status),
                Tone = MapMachineTone(machine.Status),
                Operator = machine.Operator ?? string.Empty,
                IpAddress = machine.IpAddress ?? string.Empty,
                ChannelsLabel = machine.ChannelsLabel,
                CurrentLabel = machine.CurrentLabel,
                VoltageLabel = machine.VoltageLabel,
                CapacityLabel = machine.CapacityLabel,
                CapacityValue = machine.CapacityValue,
                Percent = machine.Percent,
                Metrics = new DashboardMachineOverviewMetricsDto
                {
                    UptimeHours = machine.Metrics.UptimeHours,
                    DowntimeHours = machine.Metrics.DowntimeHours,
                    RunningHours = machine.Metrics.RunningHours,
                    BatteriesTested = machine.Metrics.BatteriesTested,
                    Passed = machine.Metrics.Passed,
                    Failed = machine.Metrics.Failed,
                    UsagePercent = machine.Metrics.UsagePercent,
                    ChannelsInUse = machine.Metrics.ChannelsInUse,
                    TotalChannels = machine.Metrics.TotalChannels
                },
                Channels = machine.Channels.Select(MapChannel).ToList()
            };
        }

        private static DashboardMachineChannelDto MapChannel(MachineOverviewChannelDto channel)
        {
            return new DashboardMachineChannelDto
            {
                Id = channel.Id,
                Name = channel.Name,
                TestName = channel.TestName,
                Slot = channel.Slot,
                Status = NormalizeChannelStatus(channel.Status, channel.Result),
                Result = NormalizeTestResult(channel.Result),
                Progress = channel.Progress,
                Voltage = channel.Voltage,
                Current = channel.Current,
                CapacityAh = channel.CapacityAh,
                EnergyWh = channel.EnergyWh,
                DurationMinutes = channel.DurationMinutes,
                Details = new DashboardChannelTestDetailsDto
                {
                    BatteryId = channel.Details.BatteryId,
                    Chemistry = channel.Details.Chemistry,
                    CycleCount = channel.Details.CycleCount,
                    TemperatureC = channel.Details.TemperatureC,
                    InternalResistanceMOhm = channel.Details.InternalResistanceMOhm,
                    StateOfHealth = channel.Details.StateOfHealth,
                    StartedAt = channel.Details.StartedAt,
                    UpdatedAt = channel.Details.UpdatedAt,
                    Notes = channel.Details.Notes
                }
            };
        }

        private static string NormalizeMachineStatus(string status)
        {
            return status switch
            {
                "Running" => "Running",
                "Idle" => "Idle",
                "Offline" => "Offline",
                "Down" => "Down",
                "Maintenance" => "Down",
                "Alarm" => "Down",
                _ => "Idle"
            };
        }

        private static string MapMachineTone(string status)
        {
            return NormalizeMachineStatus(status) switch
            {
                "Running" => "running",
                "Idle" => "idle",
                "Offline" => "offline",
                "Down" => "down",
                _ => "idle"
            };
        }

        private static string NormalizeChannelStatus(string status, string result)
        {
            if (status.Equals("Active", StringComparison.OrdinalIgnoreCase) || result.Equals("In Progress", StringComparison.OrdinalIgnoreCase))
            {
                return "Active";
            }

            if (result.Equals("Passed", StringComparison.OrdinalIgnoreCase))
            {
                return "Completed";
            }

            if (result.Equals("Failed", StringComparison.OrdinalIgnoreCase) || result.Equals("Unsafe", StringComparison.OrdinalIgnoreCase))
            {
                return "Failed";
            }

            return "Idle";
        }

        private static string NormalizeTestResult(string result)
        {
            return result switch
            {
                "Passed" => "Passed",
                "Failed" => "Failed",
                "Unsafe" => "Unsafe",
                "In Progress" => "In Progress",
                "Aborted" => "Failed",
                _ => "In Progress"
            };
        }
    }
}
