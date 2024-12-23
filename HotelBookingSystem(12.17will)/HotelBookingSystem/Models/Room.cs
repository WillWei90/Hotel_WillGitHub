using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingSystem.Models
{
    [Table("Room")]
    public class Room
    {
        [Key]
        public int RoomNo { get; set; }

        public string RoomName { get; set; }
        public int Price { get; set; }
        public string RoomType { get; set; }
        public string Address { get; set; }
        public string RoomContent { get; set; }
        public bool Activate { get; set; }
        public string? ImagePath { get; set; } // 可為 NULL
        public int? RoomCapacity { get; set; } // 可為 NULL
        public byte[]? ProfilePicture { get; set; } // 可為 NULL


        // 分頁屬性
        [NotMapped]
        public int TotalRecords { get; set; }
        [NotMapped]
        public int PageSize { get; set; }
        [NotMapped]
        public int CurrentPage { get; set; }
        [NotMapped]
        public int TotalPages { get; set; }

        // 導覽屬性：與 Order 的一對多關係
        public virtual ICollection<Order> Orders { get; set; }

        // 建構函數初始化集合
        public Room()
        {
            Orders = new HashSet<Order>();
        }
    }
}
