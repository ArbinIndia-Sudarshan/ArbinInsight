namespace ArbinInsight.Models.RemoteData
{
    public class RemoteTestHierarchy
    {
        public RemoteDatabaseRow TestList { get; set; } = new();
        public List<RemoteSubTestHierarchy> SubTests { get; set; } = new();
        public List<RemoteDatabaseRow> UnmatchedLimits { get; set; } = new();
    }
}
