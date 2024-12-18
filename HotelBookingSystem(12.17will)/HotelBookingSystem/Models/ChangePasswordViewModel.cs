using System.ComponentModel.DataAnnotations;

namespace HotelBookingSystem.Models
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "請輸入目前密碼")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "請輸入新密碼")]
        [StringLength(100, ErrorMessage = "密碼長度至少需要8個字元", MinimumLength = 8)]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8,}$", ErrorMessage = "密碼至少8個字元且至少一個字母和一個數字")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "新密碼和確認密碼不相符")]
        public string ConfirmPassword { get; set; }
    }
}
