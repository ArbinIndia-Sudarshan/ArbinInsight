using System.ComponentModel.DataAnnotations;

namespace ArbinInsight.Models.Dto
{
    public class TestDto
    {
        public int Id { get; set; }
        public int TestID { get; set; }
        public bool Enable { get; set; }
        public bool StopOnFail { get; set; }
        public string TestName { get; set; }
        public string ScheduleName { get; set; }
        public string TestStatus { get; set; }
        public string Result { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public List<LimitDto> Limits { get; set; }
    }
}
