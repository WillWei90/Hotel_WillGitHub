using HotelBookingSystem.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.Net.Mail;

namespace HotelBookingSystem.Controllers
{
    public class ConsoleMemberController : Controller
    {
        private readonly HotelDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ConsoleMemberController(HotelDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpGet]
        public IActionResult MemberManagement(string searchTerm)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            ViewBag.UserName = session?.GetString("UserName");

            if (string.IsNullOrEmpty(ViewBag.UserName))
            {
                // 如果 Session 中沒有 UserName，重定向到登錄頁面
                return RedirectToAction("Login", "ConsoleHome");
            }

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
        public IActionResult Edit(int id)
        {
            var member = _context.Members.Find(id);
            if (member == null)
            {
                return NotFound();
            }
            return View(member);  // 載入編輯頁面
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Member member)
        {
            if (id != member.MemberNo)
            {
                return BadRequest();  // 檢查ID是否一致
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(member);
                    _context.SaveChanges();
                    TempData["SuccessMessage"] = "會員資料已成功更新！";
                    return RedirectToAction(nameof(MemberManagement));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Members.Any(m => m.MemberNo == id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return View(member);  // 如果驗證失敗，返回編輯頁面
        }

        [HttpPost]
        [Route("api/member/update")]
        public IActionResult UpdateMember([FromBody] Member member)
        {
            if (member == null)
            {
                return BadRequest("無效的資料");
            }

            var existingMember = _context.Members.Find(member.MemberNo);
            if (existingMember == null)
            {
                return NotFound();
            }

            existingMember.Phone = member.Phone;
            existingMember.Birthday = member.Birthday;

            _context.SaveChanges();

            return Json(new { success = true, message = "會員資料已成功更新" });
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

        [HttpPost]
        [Route("ConsoleMember/ResetPassword/{id}")]
        public IActionResult ResetPassword(int id)
        {
            var member = _context.Members.Find(id);
            if (member == null)
            {
                return NotFound();
            }

            // 生成隨機密碼
            var randomPassword = GenerateRandomPassword();

            // 使用 HashPassword 進行密碼加密
            var hashedPassword = HashPassword(randomPassword);

            // 更新會員的密碼
            member.Password = hashedPassword;
            _context.SaveChanges();

            // 發送重置密碼郵件
            SendPasswordResetEmail(member.UserName, member.Phone, randomPassword);

            // 返回成功訊息並將新密碼顯示
            return Json(new { success = true, message = "密碼已重置", newPassword = randomPassword });
        }

        // 隨機生成密碼的功能
        private string GenerateRandomPassword(int length = 8)
        {
            const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            Random random = new Random();
            char[] passwordChars = new char[length];
            for (int i = 0; i < length; i++)
            {
                passwordChars[i] = validChars[random.Next(validChars.Length)];
            }
            return new string(passwordChars);
        }

        // 密碼哈希功能
        private string HashPassword(string password)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private void SendPasswordResetEmail(string userName, string phone, string newPassword)
        {
            try
            {
                var smtpClient = new SmtpClient("smtp.gmail.com")  // 使用您的SMTP伺服器
                {
                    Port = 587, // 使用您的SMTP伺服器端口，Gmail通常是587
                    Credentials = new NetworkCredential("hotellazzydog@gmail.com", "lbiu mbvn zdsj zxei"),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress("hotellazzydog@gmail.com", "四季飯店"), // 設定寄件者名稱
                    Subject = "密碼重置通知",
                    Body = $@"
        <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; }}
                    .container {{ margin: 20px; }}
                    .greeting {{ font-size: 18px; font-weight: bold; }}
                    .new-password {{ font-size: 16px; color: #ff5722; font-weight: bold; }}
                    .message {{ font-size: 14px; margin-bottom: 20px; }}
                    .footer {{ margin-top: 20px; font-size: 12px; color: #888; }}
                    .link {{ color: #007bff; text-decoration: none; }}
                    .link:hover {{ text-decoration: underline; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <p class='greeting'>親愛的 {userName}，</p>
                    <p class='message'>
                        您的會員帳號已經成功重置密碼。為了您的安全，請記得在第一次登錄後更改此密碼。
                    </p>
                    <p class='message'>
                        您的新密碼是：<span class='new-password'>{newPassword}</span><br />
                        請使用該密碼登錄，並根據以下鏈接進行下一步操作：
                    </p>
                    <p class='message'>
                        <a href='https://hotelfront.lazzydog.store:9985/' class='link' target='_blank'>
                            點此訪問您的帳戶
                        </a>
                    </p>
                    <p class='message'>
                        如果您遇到任何問題，請隨時聯繫我們的客戶服務團隊，我們將竭誠為您提供幫助。
                    </p>
                    <div class='footer'>
                        <p>謝謝您的使用，祝您有愉快的一天！</p>
                    </div>
                </div>
            </body>
        </html>
    ",
                    IsBodyHtml = true, // 設置郵件內容為HTML
                };

                mailMessage.To.Add(userName); // 用戶的郵件地址

                smtpClient.Send(mailMessage);
            }
            catch (Exception ex)
            {
                // 處理發送郵件錯誤
                Console.WriteLine("發送郵件失敗: " + ex.Message);
            }
        }


    }
}
