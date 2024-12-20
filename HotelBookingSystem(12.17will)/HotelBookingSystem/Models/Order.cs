using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingSystem.Models
{
    [Table("Order")]
    public class Order
    {
        [Key]
        public int OrderNo { get; set; }

        [ForeignKey("Member")]
        public int MemberNo { get; set; } // 外鍵，對應 Member 的 MemberNo

        public DateTime OrderDate { get; set; }
        public int RoomNo { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? ExtraServicenNo { get; set; }
        public bool IsPay { get; set; }
        public bool Cancel { get; set; }
        public decimal TotalAmount { get; set; }

        // 新增屬性：房間名稱
        [NotMapped] // 此屬性不會被映射到資料庫
        public string RoomName { get; set; }

        // 導覽屬性
        public virtual Member Member { get; set; }

        [ForeignKey("RoomNo")]
        public virtual Room Room { get; set; }
    }


}
