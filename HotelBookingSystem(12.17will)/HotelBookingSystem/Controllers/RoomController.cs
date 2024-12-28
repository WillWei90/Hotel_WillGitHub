using System.Diagnostics;
using HotelBookingSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


public class RoomController : Controller
{
    private readonly HotelDbContext _context;

    public RoomController(HotelDbContext context)
    {
        _context = context;
    }

    public IActionResult RoomList()
    {
        try
        {
            if (_context.Rooms == null || !_context.Rooms.Any())
            {
                ViewBag.Message = "目前沒有可用的房間。";
                return View(Enumerable.Empty<Room>()); // 傳遞空的列表給視圖
            }

            // 查詢啟用的房間並處理圖片轉換
            var rooms = _context.Rooms
                .Where(r => r.Activate) // 過濾啟用的房間
                .Select(r => new Room
                {
                    RoomNo = r.RoomNo,
                    RoomName = r.RoomName,
                    Price = r.Price,
                    RoomType = r.RoomType,
                    Address = r.Address,
                    RoomContent = r.RoomContent,
                    Activate = r.Activate,
                    ProfilePicture = r.ProfilePicture // 圖片保持 byte[] 格式
                })
                .ToList();

            if (!rooms.Any())
            {
                ViewBag.Message = "目前沒有可用的房間。";
            }

            return View(rooms); // 傳遞房間列表給視圖
        }
        catch (Exception ex)
        {
            return View("Error", new ErrorViewModel
            {
                Message = "發生錯誤，無法載入房間列表。",
                Details = ex.Message,
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }

}
