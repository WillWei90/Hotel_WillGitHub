using System.ComponentModel.DataAnnotations;

namespace HotelBookingSystem.Models
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "請輸入電子信箱")]
        [EmailAddress(ErrorMessage = "請輸入有效的電子信箱")]
        public string Email { get; set; }
    }
}
