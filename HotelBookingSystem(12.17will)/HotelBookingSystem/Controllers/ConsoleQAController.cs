using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using HotelBookingSystem.Models;
using PagedList.Core; // 如果是 ASP.NET Core

namespace HotelBookingSystem.Controllers
{
    public class ConsoleQAController : Controller
    {
        private readonly HotelDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ConsoleQAController(HotelDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // 顯示所有 QA 問題列表
        public IActionResult Index(string tab = "all", int? page = 1)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            ViewBag.UserName = session?.GetString("UserName");

            if (string.IsNullOrEmpty(ViewBag.UserName))
            {
                return RedirectToAction("Login", "ConsoleHome");
            }

            IQueryable<QA> query;

            // 根據 tab 過濾數據
            switch (tab.ToLower())
            {
                case "solved":
                    query = _context.QAs.Where(q => q.Solve);
                    break;
                case "unsolved":
                    query = _context.QAs.Where(q => !q.Solve);
                    break;
                case "all":
                default:
                    query = _context.QAs;
                    break;
            }

            // 排序與查詢數據
            var qaList = query
                .OrderByDescending(q => q.CreateTime) // 按創建時間降序排列
                .Select(q => new QA
                {
                    QaNo = q.QaNo,
                    CreateTime = q.CreateTime, // 加入創建時間
                    Question = q.Question ?? "無內容",
                    Answer = q.Answer ?? "尚未回覆",
                    Name = q.Name ?? "未知",
                    Solve = q.Solve
                    
                });

            // 分頁邏輯
            int pageSize = 10;
            int pageNumber = page ?? 1;

            var pagedList = qaList.ToPagedList(pageNumber, pageSize);

            ViewBag.CurrentTab = tab;
            return View(pagedList);
        }






        // 回覆問題 - 一般回覆表單 (顯示特定問題的詳細資訊)
        public IActionResult Reply(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("問題編號無效");
            }

            var qa = _context.QAs.FirstOrDefault(q => q.QaNo == id);
            if (qa == null)
            {
                return NotFound("找不到指定的問題");
            }

            return View(qa); // 傳遞單一問題到視圖
        }

        // 提交回覆 - 一般方式
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reply(string id, [Bind("Answer,Name")] QA model)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("問題編號無效");
            }

            var qa = _context.QAs.FirstOrDefault(q => q.QaNo == id);
            if (qa == null)
            {
                return NotFound("找不到指定的問題");
            }

            if (ModelState.IsValid)
            {
                qa.Answer = model.Answer;
                qa.Name = model.Name;
                qa.ReplyTime = DateTime.Now;
                qa.Solve = true;

                _context.SaveChanges();
                return RedirectToAction(nameof(Index)); // 回到問題列表頁
            }

            return View(model); // 如果資料驗證失敗，返回回覆表單
        }

        // 提供 AJAX 獲取當前使用者名稱
        [HttpGet]
        public JsonResult GetUserName()
        {
            var userName = _httpContextAccessor.HttpContext?.Session?.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return Json(new { success = false, message = "使用者未登錄" });
            }
            return Json(new { success = true, userName });
        }

        // AJAX 回覆接口
        [HttpPost]
        public JsonResult ReplyAjax([FromBody] QAReplyModel reply)
        {
            var qa = _context.QAs.FirstOrDefault(q => q.QaNo == reply.QaNo);
            if (qa == null)
            {
                return Json(new { success = false, message = "找不到該問題。" });
            }

            // 更新回覆
            var userName = _httpContextAccessor.HttpContext?.Session?.GetString("UserName") ?? "匿名";
            qa.Answer = reply.Answer;
            qa.Name = userName;
            qa.ReplyTime = DateTime.Now;
            qa.Solve = true;

            _context.SaveChanges();

            return Json(new
            {
                success = true,
                answer = qa.Answer,
                name = qa.Name,
                replyTime = qa.ReplyTime?.ToString("yyyy-MM-dd HH:mm")
            });
        }



        [HttpGet]
        public IActionResult GetTabContent(string tab)
        {
            IEnumerable<QA> data;

            switch (tab)
            {
                case "solved":
                    data = _context.QAs.Where(q => q.Solve).ToList();
                    break;
                case "unsolved":
                    data = _context.QAs.Where(q => !q.Solve).ToList();
                    break;
                case "all":
                default:
                    data = _context.QAs.ToList();
                    break;
            }

            return PartialView("_QAList", data);
        }




    }
}
