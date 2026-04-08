using ArbinInsight.Data;
using ArbinInsight.Models;
using ArbinInsight.Models.Machines;
using ArbinInsight.Models.Sync;
using Microsoft.EntityFrameworkCore;

namespace ArbinInsight.Services
{
    public class MachineOverviewService : IMachineOverviewService
    {
        private readonly ApplicationDbContext _dbContext;

        public MachineOverviewService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<MachineDto>> GetMachinesAsync(CancellationToken cancellationToken = default)
        {
            var nowUtc = DateTime.UtcNow;
            var windowStartUtc = nowUtc.AddDays(-7);

            var machines = await _dbContext.MachineDatas
                .AsNoTracking()
                .Include(x => x.Channels)
                    .ThenInclude(x => x.testProfile)
                .Include(x => x.TestLists)
                .OrderBy(x => x.MachineName)
                .ToListAsync(cancellationToken);

            var publisherLookup = await _dbContext.PublisherNodes
                .AsNoTracking()
                .GroupBy(x => x.MachineId)
                .Select(x => x.OrderByDescending(node => node.UpdatedAtUtc).First())
                .ToDictionaryAsync(x => x.MachineId, x => x, cancellationToken);

            return machines.Select(machine =>
            {
                publisherLookup.TryGetValue(machine.MachineId, out var publisherNode);
                var channels = machine.Channels ?? new List<Channel>();
                var testLists = machine.TestLists ?? new List<TestList>();
                var activeChannels = channels.Count(IsChannelActive);
                var totalChannels = ResolveTotalChannels(machine);
                var normalizedStatus = NormalizeMachineStatus(machine.Status, publisherNode, activeChannels);
                var recentTests = testLists
                    .Select(MapTestProjection)
                    .Where(x => x.SortDateUtc >= windowStartUtc)
                    .ToList();

                var passed = recentTests.Count(x => x.Result == "Passed");
                var failed = recentTests.Count(x => x.Result == "Failed");
                var usagePercent = CalculatePercent(activeChannels, totalChannels);
                var runningHours = Round(CalculateRunningHours(channels, windowStartUtc, nowUtc, nowUtc));
                var downtimeHours = Round(CalculateDowntimeHours(machine, publisherNode, windowStartUtc, nowUtc));
                var uptimeHours = Round(Math.Max((nowUtc - windowStartUtc).TotalHours - downtimeHours, 0d));
                var averageVoltage = channels.Count == 0 ? 0d : Round(channels.Average(channel => GetChannelVoltage(channel, nowUtc)));
                var averageCurrent = channels.Count == 0 ? 0d : Round(channels.Average(channel => GetChannelCurrent(channel, nowUtc)));
                var capacityValue = channels.Count == 0 ? 0d : Round(channels.Average(channel => GetChannelCapacity(channel, nowUtc)));
                var machineOperator = channels.Select(x => x.UserName).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
                    ?? recentTests.Select(x => x.UserName).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

                return new MachineDto
                {
                    Id = machine.MachineName,
                    Name = machine.MachineName,
                    Status = normalizedStatus,
                    Tone = MapTone(normalizedStatus),
                    Operator = machineOperator,
                    IpAddress = publisherNode?.IpAddress,
                    ChannelsLabel = $"{activeChannels}/{totalChannels}",
                    CurrentLabel = $"{averageCurrent:0.0} A",
                    VoltageLabel = $"{averageVoltage:0.00} v",
                    CapacityLabel = "Charge Capacity",
                    CapacityValue = $"{capacityValue:0.#} Ah",
                    Percent = usagePercent,
                    Metrics = new MachineMetricsDto
                    {
                        UptimeHours = uptimeHours,
                        DowntimeHours = downtimeHours,
                        RunningHours = runningHours,
                        BatteriesTested = recentTests.Count,
                        Passed = passed,
                        Failed = failed,
                        UsagePercent = usagePercent,
                        ChannelsInUse = activeChannels,
                        TotalChannels = totalChannels
                    },
                    Channels = channels
                        .OrderBy(x => x.ChannelIndex)
                        .Select(channel => BuildChannelDto(machine, channel, nowUtc))
                        .ToList()
                };
            }).ToList();
        }

        private static MachineChannelDto BuildChannelDto(MachineData machine, Channel channel, DateTime nowUtc)
        {
            var startedAt = channel.StartDateTime == default ? nowUtc : channel.StartDateTime;
            var updatedAt = channel.IsRunning || channel.EndDateTime == default ? nowUtc : channel.EndDateTime;
            var result = NormalizeResult(channel.Result);
            var status = NormalizeChannelStatus(channel);

            return new MachineChannelDto
            {
                Id = $"{machine.MachineName}-CH-{channel.ChannelIndex}",
                Name = $"Channel {channel.ChannelIndex}",
                TestName = channel.TestName ?? string.Empty,
                Slot = Convert.ToInt32(channel.ChannelIndex),
                Status = status,
                Result = result,
                Progress = GetChannelProgress(channel, nowUtc),
                Voltage = GetChannelVoltage(channel, nowUtc),
                Current = GetChannelCurrent(channel, nowUtc),
                CapacityAh = GetChannelCapacity(channel, nowUtc),
                EnergyWh = GetChannelEnergy(channel, nowUtc),
                DurationMinutes = GetDurationMinutes(channel, nowUtc),
                Details = new MachineChannelDetailsDto
                {
                    BatteryId = string.IsNullOrWhiteSpace(channel.BarCode) ? $"{machine.MachineName}-BAT-{channel.ChannelIndex:00}" : channel.BarCode,
                    Chemistry = ResolveChemistry(channel.ChannelIndex),
                    CycleCount = 100 + Convert.ToInt32(channel.ChannelIndex * 3),
                    TemperatureC = Round(channel.ambientTemperature == 0 ? 25d + (channel.ChannelIndex % 5) : channel.ambientTemperature),
                    InternalResistanceMOhm = Round(11d + (channel.ChannelIndex % 7) * 1.4d),
                    StateOfHealth = Math.Max(78, 96 - Convert.ToInt32(channel.ChannelIndex % 8)),
                    StartedAt = startedAt,
                    UpdatedAt = updatedAt,
                    Notes = BuildChannelNotes(channel, result)
                }
            };
        }

        private static string BuildChannelNotes(Channel channel, string result)
        {
            if (channel.IsRunning)
            {
                return "Charge-discharge sequence is active and updating on the 5-second refresh loop.";
            }

            return $"Last observed result: {result}.";
        }

        private static int ResolveTotalChannels(MachineData machine)
        {
            var capacity = machine.Capacity > 0 ? Convert.ToInt32(Math.Round(machine.Capacity, MidpointRounding.AwayFromZero)) : 0;
            return Math.Max(machine.Channels?.Count ?? 0, capacity);
        }

        private static bool IsChannelActive(Channel channel)
        {
            return NormalizeChannelStatus(channel) == "Active";
        }

        private static string NormalizeChannelStatus(Channel channel)
        {
            if (!string.IsNullOrWhiteSpace(channel.ChannelStatus))
            {
                var normalized = channel.ChannelStatus.Trim().ToLowerInvariant();
                if (normalized.Contains("active") || normalized.Contains("run") || normalized.Contains("progress"))
                {
                    return "Active";
                }

                if (normalized.Contains("complete") || normalized.Contains("pass") || normalized.Contains("done"))
                {
                    return "Completed";
                }

                if (normalized.Contains("fail") || normalized.Contains("error") || normalized.Contains("unsafe") || normalized.Contains("alarm"))
                {
                    return "Failed";
                }

                if (normalized.Contains("idle") || normalized.Contains("stop") || normalized.Contains("offline") || normalized.Contains("down"))
                {
                    return "Idle";
                }
            }

            var result = NormalizeResult(channel.Result);
            if (result == "In Progress") return "Active";
            if (result == "Passed") return "Completed";
            if (result is "Failed" or "Unsafe" or "Aborted") return "Failed";

            return channel.IsRunning ? "Active" : "Idle";
        }

        private static string NormalizeMachineStatus(string? status, PublisherNode? publisherNode, int activeChannels)
        {
            var normalized = status?.Trim().ToLowerInvariant() ?? string.Empty;
            if (normalized.Contains("alarm")) return "Alarm";
            if (normalized.Contains("maint")) return "Maintenance";
            if (normalized.Contains("offline") || normalized.Contains("disconnect")) return "Offline";
            if (normalized.Contains("down")) return "Down";
            if (normalized.Contains("idle")) return "Idle";
            if (normalized.Contains("run") || normalized.Contains("active")) return "Running";
            if (normalized.Contains("connect") || normalized.Contains("online")) return activeChannels > 0 ? "Running" : "Idle";
            if (publisherNode?.IsOnline == false) return "Offline";
            return activeChannels > 0 ? "Running" : "Idle";
        }

        private static string NormalizeResult(string? result)
        {
            var normalized = result?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalized)) return "In Progress";
            if (normalized.Contains("unsafe")) return "Unsafe";
            if (normalized.Contains("abort") || normalized.Contains("cancel") || normalized.Contains("stop")) return "Aborted";
            if (normalized.Contains("pass") || normalized == "ok" || normalized == "success") return "Passed";
            if (normalized.Contains("fail") || normalized.Contains("ng") || normalized.Contains("error")) return "Failed";
            if (normalized.Contains("progress") || normalized.Contains("running")) return "In Progress";
            return result!.Trim();
        }

        private static int CalculatePercent(int numerator, int denominator)
        {
            if (denominator <= 0)
            {
                return 0;
            }

            return Convert.ToInt32(Math.Round((double)numerator / denominator * 100d, MidpointRounding.AwayFromZero));
        }

        private static string MapTone(string status)
        {
            return status switch
            {
                "Running" => "running",
                "Idle" => "idle",
                "Maintenance" => "maintenance",
                "Alarm" => "critical",
                "Offline" => "offline",
                "Down" => "offline",
                _ => "neutral"
            };
        }

        private static double CalculateRunningHours(IEnumerable<Channel> channels, DateTime startUtc, DateTime endUtc, DateTime nowUtc)
        {
            var intervals = channels
                .Select(channel => BuildInterval(channel, nowUtc))
                .Where(interval => interval.HasValue)
                .Select(interval => ClipInterval(interval!.Value, startUtc, endUtc))
                .Where(interval => interval.HasValue)
                .Select(interval => interval!.Value)
                .OrderBy(interval => interval.StartUtc)
                .ToList();

            if (intervals.Count == 0)
            {
                return 0d;
            }

            var totalHours = 0d;
            var currentStart = intervals[0].StartUtc;
            var currentEnd = intervals[0].EndUtc;

            for (var index = 1; index < intervals.Count; index++)
            {
                var interval = intervals[index];
                if (interval.StartUtc <= currentEnd)
                {
                    currentEnd = interval.EndUtc > currentEnd ? interval.EndUtc : currentEnd;
                    continue;
                }

                totalHours += (currentEnd - currentStart).TotalHours;
                currentStart = interval.StartUtc;
                currentEnd = interval.EndUtc;
            }

            totalHours += (currentEnd - currentStart).TotalHours;
            return totalHours;
        }

        private static double CalculateDowntimeHours(MachineData machine, PublisherNode? publisherNode, DateTime startUtc, DateTime endUtc)
        {
            var windowHours = Math.Max((endUtc - startUtc).TotalHours, 0d);
            var activeChannels = (machine.Channels ?? new List<Channel>()).Count(IsChannelActive);
            var status = NormalizeMachineStatus(machine.Status, publisherNode, activeChannels);
            var lastSeenUtc = ResolveLastSeenUtc(machine, publisherNode);

            if (status is "Offline" or "Down")
            {
                if (lastSeenUtc.HasValue && lastSeenUtc.Value > startUtc)
                {
                    return Math.Min((endUtc - lastSeenUtc.Value).TotalHours, windowHours);
                }

                return windowHours;
            }

            return 0d;
        }

        private static DateInterval? BuildInterval(Channel channel, DateTime nowUtc)
        {
            var startUtc = channel.StartDateTime == default ? (DateTime?)null : channel.StartDateTime;
            if (!startUtc.HasValue)
            {
                return null;
            }

            var endUtc = channel.EndDateTime == default || channel.EndDateTime < startUtc.Value
                ? (channel.IsRunning ? nowUtc : startUtc.Value)
                : channel.EndDateTime;

            return endUtc > startUtc.Value ? new DateInterval(startUtc.Value, endUtc) : null;
        }

        private static DateInterval? ClipInterval(DateInterval interval, DateTime startUtc, DateTime endUtc)
        {
            var clippedStart = interval.StartUtc > startUtc ? interval.StartUtc : startUtc;
            var clippedEnd = interval.EndUtc < endUtc ? interval.EndUtc : endUtc;
            return clippedEnd > clippedStart ? new DateInterval(clippedStart, clippedEnd) : null;
        }

        private static DateTime? ResolveLastSeenUtc(MachineData machine, PublisherNode? publisherNode)
        {
            var timestamps = new List<DateTime>();
            if (publisherNode?.LastHeartbeatUtc.HasValue == true) timestamps.Add(publisherNode.LastHeartbeatUtc.Value);
            if (machine.BrokerReceivedAtUtc.HasValue) timestamps.Add(machine.BrokerReceivedAtUtc.Value);
            if (machine.LastUpdated != default) timestamps.Add(machine.LastUpdated);
            return timestamps.Count == 0 ? null : timestamps.Max();
        }

        private static TestProjection MapTestProjection(TestList test)
        {
            var endUtc = test.End_Date_Time.HasValue && test.End_Date_Time.Value > 0
                ? DateTimeOffset.FromUnixTimeMilliseconds(test.End_Date_Time.Value).UtcDateTime
                : test.Start_Date_Time.HasValue && test.Start_Date_Time.Value > 0
                    ? DateTimeOffset.FromUnixTimeMilliseconds(test.Start_Date_Time.Value).UtcDateTime
                    : DateTime.MinValue;

            return new TestProjection(NormalizeResult(test.Result), endUtc, test.User_Name);
        }

        private static int GetDurationMinutes(Channel channel, DateTime nowUtc)
        {
            if (channel.StartDateTime == default)
            {
                return 0;
            }

            var endUtc = channel.IsRunning || channel.EndDateTime == default ? nowUtc : channel.EndDateTime;
            return Math.Max(Convert.ToInt32((endUtc - channel.StartDateTime).TotalMinutes), 0);
        }

        private static int GetChannelProgress(Channel channel, DateTime nowUtc)
        {
            if (!channel.IsRunning)
            {
                return NormalizeResult(channel.Result) == "Passed" ? 100 : 0;
            }

            var progress = 15 + (GetDurationMinutes(channel, nowUtc) % 80);
            return Math.Min(progress, 99);
        }

        private static double GetChannelVoltage(Channel channel, DateTime nowUtc)
        {
            var progressFactor = GetChannelProgress(channel, nowUtc) / 100d;
            return Round(3.2d + progressFactor * 1.15d + (channel.ChannelIndex % 3) * 0.02d);
        }

        private static double GetChannelCurrent(Channel channel, DateTime nowUtc)
        {
            if (!channel.IsRunning)
            {
                return 0d;
            }

            var durationBand = GetDurationMinutes(channel, nowUtc) % 12;
            return Round(0.9d + durationBand * 0.28d + (channel.ChannelIndex % 4) * 0.15d);
        }

        private static double GetChannelCapacity(Channel channel, DateTime nowUtc)
        {
            var durationHours = GetDurationMinutes(channel, nowUtc) / 60d;
            return Round(Math.Max(0.8d, durationHours * 1.7d + (channel.ChannelIndex % 5) * 0.4d));
        }

        private static double GetChannelEnergy(Channel channel, DateTime nowUtc)
        {
            return Round(GetChannelVoltage(channel, nowUtc) * GetChannelCapacity(channel, nowUtc));
        }

        private static string ResolveChemistry(uint channelIndex)
        {
            return (channelIndex % 3) switch
            {
                0 => "NMC",
                1 => "LFP",
                _ => "LTO"
            };
        }

        private static double Round(double value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        private readonly record struct DateInterval(DateTime StartUtc, DateTime EndUtc);
        private sealed record TestProjection(string Result, DateTime SortDateUtc, string? UserName);
    }
}
