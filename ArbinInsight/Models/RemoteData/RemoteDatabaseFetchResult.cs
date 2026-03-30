namespace ArbinInsight.Models.RemoteData
{
    public class RemoteDatabaseFetchResult
    {
        public string ConnectionName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? Error { get; set; }
        public int TestListCount { get; set; }
        public int SubTestCount { get; set; }
        public int LimitCount { get; set; }
        public List<RemoteTestHierarchy> Tests { get; set; } = new();
    }
}
