using System;
using System.ComponentModel.DataAnnotations;

namespace HotelBookingSystem.Models
{
    public class QA
    {
        [Key]
        public string QaNo { get; set; }

        public string QuestionNo { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Question { get; set; }

        [MaxLength(1000)]
        public string? Answer { get; set; } // 改為 nullable

        [MaxLength(255)]
        public string? Name { get; set; } // 改為 nullable

        [Required]
        public DateTime CreateTime { get; set; }

        public DateTime? ReplyTime { get; set; } // 改為 nullable

        [Required]
        public bool Solve { get; set; }
    }

    public class QAReplyModel
    {
        public string QaNo { get; set; } // 流水號
        public string Answer { get; set; } // 回答內容
        public string Name { get; set; } // 回覆人
    }
}
