using System.ComponentModel.DataAnnotations;

namespace ArbinInsight.Models.Dto
{
    public class LimitDto
    {
        [Key]
        public int LimitID { get; set; }
        public string LimitName { get; set; }
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public string MeasuredValue { get; set; }
        public string Unit { get; set; }
        public string Tolerance { get; set; }
        public string Result { get; set; }
    }
}
