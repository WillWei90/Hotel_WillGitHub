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

        [HttpPost]
        public IActionResult Edit(Room room, IFormFile ProfilePicture, string ProfilePictureDefault)
        {
            try
            {
                // 打印所有接收到的字段值
                Console.WriteLine("Received form data:");
                Console.WriteLine($"RoomNo: {room.RoomNo}");
                Console.WriteLine($"RoomName: {room.RoomName}");
                Console.WriteLine($"Price: {room.Price}");
                Console.WriteLine($"RoomType: {room.RoomType}");
                Console.WriteLine($"Address: {room.Address}");
                Console.WriteLine($"RoomContent: {room.RoomContent}");
                Console.WriteLine($"Activate: {room.Activate}");
                Console.WriteLine($"ProfilePicture: {(ProfilePicture != null ? "Uploaded" : "Not Uploaded")}");
                Console.WriteLine($"ProfilePictureDefault: {ProfilePictureDefault}");

                // 检查模型是否有效
                if (!ModelState.IsValid)
                {
                    // 如果 ProfilePicture 和 ProfilePictureDefault 都为空，移除验证错误
                    if (ProfilePicture == null && string.IsNullOrWhiteSpace(ProfilePictureDefault))
                    {
                        ModelState.Remove("ProfilePicture");
                        ModelState.Remove("ProfilePictureDefault");
                    }

                    // 检查模型验证
                    if (!ModelState.IsValid)
                    {
                        // 如果 ProfilePictureDefault 是非必填项，则移除验证错误
                        if (string.IsNullOrWhiteSpace(ProfilePictureDefault))
                        {
                            ModelState.Remove("ProfilePictureDefault");
                        }

                        // 如果验证仍然失败，打印错误信息
                        if (!ModelState.IsValid)
                        {
                            Console.WriteLine("ModelState is invalid. Errors:");
                            foreach (var state in ModelState)
                            {
                                if (state.Value.Errors.Any())
                                {
                                    Console.WriteLine($"Key: {state.Key}");
                                    foreach (var error in state.Value.Errors)
                                    {
                                        Console.WriteLine($"Error: {error.ErrorMessage}");
                                    }
                                }
                            }

                            TempData["Error"] = "提交的数据无效，请检查后重试！";
                            return RedirectToAction("Room");
                        }
                    }
                }

                // 查找要更新的房间数据
                var existingRoom = _context.Rooms.FirstOrDefault(r => r.RoomNo == room.RoomNo);

                if (existingRoom == null)
                {
                    TempData["Error"] = "找不到指定的房间数据！";
                    return RedirectToAction("Room");
                }

                // 更新房间数据
                existingRoom.RoomName = room.RoomName;
                existingRoom.Price = room.Price;
                existingRoom.RoomType = room.RoomType;
                existingRoom.Address = room.Address;
                existingRoom.RoomContent = room.RoomContent;
                existingRoom.Activate = room.Activate;

                // 图片更新逻辑
                if (ProfilePicture != null && ProfilePicture.Length > 0)
                {
                    // 上传新图片
                    using (var memoryStream = new MemoryStream())
                    {
                        ProfilePicture.CopyTo(memoryStream);
                        existingRoom.ProfilePicture = memoryStream.ToArray();
                    }
                    Console.WriteLine("ProfilePicture updated from uploaded file.");
                }
                else if (!string.IsNullOrWhiteSpace(ProfilePictureDefault))
                {
                    // 从默认值更新图片
                    existingRoom.ProfilePicture = Convert.FromBase64String(ProfilePictureDefault.Split(',')[1]);
                    Console.WriteLine("ProfilePicture set from ProfilePictureDefault.");
                }
                else
                {
                    // 未上传新图片且没有默认值时保留原始图片
                    Console.WriteLine("ProfilePicture not updated. Existing value retained.");
                }

                // 保存更改
                _context.SaveChanges();
                TempData["Success"] = "房间修改成功！";
                return RedirectToAction("Room");
            }
            catch (Exception ex)
            {
                // 打印异常信息
                Console.WriteLine($"Error while editing room: {ex.Message}");
                TempData["Error"] = "修改房间时发生未知错误，请稍后再试！";
                return RedirectToAction("Room");
            }
        }





        public IActionResult GetRoom(int roomNo)
        {
            var room = _context.Rooms.FirstOrDefault(r => r.RoomNo == roomNo);
            if (room == null)
            {
                return NotFound();
            }

            var roomData = new
            {
                RoomNo = room.RoomNo,
                RoomName = room.RoomName,
                Price = room.Price,
                RoomType = room.RoomType,
                Address = room.Address,
                RoomContent = room.RoomContent,
                Activate = room.Activate,
                ProfilePicture = room.ProfilePicture != null ? Convert.ToBase64String(room.ProfilePicture) : null
            };

            return Json(roomData);
        }




    }
}

