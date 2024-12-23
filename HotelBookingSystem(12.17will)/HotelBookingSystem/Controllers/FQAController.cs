using System;
using System.Data.SqlTypes;
using System.Linq;
using HotelBookingSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingSystem.Controllers
{
    public class FQAController : Controller
    {
        private readonly HotelDbContext _context;

        // 使用建構函式注入 HotelDbContext
        public FQAController(HotelDbContext context)
        {
            _context = context;
        }

        // 顯示所有 QA
       public IActionResult Index()
        {
            // 確保處理所有可能的 NULL 值
            var qaList = _context.QAs
            .OrderByDescending(q => q.CreateTime) // 按創建時間降序排列
            .Select(q => new QA
            {
                QaNo = q.QaNo,
                CreateTime = q.CreateTime, // 加入創建時間
                Question = q.Question ?? "無內容",
                Answer = q.Answer ?? "尚未回覆",
                Name = q.Name ?? "未知",
                Solve = q.Solve

            })
            .ToList(); ;


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
        public ActionResult Create(QA qa)
        {
            if (ModelState.IsValid)
            {
                qa.QaNo = Guid.NewGuid().ToString(); // 使用 GUID 生成唯一流水號
                qa.CreateTime = DateTime.Now;
                _context.QAs.Add(qa);
                _context.SaveChanges();

                return RedirectToAction("Index");
            }

            return View(qa);
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
