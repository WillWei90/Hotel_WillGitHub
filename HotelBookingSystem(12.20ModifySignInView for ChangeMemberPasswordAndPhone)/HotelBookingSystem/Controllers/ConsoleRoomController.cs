using Microsoft.AspNetCore.Mvc;
using HotelBookingSystem.Models;
using System.Linq;
using System;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HotelBookingSystem.Controllers
{
    public class ConsoleRoomController : Controller
    {
        private readonly HotelDbContext _context;

        public ConsoleRoomController(HotelDbContext context)
        {
            _context = context;
        }

        public IActionResult Room(string keyword = "", bool? activate = null, int page = 1, int pageSize = 10)
        {
            //// 驗證是否已登入
            //var userName = HttpContext.Session.GetString("UserName");
            //if (string.IsNullOrEmpty(userName))
            //{
            //    return RedirectToAction("Login", "ConsoleHome"); // 未登入時重定向到登入頁面
            //}

            // 傳遞 UserName 到 Layout
            //ViewBag.UserName = userName;

            // 查詢房間數據
            var query = _context.Rooms.AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(r => r.RoomName.Contains(keyword));
            }

            if (activate.HasValue)
            {
                query = query.Where(r => r.Activate == activate.Value);
            }

            var totalRecords = query.Count();
            var rooms = query
                .OrderBy(r => r.RoomNo)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // 填充分頁屬性
            if (rooms.Any())
            {
                rooms[0].TotalRecords = totalRecords;
                rooms[0].PageSize = pageSize;
                rooms[0].CurrentPage = page;
                rooms[0].TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
            }

            return View(rooms);
        }

        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(Room room)
        {
            if (ModelState.IsValid)
            {
                _context.Rooms.Add(room);
                _context.SaveChanges();
                TempData["Success"] = "房間添加成功！";
                return RedirectToAction("Room");
            }

            TempData["Error"] = "添加房間時出現錯誤，請檢查輸入！";
            return View(room);
        }

        public IActionResult Edit(int roomNo)
        {
            var room = _context.Rooms.Find(roomNo);
            if (room == null)
            {
                TempData["Error"] = "找不到指定的房間！";
                return RedirectToAction("Room");
            }

            return View(room);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Room room)
        {
            Console.WriteLine($"RoomNo: {room.RoomNo}");
            Console.WriteLine($"RoomName: {room.RoomName}");
            Console.WriteLine($"Activate (啟用狀態): {room.Activate}");

            if (ModelState.IsValid)
            {
                var existingRoom = _context.Rooms.Find(room.RoomNo);
                if (existingRoom == null)
                {
                    TempData["Error"] = "找不到指定的房間！";
                    return RedirectToAction("Room");
                }

                // 更新房間資料
                existingRoom.RoomName = room.RoomName;
                existingRoom.Price = room.Price;
                existingRoom.RoomType = room.RoomType;
                existingRoom.Address = room.Address;
                existingRoom.RoomContent = room.RoomContent;
                existingRoom.Activate = room.Activate;

                _context.SaveChanges();
                TempData["Success"] = "房間更新成功！";
                return RedirectToAction("Room");
            }

            var errors = ModelState.Values
            .SelectMany(v => v.Errors) // 收集所有的錯誤
            .Select(e => e.ErrorMessage) // 取得錯誤訊息字串
            .ToList();
            TempData["Error"] = string.Join("; ", errors); // 將錯誤訊息用分號分隔
            //TempData["Error"] = $"修改房間時出現錯誤，請檢查輸入！{room.Activate}";
            return RedirectToAction("Room");
        }
    }
}


