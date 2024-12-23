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

        public IActionResult Room(string keyword = "", bool? activate = null, int page = 1, int pageSize = 5)
        {
            // 驗證是否已登入
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToAction("Login", "ConsoleHome"); // 未登入時重定向到登入頁面
            }

            var query = _context.Rooms.AsQueryable();

            var totalRecords = query.Count();

            var rooms = query
                .OrderBy(r => r.RoomNo)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new Room
                {
                    RoomNo = r.RoomNo,
                    RoomName = r.RoomName ?? "Unknown",
                    Price = r.Price,
                    RoomType = r.RoomType ?? "Unspecified",
                    Address = r.Address ?? "No Address",
                    RoomContent = r.RoomContent ?? "No Content",
                    ImagePath = r.ImagePath ?? null, // 保留可能為 null
                    RoomCapacity = r.RoomCapacity ?? 0,
                    Activate = r.Activate,
                    ProfilePicture = r.ProfilePicture // 保留可能為 null
                })
                .ToList();

            ViewBag.TotalRecords = totalRecords;
            ViewBag.PageSize = pageSize;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            return View(rooms);
        }




        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Add(Room model, IFormFile ProfilePicture)
        {
            if (ModelState.IsValid)
            {
                if (ProfilePicture != null && ProfilePicture.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        ProfilePicture.CopyTo(memoryStream);
                        model.ProfilePicture = memoryStream.ToArray(); // 將圖片轉換為二進制數據存入資料庫
                    }
                }

                _context.Rooms.Add(model); // 將房間資料存入資料庫
                _context.SaveChanges();

                TempData["Success"] = "房間新增成功！";
                return RedirectToAction("Room");
            }

            TempData["Error"] = "新增失敗，請檢查輸入的資料。";
            return View(model);
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
        public IActionResult Edit(Room room, IFormFile ProfilePicture)
        {
            if (ModelState.IsValid)
            {
                // 查找要更新的房間資料
                var existingRoom = _context.Rooms.FirstOrDefault(r => r.RoomNo == room.RoomNo);

                if (existingRoom == null)
                {
                    TempData["Error"] = "找不到指定的房間資料！";
                    return RedirectToAction("Room");
                }

                // 更新資料
                existingRoom.RoomName = room.RoomName;
                existingRoom.Price = room.Price;
                existingRoom.RoomType = room.RoomType;
                existingRoom.Address = room.Address;
                existingRoom.RoomContent = room.RoomContent;
                existingRoom.Activate = room.Activate;

                // 如果有上傳新圖片，更新 ProfilePicture 欄位
                if (ProfilePicture != null && ProfilePicture.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        ProfilePicture.CopyTo(memoryStream);
                        existingRoom.ProfilePicture = memoryStream.ToArray();
                    }
                }

                // 保存更改
                _context.SaveChanges();

                TempData["Success"] = "房間修改成功！";
                return RedirectToAction("Room");
            }

            TempData["Error"] = "修改房間時出現錯誤，請檢查輸入！";
            return RedirectToAction("Room");
        }

    }
}


