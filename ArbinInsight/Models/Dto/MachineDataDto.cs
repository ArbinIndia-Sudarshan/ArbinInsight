using System.Threading.Channels;

namespace ArbinInsight.Models.Dto
{
    public class MachineDataDto
    {
        public int Id { get; set; }
        public int MachineId { get; set; }
        public string MachineName { get; set; } = string.Empty;
        public double Capacity { get; set; }
        public string Status { get; set; } = string.Empty; //Connection status, e.g., "Connected", "Disconnected"
        public List<ChannelDto> Channels { get; set; } = new List<ChannelDto>();
        public DateTime LastUpdated { get; set; }
    }
}