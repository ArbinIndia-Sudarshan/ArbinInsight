using ArbinInsight.Models.Dashboard;
using ArbinInsight.Models.RemoteData;

namespace ArbinInsight.Services
{
    public interface IDashboardSyncService
    {
        Task<DashboardSyncResponse> SyncAsync(bool publishToQueue, CancellationToken cancellationToken = default);
        Task<DashboardSyncResponse> IngestRemoteDatabaseAsync(
            RemoteDatabaseFetchResult database,
            Guid? messageId = null,
            string messageType = "RemoteDatabaseFetchResult",
            string routingKey = "remote.testdata",
            CancellationToken cancellationToken = default);
    }
}
