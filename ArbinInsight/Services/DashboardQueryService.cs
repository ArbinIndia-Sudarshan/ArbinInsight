using ArbinInsight.Data;
using ArbinInsight.Models.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace ArbinInsight.Services
{
    public class DashboardQueryService : IDashboardQueryService
    {
        private readonly ApplicationDbContext _dbContext;

        public DashboardQueryService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default)
        {
            var machineCards = await BuildMachineCardsAsync(cancellationToken);

            return new DashboardSummaryResponse
            {
                GeneratedAtUtc = DateTime.UtcNow,
                TotalMachines = machineCards.Count,
                RunningMachines = machineCards.Count(x => x.Status == "Running"),
                IdleMachines = machineCards.Count(x => x.Status == "Idle"),
                ErrorMachines = machineCards.Count(x => x.Status == "Error"),
                OfflineMachines = machineCards.Count(x => x.Status == "Offline"),
                TotalPass = machineCards.Sum(x => x.PassCount),
                TotalFail = machineCards.Sum(x => x.FailCount)
            };
        }

        public async Task<IReadOnlyList<DashboardMachineCard>> GetMachinesAsync(CancellationToken cancellationToken = default)
        {
            return await BuildMachineCardsAsync(cancellationToken);
        }

        public async Task<DashboardReportSummaryResponse> GetReportSummaryAsync(
            DateTime? fromUtc,
            DateTime? toUtc,
            string? machineCode,
            CancellationToken cancellationToken = default)
        {
            var query = BuildReportQuery(fromUtc, toUtc, machineCode);
            var tests = await query.ToListAsync(cancellationToken);

            return new DashboardReportSummaryResponse
            {
                GeneratedAtUtc = DateTime.UtcNow,
                FromUtc = fromUtc,
                ToUtc = toUtc,
                TotalTests = tests.Count,
                PassedTests = tests.Count(x => IsMatch(x.Result, "Pass")),
                FailedTests = tests.Count(x => IsMatch(x.Result, "Fail") || IsMatch(x.Result, "Error")),
                RunningTests = tests.Count(IsRunning),
                DistinctMachines = tests.Select(x => x.PublisherNodeId).Distinct().Count(),
                DistinctBarcodes = tests.Select(x => x.Barcode).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Count()
            };
        }

        public async Task<IReadOnlyList<DashboardTestReportItem>> GetTestReportsAsync(
            DateTime? fromUtc,
            DateTime? toUtc,
            string? machineCode,
            string? result,
            CancellationToken cancellationToken = default)
        {
            var query = BuildReportQuery(fromUtc, toUtc, machineCode);
            if (!string.IsNullOrWhiteSpace(result))
            {
                query = query.Where(x => x.Result != null && x.Result.ToLower() == result.ToLower());
            }

            var tests = await query
                .OrderByDescending(x => x.Start_Date_Time ?? x.End_Date_Time ?? 0)
                .ToListAsync(cancellationToken);

            var publisherLookup = await _dbContext.PublisherNodes
                .AsNoTracking()
                .ToDictionaryAsync(x => x.PublisherNodeId, cancellationToken);

            var subTestCounts = await _dbContext.Tests
                .AsNoTracking()
                .Where(x => x.PublisherNodeId != null)
                .GroupBy(x => new { x.PublisherNodeId, x.ChannelId })
                .Select(x => new { DashboardTestRunId = x.Key, Count = x.Count() })
                .ToListAsync(cancellationToken);

            var failedLimitCounts = await _dbContext.Limits
                .AsNoTracking()
                .Where(x => x.TestId != null && (x.Result == "Fail" || x.Result == "Error"))
                .GroupBy(x => x.TestId!.Value)
                .Select(x => new { DashboardTestRunId = x.Key, Count = x.Count() })
                .ToDictionaryAsync(x => x.DashboardTestRunId, x => x.Count, cancellationToken);

            var channelLookup = await _dbContext.Channels
                .AsNoTracking()
                .ToDictionaryAsync(x => x.Id, cancellationToken);

            return tests.Select(x =>
            {
                var publisherNodeId = x.PublisherNodeId ?? Guid.Empty;
                publisherLookup.TryGetValue(publisherNodeId, out var node);
                var channelId = x.ChannelId ?? 0;
                var subTestCount = subTestCounts.FirstOrDefault(y => y.DashboardTestRunId.PublisherNodeId == x.PublisherNodeId && y.DashboardTestRunId.ChannelId == x.ChannelId)?.Count ?? 0;
                var failedLimitCount = 0;
                if (channelId != 0)
                {
                    var relatedTestIds = channelLookup.TryGetValue(channelId, out _) 
                        ? _dbContext.Tests.Where(t => t.ChannelId == channelId).Select(t => t.Id).ToList()
                        : new List<int>();
                    failedLimitCount = relatedTestIds.Sum(id => failedLimitCounts.TryGetValue(id, out var count) ? count : 0);
                }

                return new DashboardTestReportItem
                {
                    MachineCode = node?.NodeCode ?? "Unknown",
                    MachineName = node?.MachineName ?? "Unknown",
                    SourceConnectionName = node?.NodeCode ?? "Unknown",
                    SourceTestKey = x.SourceLocalId?.ToString() ?? x.Test_ID.ToString(),
                    TestName = x.Test_Name,
                    Barcode = x.Barcode,
                    Result = x.Result,
                    ChannelIndex = x.Channel_Index,
                    BinNumber = x.BIN_Number,
                    TestProfileName = x.TestProfile_Name,
                    StartDateTimeUtc = ToUtcDateTime(x.Start_Date_Time),
                    EndDateTimeUtc = ToUtcDateTime(x.End_Date_Time),
                    SubTestCount = subTestCount,
                    FailedLimitCount = failedLimitCount,
                    LastSyncedAtUtc = x.BrokerReceivedAtUtc ?? DateTime.UtcNow
                };
            }).ToList();
        }

        private async Task<List<DashboardMachineCard>> BuildMachineCardsAsync(CancellationToken cancellationToken)
        {
            var publisherNodes = await _dbContext.PublisherNodes
                .AsNoTracking()
                .OrderBy(x => x.NodeCode)
                .ToListAsync(cancellationToken);

            var machineDatas = await _dbContext.MachineDatas
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var testLists = await _dbContext.TestList_Table
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var channels = await _dbContext.Channels
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var cards = new List<DashboardMachineCard>();
            foreach (var publisherNode in publisherNodes)
            {
                var machine = machineDatas.FirstOrDefault(x => x.PublisherNodeId == publisherNode.PublisherNodeId);
                var nodeTests = testLists.Where(x => x.PublisherNodeId == publisherNode.PublisherNodeId).ToList();
                var nodeChannels = channels.Where(x => x.PublisherNodeId == publisherNode.PublisherNodeId).ToList();

                var runningChannels = nodeChannels.Where(x => x.IsRunning || IsMatch(x.Result, "InProgress")).ToList();
                var passCount = nodeTests.Count(x => IsMatch(x.Result, "Pass"));
                var failCount = nodeTests.Count(x => IsMatch(x.Result, "Fail") || IsMatch(x.Result, "Error"));
                var lastUpdateUtc = machine?.LastUpdated ?? publisherNode.LastHeartbeatUtc;

                cards.Add(new DashboardMachineCard
                {
                    PublisherNodeId = publisherNode.PublisherNodeId,
                    MachineCode = publisherNode.NodeCode,
                    MachineName = machine?.MachineName ?? publisherNode.MachineName,
                    Status = machine?.Status ?? ResolveStatus(publisherNode, runningChannels.Count, failCount, lastUpdateUtc),
                    RunningChannels = runningChannels.Select(x => x.ChannelIndex).Distinct().Count(),
                    PassCount = passCount,
                    FailCount = failCount,
                    LastUpdateUtc = lastUpdateUtc
                });
            }

            return cards;
        }

        private IQueryable<Models.TestList> BuildReportQuery(DateTime? fromUtc, DateTime? toUtc, string? machineCode)
        {
            var query = _dbContext.TestList_Table.AsNoTracking().AsQueryable();

            if (fromUtc.HasValue)
            {
                var fromMs = new DateTimeOffset(fromUtc.Value).ToUnixTimeMilliseconds();
                query = query.Where(x => (x.Start_Date_Time ?? 0) >= fromMs);
            }

            if (toUtc.HasValue)
            {
                var toMs = new DateTimeOffset(toUtc.Value).ToUnixTimeMilliseconds();
                query = query.Where(x => (x.End_Date_Time ?? x.Start_Date_Time ?? 0) <= toMs);
            }

            if (!string.IsNullOrWhiteSpace(machineCode))
            {
                var matchingPublisherIds = _dbContext.PublisherNodes
                    .Where(x => x.NodeCode.Contains(machineCode) || x.MachineName.Contains(machineCode))
                    .Select(x => x.PublisherNodeId);

                query = query.Where(x => x.PublisherNodeId.HasValue && matchingPublisherIds.Contains(x.PublisherNodeId.Value));
            }

            return query;
        }

        private static string ResolveStatus(Models.Sync.PublisherNode publisherNode, int runningCount, int failCount, DateTime? lastUpdateUtc)
        {
            if (!publisherNode.IsOnline || lastUpdateUtc == null || lastUpdateUtc < DateTime.UtcNow.AddMinutes(-15))
            {
                return "Offline";
            }

            if (runningCount > 0)
            {
                return "Running";
            }

            if (failCount > 0)
            {
                return "Error";
            }

            return "Idle";
        }

        private static bool IsRunning(Models.TestList testRun)
        {
            return IsMatch(testRun.Result, "Running")
                || IsMatch(testRun.Result, "InProgress")
                || (testRun.End_Date_Time == null && testRun.Start_Date_Time != null);
        }

        private static bool IsMatch(string? value, string expected)
        {
            return string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
        }

        private static DateTime? ToUtcDateTime(long? value)
        {
            if (!value.HasValue)
            {
                return null;
            }

            return DateTimeOffset.FromUnixTimeMilliseconds(value.Value).UtcDateTime;
        }
    }
}
