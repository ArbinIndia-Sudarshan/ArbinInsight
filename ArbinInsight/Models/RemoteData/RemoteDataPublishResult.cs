namespace ArbinInsight.Models.RemoteData
{
    public class RemoteDataPublishResult
    {
        public DateTime PublishedAtUtc { get; set; }
        public int PublishedMessageCount { get; set; }
        public List<string> PublishedConnections { get; set; } = new();
    }
}
