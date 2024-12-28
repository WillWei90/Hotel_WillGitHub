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
            ViewBag.AdminAccount = TempData["AdminAccount"];
            ViewBag.AdminPassword = TempData["AdminPassword"];

            return View();
        }

        public IActionResult Home()
        {
            // 確保系統初始化時存在 admin 帳號
            EnsureAdminExists();

            var session = _httpContextAccessor.HttpContext?.Session;
            ViewBag.UserName = session?.GetString("UserName");

            if (string.IsNullOrEmpty(ViewBag.UserName))
            {
                // 如果 Session 中沒有 UserName，將初始化資訊傳遞到 Login 頁面
                var adminAccount = session?.GetString("AdminAccount");
                var adminPassword = session?.GetString("AdminPassword");

                TempData["AdminAccount"] = adminAccount;
                TempData["AdminPassword"] = adminPassword;

                return RedirectToAction("Login", "ConsoleHome");
            }

            return View();
        }

        private void EnsureAdminExists()
        {
            var admin = _context.Users.FirstOrDefault(u => u.UserName == "admin");
            if (admin == null)
            {
                // 如果系統中沒有 admin，創建一個
                var defaultAdminPassword = "1"; // 預設密碼
                var mail = "hotellazzydog@gmail.com";
                var newAdmin = new User
                {
                    UserName = "Admin",
                    Password = PasswordHelper.HashPassword(defaultAdminPassword),
                    PermissionFlag = "Admin",
                    Activate = true,
                    Email = mail
                };

                _context.Users.Add(newAdmin);
                _context.SaveChanges();

                // 將 admin 資訊儲存到 Session，用於顯示提示
                var session = _httpContextAccessor.HttpContext?.Session;
                session?.SetString("AdminAccount", "admin");
                session?.SetString("AdminPassword", defaultAdminPassword);
            }
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
                return Json(new { success = false, message = "用戶未登入。" });
            }

            string menuHtml = "";
            if (permissionFlag == "Admin")
            {
                menuHtml = @"
            <li class='nav-item'><a class='nav-link text-dark' href='/ConsoleMember/MemberManagement'>會員管理</a></li>
            <li class='nav-item'><a class='nav-link text-dark' href='/ConsoleOrder/OrderManagement'>訂單管理</a></li>
            <li class='nav-item'><a class='nav-link text-dark' href='/ConsoleRoom/Room'>客房管理</a></li>
            <li class='nav-item'><a class='nav-link text-dark' href='/ConsoleQA/index'>客戶問題</a></li>
            <li class='nav-item'><a class='nav-link text-dark' href='/ConsoleUser/UserManagement'>使用者管理</a></li>";
            }
            else if (permissionFlag == "Product")
            {
                menuHtml = @"
            <li class='nav-item'><a class='nav-link text-dark' href='/ConsoleRoom/Room'>客房管理</a></li>
            <li class='nav-item'><a class='nav-link text-dark' href='/ConsoleQA/index'>客戶問題</a></li>";

            }
            else if (permissionFlag == "Customer")
            {
                menuHtml = @"
            <li class='nav-item'><a class='nav-link text-dark' href='/ConsoleMember/MemberManagement'>會員管理</a></li>
            <li class='nav-item'><a class='nav-link text-dark' href='/ConsoleOrder/OrderManagement'>訂單管理</a></li>
            <li class='nav-item'><a class='nav-link text-dark' href='/ConsoleQA/index'>客戶問題</a></li>";
            }

            return Json(new { success = true, userName = userName, menuHtml = menuHtml });
        }

        [HttpPost]
        public IActionResult ChangePassword(string CurrentPassword, string NewPassword, string ConfirmPassword)
        {
            if (!HttpContext.Session.TryGetValue("UserName", out byte[] userNameBytes))
            {
                return Json(new { success = false, message = "用戶未登入。" });
            }

            var userName = System.Text.Encoding.UTF8.GetString(userNameBytes);
            var user = _context.Users.FirstOrDefault(u => u.UserName == userName);

            if (user == null)
            {
                return Json(new { success = false, message = "未找到用戶。" });
            }

            if (user.Password != PasswordHelper.HashPassword(CurrentPassword))
            {
                return Json(new { success = false, message = "目前密碼不正確。" });
            }

            if (NewPassword != ConfirmPassword)
            {
                return Json(new { success = false, message = "新密碼和確認密碼不符。" });
            }

            user.Password = PasswordHelper.HashPassword(NewPassword);
            _context.SaveChanges();

            return Json(new { success = true });
        }

    }
}
