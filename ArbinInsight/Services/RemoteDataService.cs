using ArbinInsight.Data;
using ArbinInsight.Models;
using ArbinInsight.Models.RemoteData;
using ArbinInsight.Models.Sync;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ArbinInsight.Services
{
    public class RemoteDataService : IRemoteDataService
    {
        private static readonly string[] TestIdCandidates = ["Test_ID", "TestId", "Id", "ID"];
        private static readonly string[] SubTestIdCandidates = ["SubTest_List_ID", "SubTestId", "SubTestID", "Id", "ID"];
        private static readonly string[] SubTestParentCandidates = ["Test_ID", "TestId", "TestListId", "ParentTestId"];
        private static readonly string[] LimitParentCandidates = ["SubTest_ID", "SubTestId", "SubTestID", "Test_ID", "TestId"];
        private static readonly string[] ChannelIndexCandidates = ["Channel_Index", "ChannelIndex", "Channel", "Slot"];
        private static readonly string[] ResultCandidates = ["Result", "TestResult", "Status"];
        private static readonly string[] TestNameCandidates = ["Test_Name", "TestName", "Name"];
        private static readonly string[] BarcodeCandidates = ["Barcode", "BarCode", "BatteryId"];
        private static readonly string[] UserNameCandidates = ["User_Name", "UserName", "Operator"];
        private static readonly string[] RetestCandidates = ["Retest", "ReTest"];
        private static readonly string[] StartTimeCandidates = ["Start_Date_Time", "StartTime", "StartedAt", "StartDateTime"];
        private static readonly string[] EndTimeCandidates = ["End_Date_Time", "EndTime", "EndedAt", "EndDateTime"];
        private static readonly string[] BinNumberCandidates = ["BIN_Number", "BinNumber", "Bin"];
        private static readonly string[] TestProjectCandidates = ["TestProjectName", "ProjectName"];
        private static readonly string[] TestProfileCandidates = ["TestProfile_Name", "TestProfileName", "ProfileName"];

        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<RemoteDataService> _logger;

        public RemoteDataService(IConfiguration configuration, ApplicationDbContext dbContext, ILogger<RemoteDataService> logger)
        {
            _configuration = configuration;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<RemoteDataFetchResponse> FetchAllAsync(CancellationToken cancellationToken = default)
        {
            var response = new RemoteDataFetchResponse
            {
                FetchedAtUtc = DateTime.UtcNow
            };

            foreach (var (name, connectionString) in GetRemoteConnections())
            {
                response.Databases.Add(await FetchFromConnectionAsync(name, connectionString, cancellationToken));
            }

            return response;
        }

        private async Task<RemoteDatabaseFetchResult> FetchFromConnectionAsync(string connectionName, string connectionString, CancellationToken cancellationToken)
        {
            var result = new RemoteDatabaseFetchResult
            {
                ConnectionName = connectionName
            };

            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken);

                var testLists = await ReadTableAsync(connection, "TestList_Table", cancellationToken);
                var subTests = await ReadTableAsync(connection, "SubTest_List_table", cancellationToken);
                var limits = await ReadTableAsync(connection, "Limits_table", cancellationToken);

                result.Success = true;
                result.TestListCount = testLists.Count;
                result.SubTestCount = subTests.Count;
                result.LimitCount = limits.Count;
                result.Tests = BuildHierarchy(testLists, subTests, limits);
                await SyncIntoLocalDatabaseAsync(connectionName, connectionString, testLists, cancellationToken);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
                await MarkConnectionOfflineAsync(connectionName, cancellationToken);
                _logger.LogWarning(ex, "Remote sync failed for {ConnectionName}.", connectionName);
            }

            return result;
        }

        private async Task SyncIntoLocalDatabaseAsync(
            string connectionName,
            string connectionString,
            IReadOnlyList<RemoteDatabaseRow> testListRows,
            CancellationToken cancellationToken)
        {
            var nowUtc = DateTime.UtcNow;
            var machineId = BuildStableMachineId(connectionName);
            var machineName = connectionName;
            var sourceHost = TryGetDataSource(connectionString);

            var publisherNode = await _dbContext.PublisherNodes
                .FirstOrDefaultAsync(x => x.NodeCode == connectionName, cancellationToken);

            if (publisherNode == null)
            {
                publisherNode = new PublisherNode
                {
                    PublisherNodeId = Guid.NewGuid(),
                    NodeCode = connectionName,
                    CreatedAtUtc = nowUtc
                };
                _dbContext.PublisherNodes.Add(publisherNode);
            }

            publisherNode.MachineId = machineId;
            publisherNode.MachineName = machineName;
            publisherNode.HostName = sourceHost;
            publisherNode.IpAddress = ExtractIpAddress(sourceHost);
            publisherNode.IsOnline = true;
            publisherNode.LastHeartbeatUtc = nowUtc;
            publisherNode.UpdatedAtUtc = nowUtc;

            var machine = await _dbContext.MachineDatas
                .Include(x => x.Channels)
                    .ThenInclude(x => x.testProfile)
                .Include(x => x.TestLists)
                .FirstOrDefaultAsync(x => x.PublisherNodeId == publisherNode.PublisherNodeId && x.MachineId == machineId, cancellationToken);

            if (machine == null)
            {
                machine = new MachineData
                {
                    PublisherNodeId = publisherNode.PublisherNodeId,
                    MachineId = machineId,
                    MachineName = machineName,
                    Channels = new List<Channel>(),
                    TestLists = new List<TestList>()
                };
                _dbContext.MachineDatas.Add(machine);
            }

            machine.MachineName = machineName;
            machine.BrokerReceivedAtUtc = nowUtc;
            machine.LastUpdated = nowUtc;
            machine.Channels ??= new List<Channel>();
            machine.TestLists ??= new List<TestList>();

            var remoteTests = BuildRemoteTests(testListRows);
            var remoteBySourceId = remoteTests.ToDictionary(x => x.SourceLocalId);

            var existingTests = machine.TestLists
                .Where(x => x.SourceLocalId.HasValue)
                .ToDictionary(x => x.SourceLocalId!.Value);

            foreach (var remote in remoteTests)
            {
                if (!existingTests.TryGetValue(remote.SourceLocalId, out var test))
                {
                    test = new TestList
                    {
                        PublisherNodeId = publisherNode.PublisherNodeId,
                        SourceLocalId = remote.SourceLocalId,
                        MachineData = machine
                    };
                    machine.TestLists.Add(test);
                    existingTests[remote.SourceLocalId] = test;
                }

                ApplyRemoteTest(test, remote, nowUtc);
            }

            var staleTests = machine.TestLists
                .Where(x => x.SourceLocalId.HasValue && !remoteBySourceId.ContainsKey(x.SourceLocalId.Value))
                .ToList();

            if (staleTests.Count > 0)
            {
                _dbContext.TestList_Table.RemoveRange(staleTests);
            }

            var latestByChannel = remoteTests
                .Where(x => x.ChannelIndex.HasValue)
                .GroupBy(x => x.ChannelIndex!.Value)
                .Select(group => group.OrderByDescending(x => x.SortTicks).First())
                .ToList();

            var profileByChannel = machine.Channels
                .Where(x => x.SourceLocalId.HasValue && x.testProfile != null)
                .ToDictionary(x => x.SourceLocalId!.Value, x => x.testProfile);

            var channelByIndex = machine.Channels.ToDictionary(x => x.ChannelIndex);
            foreach (var latest in latestByChannel)
            {
                var channelIndex = (uint)latest.ChannelIndex!.Value;

                if (!channelByIndex.TryGetValue(channelIndex, out var channel))
                {
                    channel = new Channel
                    {
                        MachineData = machine,
                        ChannelIndex = channelIndex,
                        cANMessagePairList = new List<CANMessagePair>(),
                        sMBMessagePairList = new List<SMBMessagePair>()
                    };
                    machine.Channels.Add(channel);
                    channelByIndex[channelIndex] = channel;
                }

                if (!profileByChannel.TryGetValue(latest.ChannelIndex.Value, out var profile))
                {
                    profile = new TestProfile
                    {
                        PublisherNodeId = publisherNode.PublisherNodeId,
                        SourceLocalId = latest.ChannelIndex,
                        TestProfileName = string.IsNullOrWhiteSpace(latest.TestProfileName) ? $"Profile-{machineName}-CH{channelIndex}" : latest.TestProfileName!,
                        TestObjectName = string.IsNullOrWhiteSpace(latest.TestProjectName) ? $"Battery Test {channelIndex}" : latest.TestProjectName!,
                        Tests = new List<Test>(),
                        UDSRequestID = string.Empty,
                        UDSResponseID = string.Empty,
                        CreatedDateTime = nowUtc,
                        ModifiedDateTime = nowUtc
                    };
                    _dbContext.TestProfiles.Add(profile);
                    profileByChannel[latest.ChannelIndex.Value] = profile;
                }
                else
                {
                    profile.TestProfileName = string.IsNullOrWhiteSpace(latest.TestProfileName) ? profile.TestProfileName : latest.TestProfileName!;
                    profile.TestObjectName = string.IsNullOrWhiteSpace(latest.TestProjectName) ? profile.TestObjectName : latest.TestProjectName!;
                    profile.ModifiedDateTime = nowUtc;
                }

                channel.PublisherNodeId = publisherNode.PublisherNodeId;
                channel.SourceLocalId = latest.ChannelIndex;
                channel.BrokerReceivedAtUtc = nowUtc;
                channel.TestID = latest.SourceLocalId;
                channel.BarCode = latest.Barcode ?? string.Empty;
                channel.TestName = latest.TestName ?? string.Empty;
                channel.Result = latest.Result ?? "In Progress";
                channel.ChannelStatus = MapChannelStatus(latest.Result);
                channel.IsRunning = IsRunningResult(latest.Result);
                channel.Retest = ParseBool(latest.Retest);
                channel.RetestNumber = latest.Retest ?? string.Empty;
                channel.UserName = latest.UserName ?? string.Empty;
                channel.StartDateTime = latest.StartAtUtc ?? default;
                channel.EndDateTime = latest.EndAtUtc ?? default;
                channel.ambientTemperature = 0f;
                channel.ManuallyStopFlag = false;
                channel.StopTestsExecuted = false;
                channel.BINNumber = latest.BinNumber ?? string.Empty;
                channel.testProfile = profile;
            }

            foreach (var test in machine.TestLists)
            {
                if (!test.Channel_Index.HasValue)
                {
                    test.ChannelId = null;
                    test.TestProfileId = null;
                    continue;
                }

                var key = (uint)test.Channel_Index.Value;
                if (channelByIndex.TryGetValue(key, out var channel))
                {
                    test.Channel = channel;
                    test.TestProfile = channel.testProfile;
                }
            }

            machine.Capacity = Math.Max(machine.Channels.Count, machine.Channels.Select(x => (int)x.ChannelIndex).DefaultIfEmpty(0).Max());
            machine.Status = machine.Channels.Any(x => x.IsRunning) ? "Running" : "Idle";

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task MarkConnectionOfflineAsync(string connectionName, CancellationToken cancellationToken)
        {
            var publisherNode = await _dbContext.PublisherNodes
                .FirstOrDefaultAsync(x => x.NodeCode == connectionName, cancellationToken);

            if (publisherNode == null)
            {
                return;
            }

            publisherNode.IsOnline = false;
            publisherNode.UpdatedAtUtc = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        private static List<RemoteTestRow> BuildRemoteTests(IReadOnlyList<RemoteDatabaseRow> rows)
        {
            var tests = new List<RemoteTestRow>();

            foreach (var row in rows)
            {
                var sourceLocalId = GetInt(row, TestIdCandidates);
                if (!sourceLocalId.HasValue)
                {
                    continue;
                }

                var startAtUtc = GetDateTime(row, StartTimeCandidates);
                var endAtUtc = GetDateTime(row, EndTimeCandidates);
                var sortTicks = (endAtUtc ?? startAtUtc ?? DateTime.MinValue).Ticks;

                tests.Add(new RemoteTestRow
                {
                    SourceLocalId = sourceLocalId.Value,
                    TestName = GetString(row, TestNameCandidates),
                    Barcode = GetString(row, BarcodeCandidates),
                    Result = GetString(row, ResultCandidates),
                    Retest = GetString(row, RetestCandidates),
                    StartAtUtc = startAtUtc,
                    EndAtUtc = endAtUtc,
                    UserName = GetString(row, UserNameCandidates),
                    ChannelIndex = GetInt(row, ChannelIndexCandidates),
                    BinNumber = GetString(row, BinNumberCandidates),
                    TestProjectName = GetString(row, TestProjectCandidates),
                    TestProfileName = GetString(row, TestProfileCandidates),
                    SortTicks = sortTicks
                });
            }

            return tests;
        }

        private static void ApplyRemoteTest(TestList target, RemoteTestRow source, DateTime nowUtc)
        {
            target.BrokerReceivedAtUtc = nowUtc;
            target.Test_Name = source.TestName;
            target.Barcode = source.Barcode;
            target.Result = source.Result;
            target.Retest = source.Retest;
            target.Start_Date_Time = ToUnixMilliseconds(source.StartAtUtc);
            target.End_Date_Time = ToUnixMilliseconds(source.EndAtUtc);
            target.User_Name = source.UserName;
            target.Channel_Index = source.ChannelIndex;
            target.BIN_Number = source.BinNumber;
            target.TestProjectName = source.TestProjectName;
            target.TestProfile_Name = source.TestProfileName;
        }

        private static int BuildStableMachineId(string value)
        {
            unchecked
            {
                var hash = 2166136261u;
                foreach (var ch in value)
                {
                    hash ^= ch;
                    hash *= 16777619u;
                }

                return (int)(hash & 0x7FFFFFFF);
            }
        }

        private static string? TryGetDataSource(string connectionString)
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString);
                return string.IsNullOrWhiteSpace(builder.DataSource) ? null : builder.DataSource;
            }
            catch
            {
                return null;
            }
        }

        private static string? ExtractIpAddress(string? dataSource)
        {
            if (string.IsNullOrWhiteSpace(dataSource))
            {
                return null;
            }

            var hostPart = dataSource.Split(',', 2, StringSplitOptions.TrimEntries)[0];
            return string.IsNullOrWhiteSpace(hostPart) ? null : hostPart;
        }

        private static string MapChannelStatus(string? result)
        {
            var normalized = result?.Trim().ToLowerInvariant() ?? string.Empty;
            if (normalized.Contains("progress") || normalized.Contains("running")) return "Running";
            if (normalized.Contains("pass") || normalized == "ok" || normalized == "success") return "Completed";
            if (normalized.Contains("fail") || normalized.Contains("unsafe") || normalized.Contains("error")) return "Failed";
            return "Idle";
        }

        private static bool IsRunningResult(string? result)
        {
            var normalized = result?.Trim().ToLowerInvariant() ?? string.Empty;
            return normalized.Contains("progress") || normalized.Contains("running");
        }

        private static bool ParseBool(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (bool.TryParse(value, out var boolValue))
            {
                return boolValue;
            }

            if (int.TryParse(value, out var intValue))
            {
                return intValue != 0;
            }

            return value.Equals("yes", StringComparison.OrdinalIgnoreCase);
        }

        private static int? GetInt(RemoteDatabaseRow row, IEnumerable<string> candidates)
        {
            var value = GetValue(row, candidates);
            if (value == null) return null;

            if (value is int i) return i;
            if (value is long l && l <= int.MaxValue && l >= int.MinValue) return (int)l;
            if (value is short s) return s;
            if (value is uint ui && ui <= int.MaxValue) return (int)ui;

            var text = Convert.ToString(value);
            return int.TryParse(text, out var parsed) ? parsed : null;
        }

        private static string? GetString(RemoteDatabaseRow row, IEnumerable<string> candidates)
        {
            var value = GetValue(row, candidates);
            if (value == null) return null;
            var text = Convert.ToString(value);
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }

        private static DateTime? GetDateTime(RemoteDatabaseRow row, IEnumerable<string> candidates)
        {
            var value = GetValue(row, candidates);
            if (value == null) return null;

            if (value is DateTime dateTime)
            {
                return dateTime.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
                    : dateTime.ToUniversalTime();
            }

            if (value is DateTimeOffset dateTimeOffset)
            {
                return dateTimeOffset.UtcDateTime;
            }

            if (value is long l)
            {
                return FromUnix(l);
            }

            if (value is int i)
            {
                return FromUnix(i);
            }

            if (long.TryParse(Convert.ToString(value), out var parsed))
            {
                return FromUnix(parsed);
            }

            return DateTime.TryParse(Convert.ToString(value), out var parsedDateTime)
                ? DateTime.SpecifyKind(parsedDateTime, DateTimeKind.Utc)
                : null;
        }

        private static object? GetValue(RemoteDatabaseRow row, IEnumerable<string> candidates)
        {
            foreach (var candidate in candidates)
            {
                if (row.Values.TryGetValue(candidate, out var value))
                {
                    return value;
                }

                var matchingKey = row.Values.Keys.FirstOrDefault(x => x.Equals(candidate, StringComparison.OrdinalIgnoreCase));
                if (matchingKey != null)
                {
                    return row.Values[matchingKey];
                }
            }

            return null;
        }

        private static DateTime? FromUnix(long value)
        {
            if (value <= 0)
            {
                return null;
            }

            // Arbin exports are usually milliseconds; fall back to seconds for smaller values.
            return value > 10_000_000_000
                ? DateTimeOffset.FromUnixTimeMilliseconds(value).UtcDateTime
                : DateTimeOffset.FromUnixTimeSeconds(value).UtcDateTime;
        }

        private static long? ToUnixMilliseconds(DateTime? value)
        {
            if (!value.HasValue || value.Value == default)
            {
                return null;
            }

            var utc = value.Value.Kind == DateTimeKind.Utc
                ? value.Value
                : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
            return new DateTimeOffset(utc).ToUnixTimeMilliseconds();
        }

        private static async Task<List<RemoteDatabaseRow>> ReadTableAsync(SqlConnection connection, string tableName, CancellationToken cancellationToken)
        {
            var rows = new List<RemoteDatabaseRow>();
            var commandText = $"SELECT * FROM [{tableName}]";

            await using var command = new SqlCommand(commandText, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new RemoteDatabaseRow();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    row.Values[reader.GetName(i)] = NormalizeValue(reader.GetValue(i));
                }
                rows.Add(row);
            }

            return rows;
        }

        private static object? NormalizeValue(object value)
        {
            if (value == DBNull.Value)
            {
                return null;
            }

            return value switch
            {
                DateTime dateTime => dateTime,
                DateTimeOffset dateTimeOffset => dateTimeOffset,
                byte[] bytes => Convert.ToBase64String(bytes),
                _ => value
            };
        }

        private static List<RemoteTestHierarchy> BuildHierarchy(
            IReadOnlyList<RemoteDatabaseRow> testLists,
            IReadOnlyList<RemoteDatabaseRow> subTests,
            IReadOnlyList<RemoteDatabaseRow> limits)
        {
            var testKey = FindExistingColumn(testLists, TestIdCandidates);
            var subTestKey = FindExistingColumn(subTests, SubTestIdCandidates);
            var subTestParentKey = FindExistingColumn(subTests, SubTestParentCandidates);
            var limitParentKey = FindExistingColumn(limits, LimitParentCandidates);

            var subTestsByParent = string.IsNullOrWhiteSpace(subTestParentKey)
                ? new Dictionary<string, List<RemoteDatabaseRow>>(StringComparer.OrdinalIgnoreCase)
                : subTests
                    .Where(x => TryGetKey(x, subTestParentKey!, out _))
                    .GroupBy(x => GetKey(x, subTestParentKey!))
                    .ToDictionary(x => x.Key, x => x.ToList(), StringComparer.OrdinalIgnoreCase);

            var limitsByParent = string.IsNullOrWhiteSpace(limitParentKey)
                ? new Dictionary<string, List<RemoteDatabaseRow>>(StringComparer.OrdinalIgnoreCase)
                : limits
                    .Where(x => TryGetKey(x, limitParentKey!, out _))
                    .GroupBy(x => GetKey(x, limitParentKey!))
                    .ToDictionary(x => x.Key, x => x.ToList(), StringComparer.OrdinalIgnoreCase);

            var hierarchies = new List<RemoteTestHierarchy>();

            foreach (var testList in testLists)
            {
                var hierarchy = new RemoteTestHierarchy
                {
                    TestList = testList
                };

                if (!string.IsNullOrWhiteSpace(testKey) && TryGetKey(testList, testKey!, out var parentKey) && subTestsByParent.TryGetValue(parentKey, out var relatedSubTests))
                {
                    foreach (var subTest in relatedSubTests)
                    {
                        var subHierarchy = new RemoteSubTestHierarchy
                        {
                            SubTest = subTest
                        };

                        if (!string.IsNullOrWhiteSpace(subTestKey) && TryGetKey(subTest, subTestKey!, out var subKey) && limitsByParent.TryGetValue(subKey, out var relatedLimits))
                        {
                            subHierarchy.Limits.AddRange(relatedLimits);
                        }

                        hierarchy.SubTests.Add(subHierarchy);
                    }
                }

                if (hierarchy.SubTests.Count == 0 && !string.IsNullOrWhiteSpace(testKey) && TryGetKey(testList, testKey!, out var testListKey) && limitsByParent.TryGetValue(testListKey, out var directLimits))
                {
                    hierarchy.UnmatchedLimits.AddRange(directLimits);
                }

                hierarchies.Add(hierarchy);
            }

            return hierarchies;
        }

        private static string? FindExistingColumn(IEnumerable<RemoteDatabaseRow> rows, IEnumerable<string> candidates)
        {
            var first = rows.FirstOrDefault();
            if (first == null)
            {
                return null;
            }

            foreach (var candidate in candidates)
            {
                var actual = first.Values.Keys.FirstOrDefault(x => x.Equals(candidate, StringComparison.OrdinalIgnoreCase));
                if (actual != null)
                {
                    return actual;
                }
            }

            return null;
        }

        private static bool TryGetKey(RemoteDatabaseRow row, string columnName, out string key)
        {
            key = string.Empty;

            if (!row.Values.TryGetValue(columnName, out var value) || value == null)
            {
                return false;
            }

            key = Convert.ToString(value) ?? string.Empty;
            return !string.IsNullOrWhiteSpace(key);
        }

        private static string GetKey(RemoteDatabaseRow row, string columnName)
        {
            return Convert.ToString(row.Values[columnName]) ?? string.Empty;
        }

        private IEnumerable<(string Name, string ConnectionString)> GetRemoteConnections()
        {
            var section = _configuration.GetSection("ConnectionStrings");
            return section.GetChildren()
                .Where(x => x.Key.StartsWith("RemoteConnection", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(x.Value))
                .Select(x => (x.Key, x.Value!))
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase);
        }

        private sealed class RemoteTestRow
        {
            public int SourceLocalId { get; set; }
            public string? TestName { get; set; }
            public string? Barcode { get; set; }
            public string? Result { get; set; }
            public string? Retest { get; set; }
            public DateTime? StartAtUtc { get; set; }
            public DateTime? EndAtUtc { get; set; }
            public string? UserName { get; set; }
            public int? ChannelIndex { get; set; }
            public string? BinNumber { get; set; }
            public string? TestProjectName { get; set; }
            public string? TestProfileName { get; set; }
            public long SortTicks { get; set; }
        }
    }
}
