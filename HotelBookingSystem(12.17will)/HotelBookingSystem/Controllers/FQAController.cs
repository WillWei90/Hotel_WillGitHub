using System;
using System.Linq;
using HotelBookingSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingSystem.Controllers
{
    public class FQAController : Controller
    {
        private readonly HotelDbContext _context;
        private const int PageSize = 5; // 每頁顯示的項目數

        // 使用建構函式注入 HotelDbContext
        public FQAController(HotelDbContext context)
        {
            _context = context;
        }

        // 顯示篩選後的 QA（支援分頁）
        public IActionResult Index(int page = 1)
        {
            // 篩選條件：有回覆時間且已解決
            var filteredQAs = _context.QAs
                .Where(q => q.ReplyTime != null && q.Solve)
                .OrderByDescending(q => q.CreateTime);

            // 總資料筆數
            int totalCount = filteredQAs.Count();

            // 計算總頁數
            int totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);

            // 確保頁碼有效
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            // 分頁查詢
            var qaList = filteredQAs
                .Skip((page - 1) * PageSize) // 跳過前面頁數的項目
                .Take(PageSize) // 取得當前頁的項目
                .Select(q => new QA
                {
                    QaNo = q.QaNo,
                    CreateTime = q.CreateTime,
                    Question = q.Question ?? "無內容",
                    Answer = q.Answer ?? "尚未回覆",
                    Name = q.Name ?? "未知",
                    Solve = q.Solve,
                    ReplyTime = q.ReplyTime
                })
                .ToList();

            // 將分頁資訊傳送到 View
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(qaList);
        }

        // 顯示 QA 詳細資訊
        public ActionResult Details(string id)
        {
            if (id == null) return BadRequest(); // 返回 HTTP 400

            var qa = _context.QAs.Find(id);
            if (qa == null) return NotFound(); // 返回 HTTP 404

            return View(qa);
        }

        // 新增 QA - GET
        public ActionResult Create()
        {
            return View();
        }

        // 新增 QA - POST 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(string Question)
        {
            if (string.IsNullOrWhiteSpace(Question))
            {
                ModelState.AddModelError("", "問題內容不能為空");
                return RedirectToAction("Index");
            }

            // 生成新的 QA 編號，格式為 "QA----"
            int nextQaNumber = _context.QAs.Count() + 1; // 計算下一個序列號
            string newQaNo = $"QA{nextQaNumber:D4}"; // 使用格式化，確保編號為四位數

            var newQa = new QA
            {
                QaNo = newQaNo,
                QuestionNo = newQaNo,
                Question = Question,
                CreateTime = DateTime.Now,
                Solve = false // 預設未解決
            };

            _context.QAs.Add(newQa);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        // 編輯 QA - GET
        public ActionResult Edit(string id)
        {
            if (id == null) return BadRequest(); // 返回 HTTP 400

            var qa = _context.QAs.Find(id);
            if (qa == null) return NotFound(); // 返回 HTTP 404

            return View(qa);
        }

        // 編輯 QA - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(QA qa)
        {
            if (ModelState.IsValid)
            {
                var existingQa = _context.QAs.Find(qa.QaNo);
                if (existingQa == null) return NotFound(); // 返回 HTTP 404

                existingQa.Question = qa.Question;
                existingQa.Answer = qa.Answer;
                existingQa.Name = qa.Name;
                existingQa.ReplyTime = DateTime.Now;
                existingQa.Solve = qa.Solve;

                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(qa);
        }

        // 刪除 QA - GET
        public ActionResult Delete(string id)
        {
            if (id == null) return BadRequest(); // 返回 HTTP 400

            var qa = _context.QAs.Find(id);
            if (qa == null) return NotFound(); // 返回 HTTP 404

            return View(qa);
        }

        // 刪除 QA - POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            var qa = _context.QAs.Find(id);
            if (qa != null)
            {
                _context.QAs.Remove(qa);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        // Dispose 資源釋放
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
