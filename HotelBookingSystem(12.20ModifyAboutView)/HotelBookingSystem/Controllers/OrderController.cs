using Microsoft.AspNetCore.Mvc;
using HotelBookingSystem.Models;
using System;
using System.Linq;

namespace HotelBookingSystem.Controllers
{
    public class OrderController : Controller
    {
        private readonly HotelDbContext _context;

        public OrderController(HotelDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // 顯示可用房間列表
        public IActionResult RoomList()
        {
            try
            {
                if (_context.Rooms == null)
                {
                    return View("Error", "Rooms data is null"); // 使用 Error 視圖
                }

                var rooms = _context.Rooms
                    .Where(r => r.Activate) // 過濾啟用的房間
                    .ToList();

                if (rooms.Count == 0)
                {
                    ViewBag.Message = "No rooms available.";
                    return View(rooms); // 返回空的列表
                }

                return View("RoomList", rooms); // 返回 RoomList 視圖和房間列表
            }
            catch (Exception ex)
            {
                return View("Error", ex.Message); // 返回 Error 視圖顯示異常
            }
        }

        [HttpPost]
        public IActionResult ReserveRoom(int roomNo, DateTime startDate, DateTime endDate, int memberNo)
        {
            try
            {
                if (_context.Orders == null)
                {
                    return View("Error", "Orders data is null");
                }

                var order = new Order
                {
                    RoomNo = roomNo,
                    MemberNo = memberNo,
                    OrderDate = DateTime.Now,
                    StartDate = startDate,
                    EndDate = endDate,
                    IsPay = false,
                    Cancel = false
                };

                _context.Orders.Add(order);
                _context.SaveChanges();

                return RedirectToAction("OrderList", new { memberNo }); // 傳遞會員編號
            }
            catch (Exception ex)
            {
                return View("Error", ex.Message);
            }
        }

        public IActionResult OrderList(int memberNo)
        {
            try
            {
                // 檢查 Orders 資料表是否存在
                if (_context.Orders == null)
                {
                    return View("Error", "Orders table not found in the database.");
                }

                // 查詢所有訂單資料
                var orders = _context.Orders
                    .OrderByDescending(o => o.OrderDate) // 根據訂單日期排序 (最新在前)
                    .ToList();

                // 如果沒有訂單，顯示提示訊息
                if (!orders.Any())
                {
                    ViewBag.Message = "目前沒有任何訂單。";
                }

                return View(orders); // 返回訂單列表給 OrderList.cshtml 視圖
            }
            catch (Exception ex)
            {
                return View("Error", ex.Message); // 返回錯誤頁面
            }
        }
        public IActionResult OrderDetails(int orderNo)
        {
            try
            {
                if (_context.Orders == null)
                {
                    TempData["ErrorMessage"] = "資料庫連線異常，無法查詢訂單。";
                    return RedirectToAction("OrderList");
                }

                var order = _context.Orders.FirstOrDefault(o => o.OrderNo == orderNo);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "找不到指定的訂單。";
                    return RedirectToAction("OrderList");
                }

                var room = _context.Rooms.FirstOrDefault(r => r.RoomNo == order.RoomNo);

                var model = new OrderDetailsViewModel
                {
                    OrderNo = order.OrderNo,
                    RoomName = room?.RoomName ?? "未知房間",
                    Price = room?.Price ?? 0,
                    StartDate = order.StartDate,
                    EndDate = order.EndDate,
                    IsPay = order.IsPay,
                    Cancel = order.Cancel
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"查詢訂單時發生錯誤：{ex.Message}";
                return RedirectToAction("OrderList");
            }
        }



        [HttpPost]
        [HttpPost]
        [HttpPost]
        public IActionResult CancelOrder(int orderNo)
        {
            try
            {
                if (_context.Orders == null)
                {
                    TempData["ErrorMessage"] = "資料庫連線異常，無法取消訂單。";
                    return RedirectToAction("OrderList");
                }

                var order = _context.Orders.FirstOrDefault(o => o.OrderNo == orderNo);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "找不到指定的訂單。";
                    return RedirectToAction("OrderList");
                }

                // 標記為已取消
                order.Cancel = true; // 假設資料庫有 Cancel 欄位
                _context.Orders.Update(order); // 確保變更被追蹤
                _context.SaveChanges(); // 保存更改

                TempData["SuccessMessage"] = $"訂單編號 {orderNo} 已成功取消！";
                return RedirectToAction("OrderList");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"取消訂單時發生錯誤：{ex.Message}";
                return RedirectToAction("OrderList");
            }
        }
        // 檢查時間衝突的 API
        [HttpGet]
        [Route("Order/CheckRoomAvailability/{roomNo}")]
        public IActionResult CheckRoomAvailability(int roomNo)
        {
            try
            {
                // 查詢指定房間的所有未取消的預訂
                var orders = _context.Orders
                    .Where(o => o.RoomNo == roomNo && !o.Cancel)
                    .Select(o => new
                    {
                        StartDate = o.StartDate.ToString("yyyy-MM-dd"), // 格式化日期
                        EndDate = o.EndDate.ToString("yyyy-MM-dd")
                    })
                    .ToList();

                // 返回 JSON 格式的數據
                return Json(new { success = true, bookings = orders });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }



        [HttpGet]
        [Route("Order/ReserveRoomForm/{roomNo}")]
        public IActionResult ReserveRoomForm(int roomNo)
        {
            var room = _context.Rooms.FirstOrDefault(r => r.RoomNo == roomNo);
            if (room == null)
            {
                return Content("Error: Room not found.");
            }

            var model = new ReserveRoomViewModel
            {
                RoomNo = room.RoomNo,
                RoomName = room.RoomName,
                Price = room.Price
            };

            return View(model);
        }
        [HttpGet]
        public IActionResult GetRoomAvailability(int roomNo)
        {
            try
            {
                // 查詢指定房間的已預訂時間範圍
                var orders = _context.Orders
                    .Where(o => o.RoomNo == roomNo && !o.Cancel) // 排除已取消的訂單
                    .Select(o => new
                    {
                        StartDate = o.StartDate.ToString("yyyy-MM-dd"), // 格式化日期
                        EndDate = o.EndDate.ToString("yyyy-MM-dd")
                    })
                    .ToList();

                return Json(orders); // 返回 JSON 格式的數據
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message }); // 錯誤處理
            }
        }

        [HttpPost]
        [Route("Order/ReserveRoom")]
        public IActionResult ReserveRoom(ReserveRoomViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("ReserveRoomForm", model);
            }

            // 檢查房間是否可用
            var isAvailable = !_context.Orders.Any(o => o.RoomNo == model.RoomNo && !o.Cancel &&
                ((model.StartDate >= o.StartDate && model.StartDate < o.EndDate) ||
                 (model.EndDate > o.StartDate && model.EndDate <= o.EndDate) ||
                 (model.StartDate <= o.StartDate && model.EndDate >= o.EndDate)));

            if (!isAvailable)
            {
                TempData["ErrorMessage"] = "選擇的房間在該時間段內不可用。請選擇其他時間或房間。";
                return RedirectToAction("RoomList");
            }

            var order = new Order
            {
                RoomNo = model.RoomNo,
                MemberNo = 1, // 假設用戶ID為1，可改成登錄系統後的用戶ID
                OrderDate = DateTime.Now,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                IsPay = false,
                Cancel = false
            };

            _context.Orders.Add(order);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "預訂成功！";
            return RedirectToAction("RoomList");
        }



    }
}
