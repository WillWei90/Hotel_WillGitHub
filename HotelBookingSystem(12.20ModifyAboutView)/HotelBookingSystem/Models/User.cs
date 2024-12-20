using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingSystem.Models
{
    [Table("Admin")] // 將模型映射到資料庫中的 Admin 表
    public class User
    {
        [Key] // 指定主鍵
        public int AdminNo { get; set; } // 主鍵屬性

        public string UserName { get; set; }
        public string Password { get; set; }

        public string Email { get; set; }
        public string PermissionFlag { get; set; }
        public bool Activate { get; set; }
        public DateTime? LastLoginDate { get; set; }

    }

}
