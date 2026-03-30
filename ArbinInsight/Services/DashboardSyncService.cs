using System.Text.Json;
using ArbinInsight.Data;
using ArbinInsight.Models.Dashboard;
using ArbinInsight.Models;
using ArbinInsight.Models.RemoteData;
using ArbinInsight.Models.Sync;
using Microsoft.EntityFrameworkCore;

namespace ArbinInsight.Services
{
    public class DashboardSyncService : IDashboardSyncService
    {
        private static readonly string[] TestIdCandidates = ["Test_ID", "TestId", "Id", "ID"];
        private static readonly string[] SubTestIdCandidates = ["SubTest_List_ID", "SubTest_ID", "SubTestId", "SubTestID", "Id", "ID"];
        private static readonly string[] LimitIdCandidates = ["Limit_ID", "LimitId", "LimitID", "Id", "ID"];
        private static readonly string[] ChannelIndexCandidates = ["Channel_Index", "ChannelIndex"];
        private static readonly string[] ResultCandidates = ["Result", "Test_Result"];
        private static readonly string[] TestNameCandidates = ["Test_Name", "TestName"];
        private static readonly string[] BarcodeCandidates = ["Barcode", "BarCode"];
        private static readonly string[] RetestCandidates = ["Retest"];
        private static readonly string[] StartCandidates = ["Start_Date_Time", "StartDateTime"];
        private static readonly string[] EndCandidates = ["End_Date_Time", "EndDateTime"];
        private static readonly string[] UserCandidates = ["User_Name", "UserName"];
        private static readonly string[] BinCandidates = ["BIN_Number", "BINNumber", "BinNumber"];
        private static readonly string[] ProjectCandidates = ["TestProjectName", "Test_Object_Name", "TestObjectName"];
        private static readonly string[] ProfileCandidates = ["TestProfile_Name", "TestProfileName"];
        private static readonly string[] ScheduleCandidates = ["ScheduleName", "Schedule_Name"];
        private static readonly string[] StatusCandidates = ["TestStatus", "Test_Status", "Status"];
        private static readonly string[] EnableCandidates = ["Enable"];
        private static readonly string[] StopOnFailCandidates = ["StopOnFail"];
        private static readonly string[] LimitNameCandidates = ["LimitName", "Limit_Name"];
        private static readonly string[] MinCandidates = ["MinValue", "Min_Value"];
        private static readonly string[] MaxCandidates = ["MaxValue", "Max_Value"];
        private static readonly string[] MeasuredCandidates = ["MeasuredValue", "Measured_Value"];
        private static readonly string[] UnitCandidates = ["Unit"];
        private static readonly string[] ToleranceCandidates = ["Tolerance"];

        private readonly ApplicationDbContext _dbContext;
        private readonly IRemoteDataService _remoteDataService;
        private readonly IRemoteDataPublisher _remoteDataPublisher;

        public DashboardSyncService(
            ApplicationDbContext dbContext,
            IRemoteDataService remoteDataService,
            IRemoteDataPublisher remoteDataPublisher)
        {
            _dbContext = dbContext;
            _remoteDataService = remoteDataService;
            _remoteDataPublisher = remoteDataPublisher;
        }

        public async Task<DashboardSyncResponse> SyncAsync(bool publishToQueue, CancellationToken cancellationToken = default)
        {
            var fetched = await _remoteDataService.FetchAllAsync(cancellationToken);
            var syncedAtUtc = DateTime.UtcNow;
            var response = new DashboardSyncResponse
            {
                SyncedAtUtc = syncedAtUtc,
                RemoteData = fetched
            };

            foreach (var database in fetched.Databases.Where(x => x.Success))
            {
                var ingestResult = await IngestRemoteDatabaseAsync(database, Guid.NewGuid(), "DashboardSync", "dashboard.sync", cancellationToken);
                response.SavedTests += ingestResult.SavedTests;
                response.SavedSubTests += ingestResult.SavedSubTests;
                response.SavedLimits += ingestResult.SavedLimits;
                response.SynchronizedConnections.AddRange(ingestResult.SynchronizedConnections);
            }

            if (publishToQueue)
            {
                var published = await _remoteDataPublisher.PublishAsync(fetched, cancellationToken);
                response.PublishedMessageCount = published.PublishedMessageCount;
            }

            return response;
        }

        public async Task<DashboardSyncResponse> IngestRemoteDatabaseAsync(
            RemoteDatabaseFetchResult database,
            Guid? messageId = null,
            string messageType = "RemoteDatabaseFetchResult",
            string routingKey = "remote.testdata",
            CancellationToken cancellationToken = default)
        {
            var syncedAtUtc = DateTime.UtcNow;
            var resolvedMessageId = messageId ?? Guid.NewGuid();
            var response = new DashboardSyncResponse
            {
                SyncedAtUtc = syncedAtUtc,
                RemoteData = new RemoteDataFetchResponse
                {
                    FetchedAtUtc = syncedAtUtc,
                    Databases = new List<RemoteDatabaseFetchResult> { database }
                }
            };

            if (!database.Success)
            {
                return response;
            }

            if (await _dbContext.InboxMessages.AnyAsync(x => x.MessageId == resolvedMessageId, cancellationToken))
            {
                response.SynchronizedConnections.Add(database.ConnectionName);
                return response;
            }

            var publisherNode = await UpsertPublisherNodeAsync(database.ConnectionName, syncedAtUtc, cancellationToken);
            await SaveInboxMessageAsync(publisherNode.PublisherNodeId, resolvedMessageId, database, messageType, routingKey, cancellationToken);

            var machineData = await UpsertMachineDataAsync(publisherNode, database, resolvedMessageId, syncedAtUtc, cancellationToken);

            foreach (var test in database.Tests)
            {
                var testProfile = await UpsertTestProfileAsync(
                    publisherNode.PublisherNodeId,
                    test,
                    resolvedMessageId,
                    syncedAtUtc,
                    cancellationToken);

                var channel = await UpsertChannelAsync(
                    publisherNode.PublisherNodeId,
                    machineData,
                    testProfile,
                    test,
                    resolvedMessageId,
                    syncedAtUtc,
                    cancellationToken);

                await UpsertTestListAsync(
                    publisherNode.PublisherNodeId,
                    machineData,
                    channel,
                    testProfile,
                    test,
                    resolvedMessageId,
                    syncedAtUtc,
                    cancellationToken);

                response.SavedTests++;

                foreach (var subTest in test.SubTests)
                {
                    var savedSubTest = await UpsertSubTestAsync(
                        publisherNode.PublisherNodeId,
                        channel,
                        testProfile,
                        test,
                        subTest,
                        resolvedMessageId,
                        syncedAtUtc,
                        cancellationToken);
                    response.SavedSubTests++;

                    foreach (var limit in subTest.Limits)
                    {
                        await UpsertLimitAsync(
                            publisherNode.PublisherNodeId,
                            savedSubTest,
                            test,
                            subTest,
                            limit,
                            resolvedMessageId,
                            syncedAtUtc,
                            cancellationToken);
                        response.SavedLimits++;
                    }
                }

                foreach (var limit in test.UnmatchedLimits)
                {
                    await UpsertLimitAsync(
                        publisherNode.PublisherNodeId,
                        null,
                        test,
                        null,
                        limit,
                        resolvedMessageId,
                        syncedAtUtc,
                        cancellationToken);
                    response.SavedLimits++;
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            response.SynchronizedConnections.Add(database.ConnectionName);
            return response;
        }

        private async Task<PublisherNode> UpsertPublisherNodeAsync(string connectionName, DateTime syncedAtUtc, CancellationToken cancellationToken)
        {
            var publisherNode = await _dbContext.PublisherNodes
                .SingleOrDefaultAsync(x => x.NodeCode == connectionName, cancellationToken);

            if (publisherNode == null)
            {
                publisherNode = new PublisherNode
                {
                    PublisherNodeId = Guid.NewGuid(),
                    NodeCode = connectionName,
                    MachineId = ExtractMachineId(connectionName),
                    MachineName = connectionName,
                    IsOnline = true,
                    LastHeartbeatUtc = syncedAtUtc,
                    UpdatedAtUtc = syncedAtUtc
                };

                _dbContext.PublisherNodes.Add(publisherNode);
                return publisherNode;
            }

            publisherNode.IsOnline = true;
            publisherNode.LastHeartbeatUtc = syncedAtUtc;
            publisherNode.UpdatedAtUtc = syncedAtUtc;
            publisherNode.MachineName = connectionName;
            if (publisherNode.MachineId == 0)
            {
                publisherNode.MachineId = ExtractMachineId(connectionName);
            }

            return publisherNode;
        }

        private Task SaveInboxMessageAsync(
            Guid publisherNodeId,
            Guid messageId,
            RemoteDatabaseFetchResult database,
            string messageType,
            string routingKey,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _dbContext.InboxMessages.Add(new InboxMessage
            {
                MessageId = messageId,
                PublisherNodeId = publisherNodeId,
                MessageType = messageType,
                RoutingKey = routingKey,
                PayloadJson = JsonSerializer.Serialize(database),
                ReceivedAtUtc = DateTime.UtcNow,
                ProcessedAtUtc = DateTime.UtcNow,
                Status = 1
            });

            return Task.CompletedTask;
        }

        private async Task<MachineData> UpsertMachineDataAsync(
            PublisherNode publisherNode,
            RemoteDatabaseFetchResult database,
            Guid messageId,
            DateTime syncedAtUtc,
            CancellationToken cancellationToken)
        {
            var status = ResolveMachineStatus(database.Tests);
            var entity = await _dbContext.MachineDatas
                .SingleOrDefaultAsync(x => x.PublisherNodeId == publisherNode.PublisherNodeId && x.MachineId == publisherNode.MachineId, cancellationToken);

            entity ??= new MachineData
            {
                PublisherNodeId = publisherNode.PublisherNodeId,
                SourceLocalId = publisherNode.MachineId,
                MachineId = publisherNode.MachineId,
                MachineName = publisherNode.MachineName,
                Channels = new List<Channel>()
            };

            entity.MachineName = publisherNode.MachineName;
            entity.Status = status;
            entity.Capacity = database.TestListCount;
            entity.LastUpdated = syncedAtUtc;
            entity.LastMessageId = messageId;
            entity.BrokerReceivedAtUtc = syncedAtUtc;

            if (entity.Id == 0)
            {
                _dbContext.MachineDatas.Add(entity);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            return entity;
        }

        private async Task<TestProfile> UpsertTestProfileAsync(
            Guid publisherNodeId,
            RemoteTestHierarchy test,
            Guid messageId,
            DateTime syncedAtUtc,
            CancellationToken cancellationToken)
        {
            var profileName = GetString(test.TestList, ProfileCandidates) ?? "Default";
            var profileObjectName = GetString(test.TestList, ProjectCandidates) ?? profileName;
            var sourceLocalId = ToSourceLocalId($"{profileName}:{profileObjectName}");
            var entity = await _dbContext.TestProfiles
                .SingleOrDefaultAsync(x => x.PublisherNodeId == publisherNodeId && x.SourceLocalId == sourceLocalId, cancellationToken);

            entity ??= new TestProfile
            {
                PublisherNodeId = publisherNodeId,
                SourceLocalId = sourceLocalId,
                Tests = new List<Test>(),
                CreatedDateTime = syncedAtUtc
            };

            entity.TestProfileName = profileName;
            entity.TestObjectName = profileObjectName;
            entity.UDSRequestID = string.Empty;
            entity.UDSResponseID = string.Empty;
            entity.ModifiedDateTime = syncedAtUtc;
            entity.LastMessageId = messageId;
            entity.BrokerReceivedAtUtc = syncedAtUtc;
            entity.ExecuteStopTests = false;

            if (entity.Id == 0)
            {
                _dbContext.TestProfiles.Add(entity);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            return entity;
        }

        private async Task<Channel> UpsertChannelAsync(
            Guid publisherNodeId,
            MachineData machineData,
            TestProfile testProfile,
            RemoteTestHierarchy test,
            Guid messageId,
            DateTime syncedAtUtc,
            CancellationToken cancellationToken)
        {
            var channelIndex = GetInt(test.TestList, ChannelIndexCandidates) ?? 0;
            var entity = await _dbContext.Channels
                .SingleOrDefaultAsync(x => x.MachineDataId == machineData.Id && x.ChannelIndex == (uint)channelIndex, cancellationToken);

            entity ??= new Channel
            {
                PublisherNodeId = publisherNodeId,
                SourceLocalId = channelIndex,
                MachineDataId = machineData.Id,
                cANMessagePairList = new List<CANMessagePair>(),
                sMBMessagePairList = new List<SMBMessagePair>()
            };

            entity.PublisherNodeId = publisherNodeId;
            entity.LastMessageId = messageId;
            entity.BrokerReceivedAtUtc = syncedAtUtc;
            entity.ambientTemperature = 0;
            entity.ChannelIndex = (uint)channelIndex;
            entity.TestID = ToSourceLocalId(GetSourceKey(test.TestList, TestIdCandidates, BuildFallbackTestKey(test.TestList)));
            entity.BarCode = GetString(test.TestList, BarcodeCandidates) ?? string.Empty;
            entity.TestName = GetString(test.TestList, TestNameCandidates) ?? string.Empty;
            entity.ChannelStatus = ResolveChannelStatus(GetString(test.TestList, ResultCandidates));
            entity.IsRunning = IsRunningResult(GetString(test.TestList, ResultCandidates));
            entity.Result = GetString(test.TestList, ResultCandidates) ?? string.Empty;
            entity.Retest = ParseBool(GetString(test.TestList, RetestCandidates));
            entity.RetestNumber = GetString(test.TestList, RetestCandidates) ?? "0";
            entity.UserName = GetString(test.TestList, UserCandidates) ?? string.Empty;
            entity.StartDateTime = GetDateTime(test.TestList, StartCandidates) ?? syncedAtUtc;
            entity.EndDateTime = GetDateTime(test.TestList, EndCandidates) ?? syncedAtUtc;
            entity.TestProfileId = testProfile.Id;
            entity.testProfile = testProfile;
            entity.ManuallyStopFlag = false;
            entity.StopTestsExecuted = false;
            entity.BINNumber = GetString(test.TestList, BinCandidates) ?? string.Empty;

            if (entity.Id == 0)
            {
                _dbContext.Channels.Add(entity);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            return entity;
        }

        private async Task UpsertTestListAsync(
            Guid publisherNodeId,
            MachineData machineData,
            Channel channel,
            TestProfile testProfile,
            RemoteTestHierarchy test,
            Guid messageId,
            DateTime syncedAtUtc,
            CancellationToken cancellationToken)
        {
            var sourceTestKey = GetSourceKey(test.TestList, TestIdCandidates, BuildFallbackTestKey(test.TestList));
            var sourceLocalId = ToSourceLocalId(sourceTestKey);
            var entity = await _dbContext.TestList_Table
                .SingleOrDefaultAsync(x => x.PublisherNodeId == publisherNodeId && x.SourceLocalId == sourceLocalId, cancellationToken);

            entity ??= new TestList
            {
                PublisherNodeId = publisherNodeId,
                SourceLocalId = sourceLocalId
            };

            entity.MachineDataId = machineData.Id;
            entity.ChannelId = channel.Id;
            entity.TestProfileId = testProfile.Id;
            entity.LastMessageId = messageId;
            entity.BrokerReceivedAtUtc = syncedAtUtc;
            entity.Test_Name = channel.TestName;
            entity.Barcode = channel.BarCode;
            entity.Result = channel.Result;
            entity.Retest = channel.RetestNumber;
            entity.Start_Date_Time = GetLong(test.TestList, StartCandidates);
            entity.End_Date_Time = GetLong(test.TestList, EndCandidates);
            entity.User_Name = channel.UserName;
            entity.Channel_Index = (int)channel.ChannelIndex;
            entity.BIN_Number = channel.BINNumber;
            entity.TestProjectName = testProfile.TestObjectName;
            entity.TestProfile_Name = testProfile.TestProfileName;

            if (entity.Test_ID == 0)
            {
                _dbContext.TestList_Table.Add(entity);
            }
        }

        private async Task<Test> UpsertSubTestAsync(
            Guid publisherNodeId,
            Channel channel,
            TestProfile testProfile,
            RemoteTestHierarchy parentTest,
            RemoteSubTestHierarchy subTest,
            Guid messageId,
            DateTime syncedAtUtc,
            CancellationToken cancellationToken)
        {
            var sourceSubTestKey = GetSourceKey(subTest.SubTest, SubTestIdCandidates, BuildFallbackSubTestKey(subTest.SubTest, GetSourceKey(parentTest.TestList, TestIdCandidates, BuildFallbackTestKey(parentTest.TestList))));
            var sourceLocalId = ToSourceLocalId(sourceSubTestKey);
            var entity = await _dbContext.Tests
                .SingleOrDefaultAsync(x => x.PublisherNodeId == publisherNodeId && x.SourceLocalId == sourceLocalId, cancellationToken);

            entity ??= new Test
            {
                PublisherNodeId = publisherNodeId,
                SourceLocalId = sourceLocalId
            };

            entity.ChannelId = channel.Id;
            entity.TestProfileId = testProfile.Id;
            entity.TestID = sourceLocalId;
            entity.Enable = GetBool(subTest.SubTest, EnableCandidates) ?? true;
            entity.StopOnFail = GetBool(subTest.SubTest, StopOnFailCandidates) ?? false;
            entity.TestName = GetString(subTest.SubTest, TestNameCandidates) ?? channel.TestName;
            entity.ScheduleName = GetString(subTest.SubTest, ScheduleCandidates) ?? string.Empty;
            entity.TestStatus = GetString(subTest.SubTest, StatusCandidates) ?? ResolveChannelStatus(channel.Result);
            entity.Result = GetString(subTest.SubTest, ResultCandidates) ?? channel.Result;
            entity.StartDateTime = GetDateTime(subTest.SubTest, StartCandidates) ?? channel.StartDateTime;
            entity.EndDateTime = GetDateTime(subTest.SubTest, EndCandidates) ?? channel.EndDateTime;
            entity.LastMessageId = messageId;
            entity.BrokerReceivedAtUtc = syncedAtUtc;

            if (entity.Id == 0)
            {
                _dbContext.Tests.Add(entity);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            return entity;
        }

        private async Task UpsertLimitAsync(
            Guid publisherNodeId,
            Test? subTestEntity,
            RemoteTestHierarchy parentTest,
            RemoteSubTestHierarchy? subTest,
            RemoteDatabaseRow limit,
            Guid messageId,
            DateTime syncedAtUtc,
            CancellationToken cancellationToken)
        {
            var sourceLimitKey = GetSourceKey(limit, LimitIdCandidates, BuildFallbackLimitKey(limit, GetSourceKey(parentTest.TestList, TestIdCandidates, BuildFallbackTestKey(parentTest.TestList)), subTest == null ? null : GetSourceKey(subTest.SubTest, SubTestIdCandidates, "subtest")));
            var sourceLocalId = ToSourceLocalId(sourceLimitKey);
            var entity = await _dbContext.Limits
                .SingleOrDefaultAsync(x => x.PublisherNodeId == publisherNodeId && x.SourceLocalId == sourceLocalId, cancellationToken);

            entity ??= new Limit
            {
                PublisherNodeId = publisherNodeId,
                SourceLocalId = sourceLocalId
            };

            entity.TestId = subTestEntity?.Id;
            entity.LimitName = GetString(limit, LimitNameCandidates) ?? string.Empty;
            entity.MinValue = GetDouble(limit, MinCandidates) ?? 0;
            entity.MaxValue = GetDouble(limit, MaxCandidates) ?? 0;
            entity.MeasuredValue = GetString(limit, MeasuredCandidates) ?? string.Empty;
            entity.Unit = GetString(limit, UnitCandidates) ?? string.Empty;
            entity.Tolerance = GetString(limit, ToleranceCandidates) ?? string.Empty;
            entity.Result = GetString(limit, ResultCandidates) ?? string.Empty;
            entity.LastMessageId = messageId;
            entity.BrokerReceivedAtUtc = syncedAtUtc;

            if (entity.LimitID == 0)
            {
                _dbContext.Limits.Add(entity);
            }
        }

        private static string GetSourceKey(RemoteDatabaseRow row, IEnumerable<string> candidates, string fallback)
        {
            foreach (var candidate in candidates)
            {
                if (TryGetValue(row, candidate, out var value) && !string.IsNullOrWhiteSpace(value))
                {
                    return value!;
                }
            }

            return fallback;
        }

        private static string BuildFallbackTestKey(RemoteDatabaseRow row)
        {
            var barcode = GetString(row, BarcodeCandidates) ?? "unknown-barcode";
            var channel = GetInt(row, ChannelIndexCandidates)?.ToString() ?? "unknown-channel";
            var start = GetLong(row, StartCandidates)?.ToString() ?? "unknown-start";
            return $"{barcode}:{channel}:{start}";
        }

        private static string BuildFallbackSubTestKey(RemoteDatabaseRow row, string parentSourceTestKey)
        {
            var name = GetString(row, TestNameCandidates) ?? "unknown-subtest";
            var start = GetDateTime(row, StartCandidates)?.Ticks.ToString() ?? "unknown-start";
            return $"{parentSourceTestKey}:{name}:{start}";
        }

        private static string BuildFallbackLimitKey(RemoteDatabaseRow row, string parentSourceTestKey, string? parentSourceSubTestKey)
        {
            var name = GetString(row, LimitNameCandidates) ?? "unknown-limit";
            var result = GetString(row, ResultCandidates) ?? "unknown-result";
            return $"{parentSourceTestKey}:{parentSourceSubTestKey}:{name}:{result}";
        }

        private static bool TryGetValue(RemoteDatabaseRow row, string key, out string? value)
        {
            var actualKey = row.Values.Keys.FirstOrDefault(x => x.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (actualKey == null)
            {
                value = null;
                return false;
            }

            value = Convert.ToString(row.Values[actualKey]);
            return true;
        }

        private static string? GetString(RemoteDatabaseRow row, IEnumerable<string> candidates)
        {
            foreach (var candidate in candidates)
            {
                if (TryGetValue(row, candidate, out var value) && !string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return null;
        }

        private static int? GetInt(RemoteDatabaseRow row, IEnumerable<string> candidates)
        {
            foreach (var candidate in candidates)
            {
                if (TryGetValue(row, candidate, out var value) && int.TryParse(value, out var result))
                {
                    return result;
                }
            }

            return null;
        }

        private static long? GetLong(RemoteDatabaseRow row, IEnumerable<string> candidates)
        {
            foreach (var candidate in candidates)
            {
                if (TryGetValue(row, candidate, out var value) && long.TryParse(value, out var result))
                {
                    return result;
                }
            }

            return null;
        }

        private static double? GetDouble(RemoteDatabaseRow row, IEnumerable<string> candidates)
        {
            foreach (var candidate in candidates)
            {
                if (TryGetValue(row, candidate, out var value) && double.TryParse(value, out var result))
                {
                    return result;
                }
            }

            return null;
        }

        private static bool? GetBool(RemoteDatabaseRow row, IEnumerable<string> candidates)
        {
            foreach (var candidate in candidates)
            {
                if (TryGetValue(row, candidate, out var value))
                {
                    if (bool.TryParse(value, out var boolResult))
                    {
                        return boolResult;
                    }

                    if (int.TryParse(value, out var intResult))
                    {
                        return intResult != 0;
                    }
                }
            }

            return null;
        }

        private static DateTime? GetDateTime(RemoteDatabaseRow row, IEnumerable<string> candidates)
        {
            foreach (var candidate in candidates)
            {
                if (TryGetValue(row, candidate, out var value))
                {
                    if (DateTime.TryParse(value, out var dateTime))
                    {
                        return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                    }

                    if (long.TryParse(value, out var raw))
                    {
                        return ToUtcDateTime(raw);
                    }
                }
            }

            return null;
        }

        private static DateTime? ToUtcDateTime(long? value)
        {
            if (!value.HasValue)
            {
                return null;
            }

            // Remote tables commonly store Unix epoch milliseconds.
            return DateTimeOffset.FromUnixTimeMilliseconds(value.Value).UtcDateTime;
        }

        private static int ExtractMachineId(string connectionName)
        {
            var digits = new string(connectionName.Where(char.IsDigit).ToArray());
            if (int.TryParse(digits, out var machineId))
            {
                return machineId;
            }

            return Math.Abs(connectionName.GetHashCode());
        }

        private static int ToSourceLocalId(string value)
        {
            if (int.TryParse(value, out var parsed))
            {
                return parsed;
            }

            return Math.Abs(StringComparer.OrdinalIgnoreCase.GetHashCode(value));
        }

        private static string ResolveMachineStatus(IEnumerable<RemoteTestHierarchy> tests)
        {
            var results = tests.Select(x => GetString(x.TestList, ResultCandidates)).ToList();
            if (results.Any(IsRunningResult))
            {
                return "Running";
            }

            if (results.Any(x => IsMatch(x, "Fail") || IsMatch(x, "Error")))
            {
                return "Error";
            }

            return "Idle";
        }

        private static string ResolveChannelStatus(string? result)
        {
            if (IsRunningResult(result))
            {
                return "Running";
            }

            if (IsMatch(result, "Fail") || IsMatch(result, "Error"))
            {
                return "Error";
            }

            return "Idle";
        }

        private static bool IsRunningResult(string? result)
        {
            return IsMatch(result, "Running") || IsMatch(result, "InProgress");
        }

        private static bool IsMatch(string? value, string expected)
        {
            return string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
        }

        private static bool ParseBool(string? value)
        {
            if (bool.TryParse(value, out var boolResult))
            {
                return boolResult;
            }

            return value == "1";
        }
    }
}
