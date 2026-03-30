using ArbinInsight.Models.RemoteData;

namespace ArbinInsight.Models.Dashboard
{
    public class DashboardSyncResponse
    {
        public DateTime SyncedAtUtc { get; set; }
        public int SavedTests { get; set; }
        public int SavedSubTests { get; set; }
        public int SavedLimits { get; set; }
        public int PublishedMessageCount { get; set; }
        public List<string> SynchronizedConnections { get; set; } = new();
        public RemoteDataFetchResponse RemoteData { get; set; } = new();
    }
}
