using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArbinInsight.Models.Dto
{
    public class TestListDto
    {
        [Key]
        public int Test_ID { get; set; }
        public string? Test_Name { get; set; }
        public string? Barcode { get; set; }
        public string? Result { get; set; }
        public string? Retest { get; set; }
        public long? Start_Date_Time { get; set; }
        public long? End_Date_Time { get; set; }
        public string? User_Name { get; set; }
        public int? Channel_Index { get; set; }
        public string? BIN_Number { get; set; }
        public string? TestProjectName { get; set; }
        public string? TestProfile_Name { get; set; }
        public MachineDataDto? MachineData { get; set; }
    }
}

