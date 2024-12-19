using System.ComponentModel.DataAnnotations;

namespace HotelBookingSystem.Models
{
    public class ChangePhoneViewModel
    {
        [Required(ErrorMessage = "請輸入目前電話")]
        [RegularExpression(@"^09\d{8}$", ErrorMessage = "請輸入有效的台灣手機號碼")]
        public string CurrentPhone { get; set; }

        [Required(ErrorMessage = "請輸入新電話")]
        [RegularExpression(@"^09\d{8}$", ErrorMessage = "請輸入有效的台灣手機號碼")]
        public string NewPhone { get; set; }

        [Required(ErrorMessage = "請確認新電話")]
        [RegularExpression(@"^09\d{8}$", ErrorMessage = "請輸入有效的台灣手機號碼")]
        public string ConfirmPhone { get; set; }
    }
}
