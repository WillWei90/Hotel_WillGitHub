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
using System.Net.Mail;
using System.Net;

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
            // 檢查輸入是否為空
            if (string.IsNullOrEmpty(user.UserName) || string.IsNullOrEmpty(user.Password))
            {
                ViewData["result"] = "請輸入帳號和密碼";
                return View(user);
            }

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

            // 驗證模型狀態是否有效
            if (!ModelState.IsValid)
            {
                return View(user);
            }

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
                return Json("帳號已存在, 請使用其他信箱註冊");
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

                // 檢查新密碼是否與當前密碼相同
                if (PasswordHelper.VerifyPassword(model.NewPassword, user.Password))
                {
                    return BadRequest(new { success = false, message = "新密碼不能與目前密碼相同" });
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

                // 檢查新電話是否與當前電話相同
                if (model.NewPhone == model.CurrentPhone)
                {
                    return BadRequest(new { success = false, message = "新電話不能與目前電話相同" });
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

        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            MemberConnection connection = new MemberConnection();
            var accounts = connection.getAccounts();
            var user = accounts.FirstOrDefault(a => a.UserName == model.Email);

            if (user == null)
            {
                ViewData["IsError"] = true;
                ViewData["Message"] = "找不到此電子信箱";
                return View(model);
            }

            // 產生新密碼
            string newPassword = GenerateRandomPassword();

            // 更新資料庫中的密碼
            user.Password = PasswordHelper.HashPassword(newPassword);
            connection.UpdatePassword(user.MemberNo, user.Password);

            // 發送郵件
            try
            {
                using (var client = new SmtpClient("smtp.gmail.com", 587))
                {
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential("hotellazzydog@gmail.com", "lbiu mbvn zdsj zxei");

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress("hotellazzydog@gmail.com", "四季飯店"),
                        Subject = "四季飯店會員密碼重置",
                        Body = 

                        $@"<html>
                            <body style='font-family: Arial, sans-serif; font-size: 18px;line-height: 1.6; color: #333;'>
                                <p>親愛的四季飯店會員您好，：</p>

                                <p>您的新密碼已經重置成功，請使用以下密碼登入四季飯店訂房系統：</p>

                                <p style='font-size: 24px; color: #ff4500; font-weight: bold;'>
                                    {newPassword}
                                </p>

                                <p>
                                    為了保護您的帳號安全，請您登入後盡快修改密碼。<br>
                                    建議您將密碼設為包含大寫字母、小寫字母、數字及特殊符號的組合，
                                    並避免使用過於簡單的密碼。
                                </p>

                                <p>
                                    您可以點擊以下連結登入系統：<br>
                                    <a href='https://hotelfront.lazzydog.store:9985/Member/SignIn' 
                                       style='color: #1a0dab; text-decoration: none; font-size: 24px;'>
                                        點擊這裡登入系統
                                    </a>
                                </p>

                                <p>
                                    若您並未申請密碼重置，請立即與飯店聯繫，避免帳號遭到未授權的使用。
                                </p>

                                <p>
                                    感謝您的使用，祝您順心！
                                </p>

                                <p>四季飯店系統管理團隊 敬上</p>
                            </body>
                            </html>",
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(model.Email);

                    await client.SendMailAsync(mailMessage);
                }

                TempData["SignInMessage"] = "新密碼已寄送至您的信箱";
                return RedirectToAction("SignIn");
            }
            catch (Exception ex)
            {
                ViewData["IsError"] = true;
                ViewData["Message"] = "寄送郵件時發生錯誤，請稍後再試";
                return View(model);
            }
        }

        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            var random = new Random();
            var password = new string(Enumerable.Repeat(chars, 12)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            // 確保至少包含一個數字和一個字母
            if (!password.Any(char.IsDigit) || !password.Any(char.IsLetter))
            {
                return GenerateRandomPassword();
            }

            return password;
        }

    }
}