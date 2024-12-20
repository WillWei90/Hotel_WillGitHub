using System.Diagnostics;
using HotelBookingSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using static HotelBookingSystem.Controllers.ConsoleUserController;

namespace HotelBookingSystem.Controllers
{
    public class ConsoleHomeController : Controller
    {
        private readonly ILogger<ConsoleHomeController> _logger;
        private readonly HotelDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ConsoleHomeController(HotelDbContext context, ILogger<ConsoleHomeController> logger, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public IActionResult Login()
        {
            return View();
        }

        public IActionResult Home()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            ViewBag.UserName = session?.GetString("UserName");

            if (string.IsNullOrEmpty(ViewBag.UserName))
            {
                // 如果 Session 中沒有 UserName，重定向到登錄頁面
                return RedirectToAction("Login", "ConsoleHome");
            }

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            // 將使用者輸入的密碼進行哈希
            string hashedPassword = PasswordHelper.HashPassword(password);

            // 驗證使用者名稱和密碼
            var user = _context.Users
                .FirstOrDefault(u => u.UserName == username && u.Password == hashedPassword);

            if (user == null)
            {
                // 使用者名稱或密碼無效
                return Json(new { success = false, message = "錯誤使用者名稱或密碼." });
            }
            else if (!user.Activate)
            {
                // 使用者未啟用
                return Json(new { success = false, message = "此使用者尚未啟用,請連絡後台管理者" });
            }
            else
            {
                // 更新 LastLoginDate
                user.LastLoginDate = DateTime.Now;

                // 儲存變更到資料庫
                _context.SaveChanges();

                // 登入成功，設定 Session
                var session = _httpContextAccessor.HttpContext?.Session;
                session?.SetString("UserName", user.UserName);
                session?.SetString("PermissionFlag", user.PermissionFlag);

                // 返回成功結果和重定向 URL
                return Json(new { success = true, redirectUrl = Url.Action("Home", "ConsoleHome") });
            }
        }



        public IActionResult Logout()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            session?.Clear();
            return RedirectToAction("Login", "ConsoleHome");
        }

        [HttpGet]
        public IActionResult GetUserInfo()
        {
            var userName = HttpContext.Session.GetString("UserName");
            var permissionFlag = HttpContext.Session.GetString("PermissionFlag");

            if (string.IsNullOrEmpty(userName))
            {
                return Json(new { success = false, message = "User is not logged in." });
            }

            string menuHtml = "";
            if (permissionFlag == "Admin")
            {
                menuHtml = @"
            <li class='nav-item'><a class='nav-link text-dark' href='/ConsoleMember/MemberManagement'>會員管理</a></li>
            <li class='nav-item'><a class='nav-link text-dark' href='/ConsoleOrder/OrderManagement'>訂單管理</a></li>
            <li class='nav-item'><a class='nav-link text-dark' href='/ConsoleRoom/Room'>客房管理</a></li>
            <li class='nav-item'><a class='nav-link text-dark' href='/ConsoleUser/UserManagement'>使用者管理</a></li>";
            }
            else if (permissionFlag == "Product")
            {
                menuHtml = @"
            <li class='nav-item'><a class='nav-link text-dark' href='/ConsoleRoom/Room'>客房管理</a></li>";
            }
            else if (permissionFlag == "Customer")
            {
                menuHtml = @"
            <li class='nav-item'><a class='nav-link text-dark' href='/ConsoleMember/MemberManagement'>會員管理</a></li>
            <li class='nav-item'><a class='nav-link text-dark' href='/ConsoleOrder/OrderManagement'>訂單管理</a></li>";
            }

            return Json(new { success = true, userName = userName, menuHtml = menuHtml });
        }

        [HttpPost]
        public IActionResult ChangePassword(string CurrentPassword, string NewPassword, string ConfirmPassword)
        {
            if (!HttpContext.Session.TryGetValue("UserName", out byte[] userNameBytes))
            {
                return Json(new { success = false, message = "User not logged in." });
            }

            var userName = System.Text.Encoding.UTF8.GetString(userNameBytes);
            var user = _context.Users.FirstOrDefault(u => u.UserName == userName);

            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            if (user.Password != PasswordHelper.HashPassword(CurrentPassword))
            {
                return Json(new { success = false, message = "Current password is incorrect." });
            }

            if (NewPassword != ConfirmPassword)
            {
                return Json(new { success = false, message = "New password and confirmation do not match." });
            }

            user.Password = PasswordHelper.HashPassword(NewPassword);
            _context.SaveChanges();

            return Json(new { success = true });
        }

    }
}
