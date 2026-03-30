using static System.Net.Mime.MediaTypeNames;

namespace ArbinInsight.Models.Dto
{
    public class TestProfileDto
    {
        public int Id { get; set; }
        public string TestProfileName { get; set; }
        public string TestObjectName { get; set; }
        public string? CANBMSFileName { get; set; }
        public string? SMBFileName { get; set; }
        public List<TestDto> Tests { get; set; }
        public string UDSRequestID { get; set; }
        public string UDSResponseID { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public DateTime ModifiedDateTime { get; set; }
        public string? Creator { get; set; }
        public string? Modifier { get; set; }
        public bool ExecuteStopTests { get; set; }
    }
}
