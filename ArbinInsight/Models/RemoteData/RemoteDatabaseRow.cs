namespace ArbinInsight.Models.RemoteData
{
    public class RemoteDatabaseRow
    {
        public Dictionary<string, object?> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
