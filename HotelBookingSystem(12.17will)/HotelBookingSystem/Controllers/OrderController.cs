using Microsoft.AspNetCore.Mvc;
using HotelBookingSystem.Models;

using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Net;

namespace HotelBookingSystem.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly HotelDbContext _context;

        public OrderController(HotelDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // 顯示可用房間列表     
        [Authorize]
        public IActionResult RoomList()
        {
            try
            {
                if (_context.Rooms == null || !_context.Rooms.Any())
                {
                    ViewBag.Message = "目前沒有可用的房間。";
                    return View(Enumerable.Empty<Room>()); // 傳遞空的列表給視圖
                }

                var rooms = _context.Rooms
                    .Where(r => r.Activate) // 過濾啟用的房間
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
                }); // 返回錯誤頁面
            }
        }


        [Authorize]
        [HttpGet]
        [Route("Order/ReserveRoomForm/{roomNo}")]
        public IActionResult ReserveRoomForm(int roomNo)
        {
            var room = _context.Rooms.FirstOrDefault(r => r.RoomNo == roomNo);
            if (room == null)
            {
                return NotFound("房間不存在。");
            }

            var model = new ReserveRoomViewModel
            {
                RoomNo = room.RoomNo,
                RoomName = room.RoomName,
                Price = room.Price
            };

            return View(model);
        }

        // 預訂房間
        [Authorize]
        [HttpPost]
        public IActionResult ReserveRoom(ReserveRoomViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("ReserveRoomForm", model);
            }

            var memberIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (memberIdClaim == null)
            {
                TempData["ErrorMessage"] = "無法識別會員資訊，請重新登入。";
                return RedirectToAction("SignIn", "Member");
            }

            int memberId = int.Parse(memberIdClaim.Value);

            var isAvailable = !_context.Orders.Any(o => o.RoomNo == model.RoomNo && !o.Cancel &&
                ((model.StartDate >= o.StartDate && model.StartDate < o.EndDate) ||
                 (model.EndDate > o.StartDate && model.EndDate <= o.EndDate) ||
                 (model.StartDate <= o.StartDate && model.EndDate >= o.EndDate)));

            if (!isAvailable)
            {
                TempData["ErrorMessage"] = "選擇的房間在該時間段內不可用。";
                return RedirectToAction("RoomList");
            }

            var room = _context.Rooms.FirstOrDefault(r => r.RoomNo == model.RoomNo);
            if (room == null)
            {
                TempData["ErrorMessage"] = "無法找到房間資訊。";
                return RedirectToAction("RoomList");
            }

            int totalNights = Math.Max((model.EndDate - model.StartDate).Days, 1);
            decimal totalAmount = room.Price * totalNights;

            var order = new Order
            {
                RoomNo = model.RoomNo,
                MemberNo = memberId,
                OrderDate = DateTime.Now,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                IsPay = false,
                Cancel = false,
                TotalAmount = totalAmount
            };

            _context.Orders.Add(order);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "預訂成功！可至查詢訂單處查看您的訂單。";

            // **新增郵件寄送邏輯**
            try
            {
                var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value; // 獲取使用者的 Email
                if (!string.IsNullOrEmpty(userEmail))
                {
                    string subject = "訂房成功通知";
                    string body = $@"
                        <html>
                            <body style='font-family: Arial, sans-serif; font-size: 18px; line-height: 1.6; color: #333;'>
                                <p>親愛的四季飯店會員您好：</p>
                                <p>感謝您選擇四季飯店，您的訂房已成功！以下是您的訂房詳細資訊：</p>
                                <ul>
                                    <li>訂房編號：{order.OrderNo}</li>
                                    <li>入住日期：{order.StartDate:yyyy-MM-dd}</li>
                                    <li>退房日期：{order.EndDate:yyyy-MM-dd}</li>
                                    <li>房型：{room.RoomName}</li>
                                    <li>總金額：{order.TotalAmount:C}</li>
                                </ul>
                                <p>期待您的光臨！如有任何問題，請隨時與我們聯繫。</p>
                                <p>四季飯店 敬上</p>
                            </body>
                        </html>";

                    using (var client = new SmtpClient("smtp.gmail.com", 587))
                    {
                        client.EnableSsl = true;
                        client.Credentials = new NetworkCredential("hotellazzydog@gmail.com", "lbiu mbvn zdsj zxei");

                        var mailMessage = new MailMessage
                        {
                            From = new MailAddress("hotellazzydog@gmail.com", "四季飯店"),
                            Subject = subject,
                            Body = body,
                            IsBodyHtml = true
                        };
                        mailMessage.To.Add(userEmail);

                        client.Send(mailMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"訂房成功，但郵件通知失敗：{ex.Message}";
            }

            return RedirectToAction("RoomList");
        }

        // 查詢當前登入會員的訂單
        [Authorize]
        public IActionResult OrderList()
        {
            try
            {
                if (_context.Orders == null)
                {
                    return View("Error", new ErrorViewModel
                    {
                        Message = "資料庫中找不到訂單表。",
                        Details = "請確認資料庫是否正確設置。",
                        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
                    });
                }

                //取得目前使用者的會員編號
                var memberIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                if (memberIdClaim == null)
                {
                    TempData["ErrorMessage"] = "無法識別會員資訊，請重新登入。";
                    return RedirectToAction("SignIn", "Member");
                }

                int memberId = int.Parse(memberIdClaim.Value);

                //查詢訂單並載入房間資料
                var orders = _context.Orders
                    .Include(o => o.Room)
                    .Where(o => o.MemberNo == memberId)
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();

                // 確保每個訂單都有 RoomName
                foreach (var order in orders)
                {
                    order.RoomName = order.Room?.RoomName ?? "未知房間";
                }


                if (!orders.Any())
                {
                    ViewBag.Message = "目前沒有任何訂單。";
                }

                return View(orders);
            }
            catch (Exception ex)
            {
                return View("Error", new ErrorViewModel
                {
                    Message = "發生錯誤，無法載入訂單列表。",
                    Details = ex.Message,
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
                });
            }
        }
                
        [Authorize]
        public IActionResult OrderDetails(int orderNo)
        {
            try
            {
                if (_context.Orders == null)
                {
                    TempData["ErrorMessage"] = "資料庫連線異常，無法查詢訂單。";
                    return RedirectToAction("OrderList");
                }

                //查詢訂單
                var order = _context.Orders
                    .Include(o => o.Room)
                    .FirstOrDefault(o => o.OrderNo == orderNo);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "找不到指定的訂單。";
                    return RedirectToAction("OrderList");
                }

                var room = _context.Rooms.FirstOrDefault(r => r.RoomNo == order.RoomNo);

                var model = new OrderDetailsViewModel
                {
                    OrderNo = order.OrderNo,
                    RoomName = order.Room?.RoomName ?? "未知房間",
                    Price = room?.Price ?? 0,
                    StartDate = order.StartDate,
                    EndDate = order.EndDate,
                    IsPay = order.IsPay,
                    Cancel = order.Cancel,
                    TotalAmount = order.TotalAmount
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"查詢訂單時發生錯誤：{ex.Message}";
                return RedirectToAction("OrderList");
            }
        }


        [Authorize]
        [HttpGet]
        [Route("Order/CheckRoomAvailability/{roomNo}")]
        public IActionResult CheckRoomAvailability(int roomNo)
        {
            try
            {
                DateTime now = DateTime.Now.Date; // 取得今天的日期
                DateTime threeMonthsLater = now.AddMonths(3); // 未來三個月的日期範圍

                // 查詢符合條件的訂單，篩選出退房日期至少在今天之後，並且入住日期不晚於三個月後的訂單
                var orders = _context.Orders
                    .Where(o => o.RoomNo == roomNo && !o.Cancel &&
                                (o.StartDate <= threeMonthsLater && o.EndDate >= now))
                    .OrderBy(o => o.StartDate) // 按照入住日期排序
                    .ToList();

                // 建立一個清單存放所有已被預訂的日期
                var bookedDates = new List<string>();

                // 將每筆訂單的日期範圍展開成單日，並新增到 bookedDates 中
                foreach (var order in orders)
                {
                    for (var date = order.StartDate.Date; date <= order.EndDate.Date; date = date.AddDays(1))
                    {
                        if (date >= now && date <= threeMonthsLater) // 確保只顯示今天到三個月內的日期
                        {
                            bookedDates.Add(date.ToString("yyyy-MM-dd"));
                        }
                    }
                }

                // 返回已被預訂的日期
                return Json(new { success = true, bookings = bookedDates.Distinct().OrderBy(d => d).ToList() });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }); // 返回錯誤訊息
            }
        }

        [Authorize]
        [HttpPost]
        public IActionResult CancelOrder(int orderNo)
        {
            try
            {
                var order = _context.Orders.FirstOrDefault(o => o.OrderNo == orderNo);

                if (order == null || order.Cancel)
                {
                    TempData["ErrorMessage"] = "訂單不存在或已取消。";
                    return RedirectToAction("OrderList");
                }

                order.Cancel = true;
                _context.Orders.Update(order);
                _context.SaveChanges();

                TempData["SuccessMessage"] = $"訂單編號 {orderNo} 已成功取消。";
                return RedirectToAction("OrderList");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"取消訂單時發生錯誤：{ex.Message}";
                return RedirectToAction("OrderList");
            }
        }

        // 取得購物車內容
        [Authorize]
        public IActionResult Cart()
        {
            try
            {
                // 取得會員的身份識別資訊
                var memberIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                if (memberIdClaim == null)
                {
                    TempData["ErrorMessage"] = "無法識別會員資訊，請重新登入。";
                    return RedirectToAction("SignIn", "Member");
                }

                int memberId = int.Parse(memberIdClaim.Value);

                // 查詢未付款且未取消的訂單，並載入相關的房間資料
                var cartItems = _context.Orders
                    .Include(o => o.Room) // 載入與訂單關聯的房間資料
                    .Where(o => o.MemberNo == memberId && !o.IsPay && !o.Cancel)
                    .Select(o => new Order
                    {
                        OrderNo = o.OrderNo,
                        RoomName = o.Room.RoomName, // 設定 RoomName
                        StartDate = o.StartDate,
                        EndDate = o.EndDate,
                        TotalAmount = o.TotalAmount,
                        MemberNo = o.MemberNo,
                        IsPay = o.IsPay,
                        Cancel = o.Cancel
                    })
                    .ToList();

                // 計算總金額
                decimal totalAmount = cartItems.Sum(o => o.TotalAmount);

                // 使用 ViewBag 傳遞總金額到視圖
                ViewBag.TotalAmount = totalAmount;

                return View(cartItems);
            }
            catch (Exception ex)
            {
                // 如果發生錯誤，返回錯誤頁面
                return View("Error", new ErrorViewModel
                {
                    Message = "無法加載購物車。",
                    Details = ex.Message,
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
                });
            }
        }


        // 從購物車移除
        [Authorize]
        [HttpPost]
        public IActionResult RemoveFromCart(int orderNo)
        {
            try
            {
                var order = _context.Orders.FirstOrDefault(o => o.OrderNo == orderNo);

                if (order == null || order.IsPay || order.Cancel)
                {
                    TempData["ErrorMessage"] = "無法移除訂單，因為它不存在或已完成付款/取消。";
                    return RedirectToAction("Cart");
                }

                order.Cancel = true; // 將訂單標記為已取消
                _context.Orders.Update(order);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "成功從購物車中移除訂單。";
                return RedirectToAction("Cart");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"移除訂單時發生錯誤：{ex.Message}";
                return RedirectToAction("Cart");
            }
        }

        //購物車結帳功能
        [Authorize]
        [HttpPost]
        public IActionResult Checkout()
        {
            try
            {
                var memberIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                if (memberIdClaim == null)
                {
                    TempData["ErrorMessage"] = "無法識別會員資訊，請重新登入。";
                    return RedirectToAction("SignIn", "Member");
                }

                int memberId = int.Parse(memberIdClaim.Value);

                // 查詢購物車中的未付款訂單
                var cartItems = _context.Orders
                    .Where(o => o.MemberNo == memberId && !o.IsPay && !o.Cancel)
                    .ToList();

                if (!cartItems.Any())
                {
                    TempData["ErrorMessage"] = "購物車為空，無法結帳。";
                    return RedirectToAction("Cart");
                }

                // 將所有購物車項目標記為已付款
                foreach (var order in cartItems)
                {
                    order.IsPay = true;
                    _context.Orders.Update(order);
                }

                _context.SaveChanges();

                TempData["SuccessMessage"] = "結帳成功！您的訂單已完成付款。";
                return RedirectToAction("OrderList"); // 跳轉到訂單列表頁
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"結帳時發生錯誤：{ex.Message}";
                return RedirectToAction("Cart");
            }
        }


    }
}
