using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HotelBookingSystem.Controllers
{
    public class ConsoleReportController : Controller
    {
        private readonly ILogger<ConsoleReportController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ConsoleReportController(ILogger<ConsoleReportController> logger, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        // 預設顯示查詢頁面
        public IActionResult ConsoleReport()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            ViewBag.UserName = session?.GetString("UserName");

            // 若未登入則跳轉到登入頁面
            if (string.IsNullOrEmpty(ViewBag.UserName))
            {
                return RedirectToAction("Login", "ConsoleHome");
            }

            return View();
        }

        // 查詢圖片
        [HttpPost]
        public async Task<IActionResult> Query(DateTime startDate, DateTime endDate)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            var userName = session?.GetString("UserName");

            // 確保使用者已登入
            if (string.IsNullOrEmpty(userName))
            {
                return Unauthorized("用戶未登入");
            }

            // 將查詢條件存儲到 ViewBag
            ViewBag.StartDate = startDate.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate.ToString("yyyy-MM-dd");

            // 驗證日期範圍
            if ((endDate - startDate).TotalDays > 31)
            {
                ViewBag.ErrorMessage = "日期範圍不能超過一個月";
                return View("ConsoleReport");
            }

            try
            {
                // 構造要發送的 JSON
                var requestBody = new
                {
                    start_date = startDate.ToString("yyyy-MM-dd"),
                    end_date = endDate.ToString("yyyy-MM-dd")
                };

                // 發送 POST 請求到 Flask API
                using (var client = new HttpClient())
                {
                    string flaskApiUrl = "http://192.168.0.143:5100/generate_chart"; // 替換為 Flask API 的 URL
                    var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(flaskApiUrl, content);

                    // 確保請求成功
                    response.EnsureSuccessStatusCode();

                    // 解析 API 回應
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<dynamic>(responseContent);

                    if (result.success == true)
                    {
                        // 使用 file_id 生成圖片 URL
                        string fileId = result.file_id.ToString();
                        string imageUrl = Url.Action("ProxyImage", new { fileId });

                        // 傳遞圖片 URL 到 ViewBag
                        ViewBag.ImageUrl = imageUrl;
                        return View("ConsoleReport");
                    }
                    else
                    {
                        ViewBag.ErrorMessage = result.message.ToString();
                        return View("ConsoleReport");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"查詢失敗: {ex.Message}");
                ViewBag.ErrorMessage = "查詢過程中發生錯誤，請稍後再試。";
                return View("ConsoleReport");
            }
        }

        // 後端代理方法，用於從 Google Drive 獲取圖片
        [HttpGet]
        public async Task<IActionResult> ProxyImage(string fileId)
        {
            var fileUrl = $"https://drive.google.com/uc?id={fileId}&export=download";
            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(fileUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsByteArrayAsync();
                        return File(content, "image/jpeg"); // 根據實際檔案類型調整 MIME 類型
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"代理圖片失敗: {ex.Message}");
            }

            return NotFound("檔案無法存取");
        }
    }
}
