using System.ComponentModel.DataAnnotations;

namespace ArbinInsight.Models.Sync
{
    public class PublisherNode
    {
        [Key]
        public Guid PublisherNodeId { get; set; }
        public string NodeCode { get; set; }
        public int MachineId { get; set; }
        public string MachineName { get; set; }
        public string? HostName { get; set; }
        public string? IpAddress { get; set; }
        public bool IsOnline { get; set; }
        public DateTime? LastHeartbeatUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }
}
