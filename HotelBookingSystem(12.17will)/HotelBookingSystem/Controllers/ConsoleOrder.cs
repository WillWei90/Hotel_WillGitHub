using HotelBookingSystem.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace HotelBookingSystem.Controllers
{
    public class ConsoleOrderController : Controller
    {
        private readonly HotelDbContext _context;

        public ConsoleOrderController(HotelDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 訂單管理頁面，支援根據訂單編號和日期進行篩選
        /// </summary>
        [HttpGet]
        public IActionResult OrderManagement(int? OrderNo, DateTime? OrderDate)
        {
            // 初始化查詢
            var orders = _context.Orders.AsQueryable();

            // 訂單編號篩選
            if (OrderNo.HasValue)
            {
                orders = orders.Where(o => o.OrderNo == OrderNo.Value);
            }

            // 訂單日期篩選
            if (OrderDate.HasValue)
            {
                orders = orders.Where(o => o.OrderDate.Date == OrderDate.Value.Date); // 比較日期部分
            }

            // 加載數據並填充房間名稱
            var orderList = orders
                .ToList()
                .Select(o =>
                {
                    o.RoomName = _context.Rooms
                        .Where(r => r.RoomNo == o.RoomNo)
                        .Select(r => r.RoomName)
                        .FirstOrDefault() ?? "無";
                    return o;
                })
                .ToList();

            // 將篩選條件存儲到 ViewData 中
            ViewData["OrderNo"] = OrderNo;
            ViewData["OrderDate"] = OrderDate?.ToString("yyyy-MM-dd");

            return View(orderList);
        }
    }
}
