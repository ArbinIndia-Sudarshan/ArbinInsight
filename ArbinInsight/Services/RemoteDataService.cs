using ArbinInsight.Models.RemoteData;
using Microsoft.Data.SqlClient;

namespace ArbinInsight.Services
{
    public class RemoteDataService : IRemoteDataService
    {
        private static readonly string[] TestIdCandidates = ["Test_ID", "TestId", "Id", "ID"];
        private static readonly string[] SubTestIdCandidates = ["SubTest_List_ID", "SubTestId", "SubTestID", "Id", "ID"];
        private static readonly string[] SubTestParentCandidates = ["Test_ID", "TestId", "TestListId", "ParentTestId"];
        private static readonly string[] LimitParentCandidates = ["SubTest_ID", "SubTestId", "SubTestID", "Test_ID", "TestId"];

        private readonly IConfiguration _configuration;

        public RemoteDataService(IConfiguration configuration)
        {
            _configuration = configuration;
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
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
            }

            return result;
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
    }
}
