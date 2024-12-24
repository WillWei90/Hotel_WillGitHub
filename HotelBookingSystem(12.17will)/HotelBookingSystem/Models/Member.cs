using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingSystem.Models
{
    [Table("Member")]
    public class Member
    {
        [Key]
        public int MemberNo { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("UserName")] // 明確指定資料庫欄位名稱
        public string UserName { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("Password")]
        public string Password { get; set; }

        [MaxLength(15)]
        [Column("Phone")] // 明確指定資料庫欄位名稱
        public string Phone { get; set; }
        public DateTime Birthday { get; set; }
        public DateTime JoinDate { get; set; }

        public ICollection<Order> Orders { get; set; }
        
    }
}
