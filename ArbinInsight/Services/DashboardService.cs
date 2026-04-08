using ArbinInsight.Data;
using ArbinInsight.Models;
using ArbinInsight.Models.Dashboard;
using ArbinInsight.Models.Sync;
using Microsoft.EntityFrameworkCore;

namespace ArbinInsight.Services
{
    public class DashboardService : IDashboardService
    {
        private static readonly string[] OrderedStatuses = ["Running", "Idle", "Down", "Offline", "Maintenance", "Alarm"];
        private static readonly string[] OrderedResults = ["Passed", "Failed", "Unsafe", "In Progress", "Aborted"];

        private readonly ApplicationDbContext _dbContext;

        public DashboardService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<NetworkDashboardResponse> GetNetworkDashboardAsync(DashboardTimeFilter timeFilter, CancellationToken cancellationToken = default)
        {
            var nowUtc = DateTime.UtcNow;
            var window = ResolveWindow(timeFilter, nowUtc);
            var projections = await BuildMachineProjectionsAsync(window, nowUtc, cancellationToken);
            var filteredTests = projections.SelectMany(x => x.FilteredTests).ToList();

            return new NetworkDashboardResponse
            {
                TimeFilter = timeFilter,
                GeneratedAtUtc = nowUtc,
                RangeStartUtc = window.StartUtc,
                RangeEndUtc = window.EndUtc,
                TotalMachines = projections.Count,
                TotalBatteriesTested = filteredTests.Count,
                PassedCount = filteredTests.Count(x => x.NormalizedResult == "Passed"),
                FailedCount = filteredTests.Count(x => x.NormalizedResult == "Failed"),
                UnsafeCount = filteredTests.Count(x => x.NormalizedResult == "Unsafe"),
                InProgressCount = filteredTests.Count(x => x.NormalizedResult == "In Progress"),
                AbortedCount = filteredTests.Count(x => x.NormalizedResult == "Aborted"),
                MachineStatuses = OrderedStatuses.Select(status => new StatusMetricDto
                {
                    Status = status,
                    Count = projections.Count(x => x.NormalizedStatus == status)
                }).ToList(),
                BatteryResults = OrderedResults.Select(result => new ResultMetricDto
                {
                    Result = result,
                    Count = filteredTests.Count(x => x.NormalizedResult == result)
                }).ToList(),
                ChannelCapacity = BuildChannelCapacity(projections),
                Availability = new AvailabilitySummaryDto
                {
                    UptimeHours = Round(projections.Sum(x => x.UptimeHours)),
                    DowntimeHours = Round(projections.Sum(x => x.DowntimeHours)),
                    RunningHours = Round(projections.Sum(x => x.RunningHours))
                },
                BatteriesTrend = BuildTrend(window, timeFilter, projections, (point, _, tests) => point.Total += tests.Count),
                ResultTrend = BuildTrend(window, timeFilter, projections, (point, _, tests) =>
                {
                    point.Total += tests.Count;
                    point.Passed += tests.Count(x => x.NormalizedResult == "Passed");
                    point.Failed += tests.Count(x => x.NormalizedResult == "Failed");
                    point.Unsafe += tests.Count(x => x.NormalizedResult == "Unsafe");
                    point.InProgress += tests.Count(x => x.NormalizedResult == "In Progress");
                    point.Aborted += tests.Count(x => x.NormalizedResult == "Aborted");
                }),
                UptimeDowntimeTrend = BuildTrend(window, timeFilter, projections, (point, machine, _) =>
                {
                    point.UptimeHours += machine.GetBucketUptimeHours(point.BucketStartUtc, GetBucketEnd(point.BucketStartUtc, timeFilter, window.EndUtc));
                    point.DowntimeHours += machine.GetBucketDowntimeHours(point.BucketStartUtc, GetBucketEnd(point.BucketStartUtc, timeFilter, window.EndUtc));
                }, point =>
                {
                    point.UptimeHours = Round(point.UptimeHours);
                    point.DowntimeHours = Round(point.DowntimeHours);
                }),
                ChannelUtilizationTrend = BuildTrend(window, timeFilter, projections, (point, machine, _) =>
                {
                    point.TotalChannels += machine.TotalChannels;
                    point.ActiveChannels += machine.ActiveChannels;
                }, point => point.ChannelUsagePercentage = CalculatePercentage(point.ActiveChannels, point.TotalChannels)),
                Machines = projections.Select(BuildMachineListItem).OrderBy(x => x.MachineName).ToList()
            };
        }

        public async Task<MachineDashboardResponse?> GetMachineDashboardAsync(int machineId, DashboardTimeFilter timeFilter, CancellationToken cancellationToken = default)
        {
            var nowUtc = DateTime.UtcNow;
            var window = ResolveWindow(timeFilter, nowUtc);
            var machine = (await BuildMachineProjectionsAsync(window, nowUtc, cancellationToken))
                .FirstOrDefault(x => x.MachineId == machineId);

            if (machine == null)
            {
                return null;
            }

            return new MachineDashboardResponse
            {
                TimeFilter = timeFilter,
                GeneratedAtUtc = nowUtc,
                RangeStartUtc = window.StartUtc,
                RangeEndUtc = window.EndUtc,
                MachineId = machine.MachineId,
                MachineName = machine.MachineName,
                HostName = machine.PublisherNode?.HostName,
                IpAddress = machine.PublisherNode?.IpAddress,
                Username = machine.Username,
                Status = machine.NormalizedStatus,
                RawStatus = machine.RawStatus,
                SoftwareVersion = null,
                LastSeenUtc = machine.LastSeenUtc,
                UptimeHours = machine.UptimeHours,
                DowntimeHours = machine.DowntimeHours,
                RunningHours = machine.RunningHours,
                TotalBatteriesTested = machine.FilteredTests.Count,
                PassedCount = machine.FilteredTests.Count(x => x.NormalizedResult == "Passed"),
                FailedCount = machine.FilteredTests.Count(x => x.NormalizedResult == "Failed"),
                UnsafeCount = machine.FilteredTests.Count(x => x.NormalizedResult == "Unsafe"),
                InProgressCount = machine.FilteredTests.Count(x => x.NormalizedResult == "In Progress"),
                AbortedCount = machine.FilteredTests.Count(x => x.NormalizedResult == "Aborted"),
                ChannelCapacity = new ChannelCapacityDto
                {
                    TotalChannels = machine.TotalChannels,
                    ActiveChannels = machine.ActiveChannels,
                    FreeChannels = Math.Max(machine.TotalChannels - machine.ActiveChannels, 0),
                    UsagePercentage = machine.ChannelUsagePercentage
                },
                Channels = (machine.Machine.Channels ?? new List<Channel>()).OrderBy(x => x.ChannelIndex).Select(channel => new MachineChannelDto
                {
                    ChannelId = channel.Id,
                    ChannelIndex = channel.ChannelIndex,
                    Status = NormalizeChannelStatus(channel),
                    IsRunning = IsChannelActive(channel),
                    Result = NormalizeResult(channel.Result),
                    Barcode = NullIfWhiteSpace(channel.BarCode),
                    TestName = NullIfWhiteSpace(channel.TestName),
                    Username = NullIfWhiteSpace(channel.UserName),
                    TestProfileName = channel.testProfile?.TestProfileName,
                    TestObjectName = channel.testProfile?.TestObjectName,
                    AmbientTemperature = channel.ambientTemperature,
                    StartDateTimeUtc = NormalizeDateTime(channel.StartDateTime),
                    EndDateTimeUtc = NormalizeDateTime(channel.EndDateTime),
                    ManuallyStopFlag = channel.ManuallyStopFlag,
                    StopTestsExecuted = channel.StopTestsExecuted,
                    BinNumber = NullIfWhiteSpace(channel.BINNumber)
                }).ToList(),
                RecentTests = machine.FilteredTests.OrderByDescending(x => x.SortDateUtc ?? DateTime.MinValue).Take(10).Select(test => new RecentTestDto
                {
                    TestId = test.Test.Test_ID,
                    ChannelIndex = test.Test.Channel_Index,
                    TestName = test.Test.Test_Name,
                    Barcode = test.Test.Barcode,
                    Result = test.NormalizedResult,
                    Username = test.Test.User_Name,
                    StartedAtUtc = test.StartUtc,
                    EndedAtUtc = test.EndUtc,
                    TestProjectName = test.Test.TestProjectName,
                    TestProfileName = test.Test.TestProfile_Name,
                    BinNumber = test.Test.BIN_Number
                }).ToList(),
                RecentEvents = BuildRecentEvents(machine),
                BatteriesTrend = BuildTrend(window, timeFilter, [machine], (point, _, tests) => point.Total += tests.Count),
                ResultTrend = BuildTrend(window, timeFilter, [machine], (point, _, tests) =>
                {
                    point.Total += tests.Count;
                    point.Passed += tests.Count(x => x.NormalizedResult == "Passed");
                    point.Failed += tests.Count(x => x.NormalizedResult == "Failed");
                    point.Unsafe += tests.Count(x => x.NormalizedResult == "Unsafe");
                    point.InProgress += tests.Count(x => x.NormalizedResult == "In Progress");
                    point.Aborted += tests.Count(x => x.NormalizedResult == "Aborted");
                }),
                UptimeDowntimeTrend = BuildTrend(window, timeFilter, [machine], (point, currentMachine, _) =>
                {
                    point.UptimeHours += currentMachine.GetBucketUptimeHours(point.BucketStartUtc, GetBucketEnd(point.BucketStartUtc, timeFilter, window.EndUtc));
                    point.DowntimeHours += currentMachine.GetBucketDowntimeHours(point.BucketStartUtc, GetBucketEnd(point.BucketStartUtc, timeFilter, window.EndUtc));
                }, point =>
                {
                    point.UptimeHours = Round(point.UptimeHours);
                    point.DowntimeHours = Round(point.DowntimeHours);
                }),
                ChannelUtilizationTrend = BuildTrend(window, timeFilter, [machine], (point, currentMachine, _) =>
                {
                    point.TotalChannels += currentMachine.TotalChannels;
                    point.ActiveChannels += currentMachine.ActiveChannels;
                }, point => point.ChannelUsagePercentage = CalculatePercentage(point.ActiveChannels, point.TotalChannels))
            };
        }

        private async Task<List<MachineProjection>> BuildMachineProjectionsAsync(TimeWindow window, DateTime nowUtc, CancellationToken cancellationToken)
        {
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

            var windowHours = Math.Max((window.EndUtc - window.StartUtc).TotalHours, 0d);

            return machines.Select(machine =>
            {
                publisherLookup.TryGetValue(machine.MachineId, out var publisherNode);
                var filteredTests = (machine.TestLists ?? new List<TestList>())
                    .Select(test => MapFilteredTest(test))
                    .Where(test => test.SortDateUtc.HasValue && test.SortDateUtc.Value >= window.StartUtc && test.SortDateUtc.Value <= window.EndUtc)
                    .OrderByDescending(test => test.SortDateUtc)
                    .ToList();

                var activeChannels = (machine.Channels ?? new List<Channel>()).Count(IsChannelActive);
                var totalChannels = ResolveTotalChannels(machine);
                var runningHours = Round(CalculateRunningHours(machine.Channels ?? new List<Channel>(), window.StartUtc, window.EndUtc, nowUtc));
                var downtimeHours = Round(CalculateDowntimeHours(machine, publisherNode, window, nowUtc));
                var uptimeHours = Round(Math.Max(windowHours - downtimeHours, 0d));
                var lastSeenUtc = ResolveLastSeenUtc(machine, publisherNode);

                return new MachineProjection(
                    machine,
                    publisherNode,
                    filteredTests,
                    ResolveMachineUsername(machine, filteredTests),
                    NormalizeMachineStatus(machine.Status, publisherNode, activeChannels),
                    machine.Status,
                    totalChannels,
                    activeChannels,
                    CalculatePercentage(activeChannels, totalChannels),
                    runningHours,
                    uptimeHours,
                    downtimeHours,
                    lastSeenUtc);
            }).ToList();
        }

        private static FilteredTestProjection MapFilteredTest(TestList test)
        {
            var startUtc = FromUnixMilliseconds(test.Start_Date_Time);
            var endUtc = FromUnixMilliseconds(test.End_Date_Time);
            return new FilteredTestProjection(test, NormalizeResult(test.Result), startUtc, endUtc, endUtc ?? startUtc);
        }

        private static List<RecentEventDto> BuildRecentEvents(MachineProjection machine)
        {
            var events = new List<RecentEventDto>();
            if (machine.LastSeenUtc.HasValue)
            {
                events.Add(new RecentEventDto { EventType = "LastSeen", Description = $"{machine.MachineName} last heartbeat observed.", OccurredAtUtc = machine.LastSeenUtc.Value });
            }
            if (machine.Machine.LastUpdated != default)
            {
                events.Add(new RecentEventDto { EventType = "MachineUpdate", Description = $"{machine.MachineName} machine data updated.", OccurredAtUtc = machine.Machine.LastUpdated });
            }
            var latestTest = machine.FilteredTests.OrderByDescending(x => x.SortDateUtc ?? DateTime.MinValue).FirstOrDefault();
            if (latestTest?.SortDateUtc.HasValue == true)
            {
                events.Add(new RecentEventDto { EventType = "RecentTest", Description = $"Latest test finished with {latestTest.NormalizedResult}.", OccurredAtUtc = latestTest.SortDateUtc.Value });
            }
            return events.OrderByDescending(x => x.OccurredAtUtc).Take(10).ToList();
        }

        private static ChannelCapacityDto BuildChannelCapacity(IEnumerable<MachineProjection> projections)
        {
            var totalChannels = projections.Sum(x => x.TotalChannels);
            var activeChannels = projections.Sum(x => x.ActiveChannels);
            return new ChannelCapacityDto
            {
                TotalChannels = totalChannels,
                ActiveChannels = activeChannels,
                FreeChannels = Math.Max(totalChannels - activeChannels, 0),
                UsagePercentage = CalculatePercentage(activeChannels, totalChannels)
            };
        }

        private static MachineDashboardListItemDto BuildMachineListItem(MachineProjection projection)
        {
            return new MachineDashboardListItemDto
            {
                MachineId = projection.MachineId,
                MachineName = projection.MachineName,
                IpAddress = projection.PublisherNode?.IpAddress,
                Username = projection.Username,
                Status = projection.NormalizedStatus,
                TotalChannels = projection.TotalChannels,
                ActiveChannels = projection.ActiveChannels,
                ChannelUsagePercentage = projection.ChannelUsagePercentage,
                BatteriesTested = projection.FilteredTests.Count,
                PassedCount = projection.FilteredTests.Count(x => x.NormalizedResult == "Passed"),
                FailedCount = projection.FilteredTests.Count(x => x.NormalizedResult == "Failed"),
                UnsafeCount = projection.FilteredTests.Count(x => x.NormalizedResult == "Unsafe"),
                UptimeHours = projection.UptimeHours,
                DowntimeHours = projection.DowntimeHours,
                RunningHours = projection.RunningHours,
                LastSeenUtc = projection.LastSeenUtc
            };
        }

        private static List<TrendPointDto> BuildTrend(
            TimeWindow window,
            DashboardTimeFilter timeFilter,
            IReadOnlyCollection<MachineProjection> machines,
            Action<TrendPointDto, MachineProjection, List<FilteredTestProjection>> aggregate,
            Action<TrendPointDto>? finalize = null)
        {
            var points = new List<TrendPointDto>();
            foreach (var bucketStart in EnumerateBucketStarts(window, timeFilter))
            {
                var point = new TrendPointDto { BucketStartUtc = bucketStart, Label = FormatBucketLabel(bucketStart, timeFilter) };
                foreach (var machine in machines)
                {
                    var bucketEnd = GetBucketEnd(bucketStart, timeFilter, window.EndUtc);
                    var tests = machine.FilteredTests
                        .Where(test => test.SortDateUtc.HasValue && test.SortDateUtc.Value >= bucketStart && test.SortDateUtc.Value < bucketEnd)
                        .ToList();
                    aggregate(point, machine, tests);
                    point.Running += machine.NormalizedStatus == "Running" ? 1 : 0;
                    point.Idle += machine.NormalizedStatus == "Idle" ? 1 : 0;
                    point.Down += machine.NormalizedStatus == "Down" ? 1 : 0;
                    point.Offline += machine.NormalizedStatus == "Offline" ? 1 : 0;
                    point.Maintenance += machine.NormalizedStatus == "Maintenance" ? 1 : 0;
                    point.Alarm += machine.NormalizedStatus == "Alarm" ? 1 : 0;
                }
                finalize?.Invoke(point);
                points.Add(point);
            }
            return points;
        }

        private static IEnumerable<DateTime> EnumerateBucketStarts(TimeWindow window, DashboardTimeFilter timeFilter)
        {
            var cursor = window.StartUtc;
            while (cursor < window.EndUtc)
            {
                yield return cursor;
                cursor = timeFilter switch
                {
                    DashboardTimeFilter.Daily => cursor.AddHours(1),
                    DashboardTimeFilter.Weekly => cursor.AddDays(1),
                    DashboardTimeFilter.Monthly => cursor.AddDays(7),
                    DashboardTimeFilter.Yearly => cursor.AddMonths(1),
                    _ => cursor.AddDays(1)
                };
            }
        }

        private static DateTime GetBucketEnd(DateTime bucketStart, DashboardTimeFilter timeFilter, DateTime endUtc)
        {
            var next = timeFilter switch
            {
                DashboardTimeFilter.Daily => bucketStart.AddHours(1),
                DashboardTimeFilter.Weekly => bucketStart.AddDays(1),
                DashboardTimeFilter.Monthly => bucketStart.AddDays(7),
                DashboardTimeFilter.Yearly => bucketStart.AddMonths(1),
                _ => bucketStart.AddDays(1)
            };
            return next <= endUtc ? next : endUtc;
        }

        private static string FormatBucketLabel(DateTime bucketStart, DashboardTimeFilter timeFilter)
        {
            return timeFilter switch
            {
                DashboardTimeFilter.Daily => bucketStart.ToString("HH:mm"),
                DashboardTimeFilter.Weekly => bucketStart.ToString("ddd"),
                DashboardTimeFilter.Monthly => bucketStart.ToString("dd MMM"),
                DashboardTimeFilter.Yearly => bucketStart.ToString("MMM yyyy"),
                _ => bucketStart.ToString("u")
            };
        }

        private static TimeWindow ResolveWindow(DashboardTimeFilter timeFilter, DateTime nowUtc)
        {
            return timeFilter switch
            {
                DashboardTimeFilter.Daily => new TimeWindow(nowUtc.AddDays(-1), nowUtc),
                DashboardTimeFilter.Weekly => new TimeWindow(nowUtc.AddDays(-7), nowUtc),
                DashboardTimeFilter.Monthly => new TimeWindow(nowUtc.AddMonths(-1), nowUtc),
                DashboardTimeFilter.Yearly => new TimeWindow(nowUtc.AddYears(-1), nowUtc),
                _ => new TimeWindow(nowUtc.AddDays(-7), nowUtc)
            };
        }

        private static int ResolveTotalChannels(MachineData machine)
        {
            var capacity = machine.Capacity > 0 ? Convert.ToInt32(Math.Round(machine.Capacity, MidpointRounding.AwayFromZero)) : 0;
            return Math.Max(machine.Channels?.Count ?? 0, capacity);
        }

        private static bool IsChannelActive(Channel channel)
        {
            return channel.IsRunning || NormalizeChannelStatus(channel).Contains("run", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeChannelStatus(Channel channel)
        {
            if (!string.IsNullOrWhiteSpace(channel.ChannelStatus))
            {
                return channel.ChannelStatus.Trim();
            }
            return channel.IsRunning ? "Running" : "Idle";
        }

        private static string NormalizeMachineStatus(string? status, PublisherNode? publisherNode, int activeChannels)
        {
            var normalized = status?.Trim().ToLowerInvariant() ?? string.Empty;
            if (normalized.Contains("alarm")) return "Alarm";
            if (normalized.Contains("maint")) return "Maintenance";
            if (normalized.Contains("offline") || normalized.Contains("disconnect")) return "Offline";
            if (normalized.Contains("down")) return "Down";
            if (normalized.Contains("idle")) return "Idle";
            if (normalized.Contains("run") || normalized.Contains("connect") || normalized.Contains("online")) return activeChannels > 0 ? "Running" : "Idle";
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

        private static string? ResolveMachineUsername(MachineData machine, IReadOnlyCollection<FilteredTestProjection> filteredTests)
        {
            var testUser = filteredTests.Select(x => x.Test.User_Name).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
            if (!string.IsNullOrWhiteSpace(testUser)) return testUser;
            return (machine.Channels ?? new List<Channel>()).Select(x => x.UserName).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
        }

        private static DateTime? ResolveLastSeenUtc(MachineData machine, PublisherNode? publisherNode)
        {
            var timestamps = new List<DateTime>();
            if (publisherNode?.LastHeartbeatUtc.HasValue == true) timestamps.Add(publisherNode.LastHeartbeatUtc.Value);
            if (machine.BrokerReceivedAtUtc.HasValue) timestamps.Add(machine.BrokerReceivedAtUtc.Value);
            if (machine.LastUpdated != default) timestamps.Add(machine.LastUpdated);
            return timestamps.Count == 0 ? null : timestamps.Max();
        }

        private static double CalculateRunningHours(IEnumerable<Channel> channels, DateTime startUtc, DateTime endUtc, DateTime nowUtc)
        {
            var intervals = channels
                .Select(channel => BuildInterval(channel, nowUtc))
                .Where(interval => interval.HasValue)
                .Select(interval =>
                {
                    var value = interval.GetValueOrDefault();
                    return ClipInterval(value, startUtc, endUtc);
                })
                .Where(interval => interval.HasValue)
                .Select(interval => interval.GetValueOrDefault())
                .OrderBy(interval => interval.StartUtc)
                .ToList();

            if (intervals.Count == 0) return 0d;

            var totalHours = 0d;
            var currentStart = intervals[0].StartUtc;
            var currentEnd = intervals[0].EndUtc;
            for (var index = 1; index < intervals.Count; index++)
            {
                var interval = intervals[index];
                if (interval.StartUtc <= currentEnd)
                {
                    if (interval.EndUtc > currentEnd) currentEnd = interval.EndUtc;
                    continue;
                }
                totalHours += (currentEnd - currentStart).TotalHours;
                currentStart = interval.StartUtc;
                currentEnd = interval.EndUtc;
            }
            totalHours += (currentEnd - currentStart).TotalHours;
            return totalHours;
        }

        private static DateInterval? BuildInterval(Channel channel, DateTime nowUtc)
        {
            var startUtc = NormalizeDateTime(channel.StartDateTime);
            if (!startUtc.HasValue) return null;

            var endUtc = NormalizeDateTime(channel.EndDateTime);
            if (!endUtc.HasValue || endUtc < startUtc) endUtc = channel.IsRunning ? nowUtc : startUtc;
            return endUtc > startUtc ? new DateInterval(startUtc.Value, endUtc.Value) : null;
        }

        private static DateInterval? ClipInterval(DateInterval interval, DateTime startUtc, DateTime endUtc)
        {
            var clippedStart = interval.StartUtc > startUtc ? interval.StartUtc : startUtc;
            var clippedEnd = interval.EndUtc < endUtc ? interval.EndUtc : endUtc;
            return clippedEnd > clippedStart ? new DateInterval(clippedStart, clippedEnd) : null;
        }

        private static double CalculateDowntimeHours(MachineData machine, PublisherNode? publisherNode, TimeWindow window, DateTime nowUtc)
        {
            var windowHours = Math.Max((window.EndUtc - window.StartUtc).TotalHours, 0d);
            var status = NormalizeMachineStatus(machine.Status, publisherNode, (machine.Channels ?? new List<Channel>()).Count(IsChannelActive));
            if (status is "Offline" or "Down")
            {
                var lastSeenUtc = ResolveLastSeenUtc(machine, publisherNode);
                if (lastSeenUtc.HasValue && lastSeenUtc.Value > window.StartUtc)
                {
                    return Math.Min(Math.Max((window.EndUtc - lastSeenUtc.Value).TotalHours, 0d), windowHours);
                }
                return windowHours;
            }
            if (status is "Maintenance" or "Alarm")
            {
                return Math.Min(windowHours, Math.Max(windowHours - CalculateRunningHours(machine.Channels ?? new List<Channel>(), window.StartUtc, window.EndUtc, nowUtc), 0d));
            }
            return 0d;
        }

        private static DateTime? NormalizeDateTime(DateTime value)
        {
            return value == default ? null : value;
        }

        private static DateTime? FromUnixMilliseconds(long? milliseconds)
        {
            return milliseconds.HasValue && milliseconds.Value > 0
                ? DateTimeOffset.FromUnixTimeMilliseconds(milliseconds.Value).UtcDateTime
                : null;
        }

        private static double CalculatePercentage(int numerator, int denominator)
        {
            return denominator <= 0 ? 0d : Round((double)numerator / denominator * 100d);
        }

        private static double Round(double value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        private static string? NullIfWhiteSpace(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private readonly record struct TimeWindow(DateTime StartUtc, DateTime EndUtc);
        private readonly record struct DateInterval(DateTime StartUtc, DateTime EndUtc);
        private sealed record FilteredTestProjection(TestList Test, string NormalizedResult, DateTime? StartUtc, DateTime? EndUtc, DateTime? SortDateUtc);
        private sealed record MachineProjection(
            MachineData Machine,
            PublisherNode? PublisherNode,
            List<FilteredTestProjection> FilteredTests,
            string? Username,
            string NormalizedStatus,
            string RawStatus,
            int TotalChannels,
            int ActiveChannels,
            double ChannelUsagePercentage,
            double RunningHours,
            double UptimeHours,
            double DowntimeHours,
            DateTime? LastSeenUtc)
        {
            public int MachineId => Machine.MachineId;
            public string MachineName => Machine.MachineName;

            public double GetBucketUptimeHours(DateTime bucketStartUtc, DateTime bucketEndUtc)
            {
                var bucketHours = Math.Max((bucketEndUtc - bucketStartUtc).TotalHours, 0d);
                if (bucketHours == 0d) return 0d;
                if (NormalizedStatus is "Offline" or "Down")
                {
                    if (LastSeenUtc.HasValue && LastSeenUtc.Value > bucketStartUtc)
                    {
                        return Math.Min(Math.Max((LastSeenUtc.Value - bucketStartUtc).TotalHours, 0d), bucketHours);
                    }
                    return 0d;
                }
                if (NormalizedStatus is "Maintenance" or "Alarm")
                {
                    return Math.Min(RunningHours, bucketHours);
                }
                return bucketHours;
            }

            public double GetBucketDowntimeHours(DateTime bucketStartUtc, DateTime bucketEndUtc)
            {
                var bucketHours = Math.Max((bucketEndUtc - bucketStartUtc).TotalHours, 0d);
                return Math.Max(bucketHours - GetBucketUptimeHours(bucketStartUtc, bucketEndUtc), 0d);
            }
        }
    }
}
