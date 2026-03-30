namespace ArbinInsight.Models.RemoteData
{
    public class RemoteDataFetchResponse
    {
        public DateTime FetchedAtUtc { get; set; }
        public List<RemoteDatabaseFetchResult> Databases { get; set; } = new();
    }
}
