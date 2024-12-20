using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace HotelBookingSystem.Models
{
    public class MemberAccount
    {
        [Key] // 定義主鍵
        public int MemberNo { get; set; }

        [Required(ErrorMessage = "請輸入電子信箱")]
        [EmailAddress(ErrorMessage = "請輸入有效的電子信箱")]
        public string UserName { get; set; }
        [Required(ErrorMessage = "請輸入密碼")]
        [StringLength(100, ErrorMessage = "密碼長度至少需要8個字元", MinimumLength = 8)]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8,}$", ErrorMessage = "密碼至少8個字元且至少一個字母和一個數字")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        [Required(ErrorMessage = "請輸入手機號碼")]
        [RegularExpression(@"^09\d{8}$", ErrorMessage = "請輸入有效的台灣手機號碼")]

        public string Phone { get; set; }

        [Required(ErrorMessage = "請選擇生日")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [MinimumAge(20, ErrorMessage = "必須年滿20歲才能註冊")]
        public DateTime Birthday { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime JoinDate { get; set; }
    }

    // 自訂驗證屬性，檢查是否滿20歲
    public class MinimumAgeAttribute : ValidationAttribute
    {
        private readonly int _minimumAge;

        public MinimumAgeAttribute(int minimumAge)
        {
            _minimumAge = minimumAge;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return new ValidationResult("請選擇生日");
            }

            var birthday = (DateTime)value;
            var age = DateTime.Today.Year - birthday.Year;

            // 如果今年還沒過生日，要減一歲
            if (birthday.Date > DateTime.Today.AddYears(-age))
            {
                age--;
            }

            if (age < _minimumAge)
            {
                return new ValidationResult($"必須年滿{_minimumAge}歲才能註冊");
            }

            return ValidationResult.Success;
        }
    }
}
