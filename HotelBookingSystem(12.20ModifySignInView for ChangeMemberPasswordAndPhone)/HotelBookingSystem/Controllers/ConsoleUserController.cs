using System;
using System.Linq;
using System.Net.Mail;
using System.Net;
using HotelBookingSystem.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace HotelBookingSystem.Controllers
{
    public class ConsoleUserController : Controller
    {
        private readonly HotelDbContext _context;

        public ConsoleUserController(HotelDbContext context)
        {
            _context = context;
        }

        private bool IsUserLoggedIn()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("UserName"));
        }

        public IActionResult UserManagement()
        {
            // 檢查使用者是否已登入
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login", "ConsoleHome");
            }

            // 獲取當前使用者名稱
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                TempData["Error"] = "使用者未登入，請重新登入。";
                return RedirectToAction("Login", "ConsoleHome");
            }

            ViewBag.UserName = userName;

            // 查詢所有使用者
            var users = _context.Users.ToList();

            // 檢查是否有使用者資料
            if (users == null || users.Count == 0)
            {
                TempData["Info"] = "目前沒有使用者資料。";
                return View(new List<User>()); // 傳遞空列表以防錯誤
            }

            return View(users);
        }

        public static class PasswordHelper
        {
            public static string HashPassword(string password)
            {
                using (var sha256 = SHA256.Create())
                {
                    var bytes = Encoding.UTF8.GetBytes(password);
                    var hash = sha256.ComputeHash(bytes);
                    return Convert.ToBase64String(hash);
                }
            }
        }

        [HttpPost]
        public IActionResult AddUser(User user)
        {
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login");
            }

            if (_context.Users.Any(u => u.UserName == user.UserName))
            {
                TempData["Error"] = "User already exists.";
                return RedirectToAction("UserManagement");
            }

            // 將密碼加密
            user.Password = PasswordHelper.HashPassword(user.Password);

            _context.Users.Add(user);
            _context.SaveChanges();

            TempData["Success"] = "User added successfully.";
            return RedirectToAction("UserManagement");
        }



        //啟用 or 停用使用者
        [HttpPost]
        public IActionResult ToggleActivate(int id)
        {
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login");
            }

            // 获取当前登录用户的用户名
            var currentUserName = HttpContext.Session.GetString("UserName");

            // 查找目标用户
            var user = _context.Users.FirstOrDefault(u => u.AdminNo == id);

            if (user != null)
            {
                // 检查是否为 Admin 用户或当前登录用户
                if (user.UserName.ToLower() == "admin")
                {
                    TempData["Error"] = "Admin user cannot be deactivated.";
                }
                else if (user.UserName.ToLower() == currentUserName.ToLower())
                {
                    TempData["Error"] = "You cannot deactivate your own account.";
                }
                else
                {
                    // 切换激活状态
                    user.Activate = !user.Activate;
                    _context.SaveChanges();
                    TempData["Success"] = "User activation status updated successfully.";
                }
            }
            else
            {
                TempData["Error"] = "User not found.";
            }

            return RedirectToAction("UserManagement");
        }


        [HttpPost]
        public IActionResult DeleteUser(int id)
        {
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login");
            }

            // 获取当前登录用户的用户名
            var currentUserName = HttpContext.Session.GetString("UserName");

            // 查找要删除的用户
            var user = _context.Users.FirstOrDefault(u => u.AdminNo == id);
            if (user != null)
            {
                // 检查是否尝试删除 Admin 用户或当前登录用户
                if (user.UserName.ToLower() == "admin")
                {
                    TempData["Error"] = "Admin user cannot be deleted.";
                }
                else if (user.UserName.ToLower() == currentUserName.ToLower())
                {
                    TempData["Error"] = "You cannot delete your own account.";
                }
                else
                {
                    // 执行删除操作
                    _context.Users.Remove(user);
                    _context.SaveChanges();
                    TempData["Success"] = "User deleted successfully.";
                }
            }
            else
            {
                TempData["Error"] = "User not found.";
            }

            return RedirectToAction("UserManagement");
        }




        //原本改密碼方式
        //[HttpPost]
        //public IActionResult ChangePassword(int AdminNo, string NewPassword)
        //{
        //    if (!IsUserLoggedIn())
        //    {
        //        return RedirectToAction("Login");
        //    }

        //    var user = _context.Users.FirstOrDefault(u => u.AdminNo == AdminNo);
        //    if (user == null)
        //    {
        //        TempData["Error"] = "User not found.";
        //        return RedirectToAction("UserManagement");
        //    }

        //    if (user.UserName.ToLower() == "admin")
        //    {
        //        TempData["Error"] = "Cannot change the password for Admin.";
        //        return RedirectToAction("UserManagement");
        //    }

        //    user.Password = NewPassword;
        //    _context.SaveChanges();
        //    TempData["Success"] = "Password updated successfully.";
        //    return RedirectToAction("UserManagement");
        //}

        [HttpPost]
        public IActionResult ChangePassword(int AdminNo)
        {
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login");
            }

            var user = _context.Users.FirstOrDefault(u => u.AdminNo == AdminNo);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("UserManagement");
            }

            if (user.UserName.ToLower() == "admin")
            {
                TempData["Error"] = "Cannot change the password for Admin.";
                return RedirectToAction("UserManagement");
            }

            // 隨機生成密碼
            string newPassword = GenerateRandomPassword();
            // 加密密碼
            user.Password = PasswordHelper.HashPassword(newPassword);

            _context.SaveChanges();

            // 發送新密碼到使用者 Email
            try
            {
                SendEmail(user.Email, "Password Reset", $"Hello {user.UserName},\n\nYour new password is: {newPassword}\n\nPlease log in and change it as soon as possible.");
                TempData["Success"] = "Password updated successfully. An email with the new password has been sent.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Password updated successfully, but failed to send email: {ex.Message}";
            }

            return RedirectToAction("UserManagement");
        }
        //隨機生成密碼的方法
        private string GenerateRandomPassword(int length = 8)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }
        // 發送 Gmail 的方法
        private void SendEmail(string toEmail, string subject, string body)
        {
            // Gmail SMTP 設定
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("x94g4jo3@gmail.com", "eysa wfln dypm qoyd"),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("x94g4jo3@gmail.com"),
                Subject = subject,
                Body = body,
                IsBodyHtml = false,
            };
            mailMessage.To.Add(toEmail);

            smtpClient.Send(mailMessage);
        }

    }
}
