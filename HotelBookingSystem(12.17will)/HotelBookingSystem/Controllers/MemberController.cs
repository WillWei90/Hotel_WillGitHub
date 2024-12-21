using HotelBookingSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingSystem.Controllers
{
    public class MemberController : Controller
    {
        private readonly HotelDbContext _context;
        private readonly ILogger<MemberController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MemberController(
            HotelDbContext context,
            ILogger<MemberController> logger, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult SignIn()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SignIn(MemberAccount user)
        {
            // 初始化資料庫連線並獲取帳號列表
            MemberConnection connection = new MemberConnection();
            List<MemberAccount> accounts = connection.getAccounts();

            // 將使用者輸入的密碼進行 MD5 雜湊
            string hashedPassword = PasswordHelper.HashPassword(user.Password);

            // 驗證帳號與密碼是否匹配
            var queryuser = accounts.FirstOrDefault(a =>
                a.UserName == user.UserName && a.Password == hashedPassword);

            if (queryuser != null)
            {
                // 建立Claims身份
                var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, queryuser.UserName),
                new Claim(ClaimTypes.NameIdentifier.ToString(), queryuser.MemberNo.ToString())
            };

                var claimsIdentity = new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    // 設定登入有效期為30分鐘
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30),
                    IsPersistent = true // 允許跨瀏覽器保持登入
                };

                // 執行登入
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return RedirectToAction("Index");
            }
            else
            {
                var userExists = accounts.Any(a => a.UserName == user.UserName);
                if (userExists)
                {
                    ViewData["result"] = "密碼錯誤";
                    user.Password = string.Empty;
                }
                else
                {
                    ViewData["result"] = "帳號或密碼錯誤";
                    user.UserName = string.Empty;
                    user.Password = string.Empty;
                }
                return View(user);
            }
        }

        [Authorize] // 要求必須登入才能訪問
        public IActionResult IndexAuthorized()
        {
            return View();
        }

        // 登出方法
        public async Task<IActionResult> SignOut()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            // 新增登出後的訊息
            TempData["SignOutMessage"] = "您已成功登出";
            return RedirectToAction("SignIn");
        }

        public IActionResult SignUp()
        {
            return View();
        }

        [HttpPost]
        public IActionResult SignUp(MemberAccount user)
        {
            MemberConnection member = new MemberConnection();

            // 可以在 newAccount 方法前檢查
            if (member.IsUserNameExists(user.UserName))
            {
                ModelState.AddModelError("UserName", "此信箱已被註冊");
                return View(user);
            }

            // 檢查是否通過驗證
            ModelState.Remove("MemberNo");
            if (ModelState.IsValid)
            {
                try
                {
                    // 設定加入日期為當前時間
                    user.JoinDate = DateTime.Now;

                    // 將密碼進行 MD5 加密
                    user.Password = PasswordHelper.HashPassword(user.Password);

                    // 使用 MemberConnection 中的方法將新帳號寫入資料庫
                    member.newAccount(user);
                    // 成功後跳轉到登入頁面
                    return RedirectToAction("SignIn");
                }
                catch (Exception e)
                {
                    // 處理資料庫寫入錯誤
                    ModelState.AddModelError("", "註冊失敗：" + e.Message);
                }
            }

            //偵錯用，過濾每個屬性的ModelState有沒有錯誤
            foreach (var state in ModelState)
            {
                if (state.Value.Errors.Count > 0)
                {
                    Console.WriteLine($"Field: {state.Key}, Errors: {string.Join(", ", state.Value.Errors.Select(e => e.ErrorMessage))}");
                }
            }

            // 如果驗證失敗，返回原頁面並保留有效的輸入
            // 手動設置有效的輸入值
            if (ModelState["UserName"] != null && ModelState["UserName"].Errors.Count == 0)
            {
                ViewData["ValidUserName"] = user.UserName;
            }

            if (ModelState["Phone"] != null && ModelState["Phone"].Errors.Count == 0)
            {
                ViewData["ValidPhone"] = user.Phone;
            }
            if (ModelState["Birthday"] != null && ModelState["Birthday"].Errors.Count == 0)
            {
                ViewData["ValidBirthday"] = user.Birthday.ToString("yyyy-MM-dd");
            }

            return View(user);
        }

        public IActionResult ShowMemberAccount()
        {
            MemberConnection connection = new MemberConnection();
            List<MemberAccount> accounts = connection.getAccounts();
            ViewData["accounts"] = accounts;

            return View();
        }

        [HttpPost]
        public JsonResult OnSubmit([FromBody] MemberAccount user)
        {
            // 建立資料庫連線物件
            MemberConnection connection = new MemberConnection();
            // 獲取所有帳號
            List<MemberAccount> accounts = connection.getAccounts();

            // 先檢查帳號是否存在  (尚未解決:不會顯示文字)
            //var existingUser = accounts.FirstOrDefault(a => a.UserName == user.UserName);
            var userExists = accounts.Any(a => a.UserName == user.UserName);

            if (userExists)
            {
                Console.WriteLine("帳號已存在");
                return Json("帳號已存在");
            }

            else
            {
                Console.WriteLine(user.UserName);
                Console.WriteLine("帳號可使用");
                return Json("");
            }

        }

        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Create(MemberAccount user)
        {
            Console.WriteLine(user);
            if (ModelState.IsValid)
            {
                MemberConnection connection = new MemberConnection();
                connection.newAccount(user);
                return RedirectToAction("Index");
            }
            else
            {
                Console.WriteLine("資料驗證失敗！");
                return View();
            }
        }

        //前端、db傳遞的名稱要一樣才能正常顯示
        public IActionResult CheckRepeatAccount(string UserName)
        {
            Console.WriteLine($"檢查帳號：{UserName}");
            MemberConnection connection = new MemberConnection();
            List<MemberAccount> Accounts = connection.getAccounts();
            if (Accounts.Count > 0)
            {
                Console.WriteLine("取到資料！");
            }
            else
            {
                Console.WriteLine("未取到資料庫資料！");
            }
            var isBookaccountRepeat = Accounts.Any(x => x.UserName == UserName);
            Console.WriteLine(isBookaccountRepeat);

            if (isBookaccountRepeat)
            {
                return Json($"{UserName}已被使用");
            }
            return Json(true);
        }

        public static class PasswordHelper
        {
            public static string HashPassword(string password)
            {
                // 使用 MD5 加密
                using (MD5 md5 = MD5.Create())
                {
                    // 將密碼轉換為位元組陣列
                    byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(password));

                    // 將位元組陣列轉換為十六進位字串
                    StringBuilder builder = new StringBuilder();
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        builder.Append(bytes[i].ToString("x2")); // 每個位元組轉為兩位的16進位
                    }
                    return builder.ToString();
                }
            }

            // 額外驗證密碼的方法
            public static bool VerifyPassword(string inputPassword, string storedPassword)
            {
                return HashPassword(inputPassword) == storedPassword;
            }
        }

        [HttpPost]
        [Authorize]
        [Route("api/[controller]/ChangePassword")]
        public async Task<IActionResult> ChangePasswordApi([FromBody] ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                });
            }

            try
            {
                var user = await _context.MemberAccounts
                    .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

                if (user == null)
                {
                    return NotFound(new { success = false, message = "找不到使用者" });
                }

                // 使用 PasswordHasher 驗證密碼
                if (!PasswordHelper.VerifyPassword(model.CurrentPassword, user.Password))
                {
                    return BadRequest(new { success = false, message = "目前密碼不正確" });
                }

                // 更新密碼
                user.Password = PasswordHelper.HashPassword(model.NewPassword);
                _context.MemberAccounts.Update(user);
                await _context.SaveChangesAsync();

                // 登出
                await HttpContext.SignOutAsync();
                return Ok(new { success = true, message = "密碼修改成功，請重新登入", forceSignOut = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "密碼變更時發生錯誤");
                return StatusCode(500, new { success = false, message = "發生未預期的錯誤" });
            }
        }

        [HttpPost]
        [Authorize]
        [Route("api/[controller]/ChangePhone")]
        public async Task<IActionResult> ChangePhoneApi([FromBody] ChangePhoneViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                });
            }

            try
            {
                var user = await _context.MemberAccounts
                    .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

                if (user == null)
                {
                    return NotFound(new { success = false, message = "找不到使用者" });
                }

                if (user.Phone != model.CurrentPhone)
                {
                    return BadRequest(new { success = false, message = "目前電話不正確" });
                }

                if (model.NewPhone != model.ConfirmPhone)
                {
                    return BadRequest(new { success = false, message = "新電話與確認電話不一致" });
                }

                user.Phone = model.NewPhone;
                _context.MemberAccounts.Update(user);
                await _context.SaveChangesAsync();

                // 登出
                await HttpContext.SignOutAsync();
                return Ok(new { success = true, message = "電話修改成功，請重新登入", forceSignOut = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "電話變更時發生錯誤");
                return StatusCode(500, new { success = false, message = "發生未預期的錯誤" });
            }
        }

    }
}