using HotelBookingSystem.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Net.Mail; // 用于发送邮件

namespace HotelBookingSystem.Controllers
{
    public class ConsoleMemberController : Controller
    {
        private readonly HotelDbContext _context;

        public ConsoleMemberController(HotelDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult MemberManagement(string searchTerm)
        {
            // 僅加載會員數據，不加載訂單
            var members = _context.Members
                .Where(m => string.IsNullOrEmpty(searchTerm) ||
                            m.UserName.Contains(searchTerm) ||
                            m.Phone.Contains(searchTerm))
                .Select(m => new Member
                {
                    MemberNo = m.MemberNo,
                    UserName = m.UserName,
                    Phone = m.Phone
                })
                .ToList();

            ViewBag.SearchTerm = searchTerm;
            return View(members);
        }

        [HttpGet]
        public IActionResult GetOrdersByMemberNo(int memberNo)
        {
            var orders = _context.Orders
                .Where(o => o.MemberNo == memberNo)
                .Select(o => new
                {
                    OrderNo = o.OrderNo,
                    RoomNo = o.RoomNo,
                    RoomName = _context.Rooms
                        .Where(r => r.RoomNo == o.RoomNo)
                        .Select(r => r.RoomName)
                        .FirstOrDefault(),
                    StartDate = o.StartDate,
                    EndDate = o.EndDate,
                    IsPay = o.IsPay,
                    Cancel = o.Cancel
                }).ToList();

            return Json(new { success = true, orders });
        }


        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        [HttpPost]
        [HttpPost]
        public IActionResult ResetPassword(string UserName)
        {
            // 根據 UserName 查找會員
            var member = _context.Members.FirstOrDefault(m => m.UserName == UserName);

            if (member == null)
            {
                TempData["Error"] = "Member not found.";
                return RedirectToAction("MemberManagement");
            }

            try
            {
                // 生成新密碼
                string newPassword = GenerateTemporaryPassword();

                // 更新資料庫中的密碼
                member.Password = newPassword;
                _context.SaveChanges();

                // 模擬通知會員成功重置密碼（例如，顯示訊息而不是發送郵件）
                TempData["Success"] = $"Password reset successfully for {member.UserName}. New password is: {newPassword}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to reset password: {ex.Message}";
            }

            return RedirectToAction("MemberManagement");
        }


        private string GenerateTemporaryPassword()
        {
            return Guid.NewGuid().ToString().Substring(0, 8); // 生成8位随机密码
        }

        private void SendEmail(string toEmail, string subject, string body)
        {
            // 配置 Gmail SMTP 客户端
            using (var client = new SmtpClient("smtp.gmail.com"))
            {
                client.Port = 465; // Gmail SMTP 使用端口587
                client.Credentials = new System.Net.NetworkCredential("hotellazzydog@gmail.com", "lbiu mbvn zdsj zxei");
                client.EnableSsl = true; // 使用 SSL 安全连接

                var mailMessage = new MailMessage
                {
                    From = new MailAddress("hotellazzydog@gmail.com"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false // 如果需要支持 HTML 格式邮件，可以将此设置为 true
                };

                mailMessage.To.Add(toEmail);

                try
                {
                    client.Send(mailMessage); // 发送邮件
                    TempData["Success"] = $"Password reset email sent successfully to {toEmail}.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Failed to send email: {ex.Message}";
                }
            }
        }
    }
}
