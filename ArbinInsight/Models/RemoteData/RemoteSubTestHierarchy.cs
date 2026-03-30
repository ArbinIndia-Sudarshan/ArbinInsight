namespace ArbinInsight.Models.RemoteData
{
    public class RemoteSubTestHierarchy
    {
        public RemoteDatabaseRow SubTest { get; set; } = new();
        public List<RemoteDatabaseRow> Limits { get; set; } = new();
    }
}
