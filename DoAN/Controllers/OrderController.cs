using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;              // << thêm
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DoAN.Data;
using DoAN.Models;

namespace DoAN.Controllers
{
    public class OrderController : Controller
    {
        private readonly AppDbContext _db;
        public OrderController(AppDbContext db) => _db = db;

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout()
        {
            // 1) Đọc cart từ Session
            var cartJson = HttpContext.Session.GetString("cart");
            if (string.IsNullOrEmpty(cartJson))
                return RedirectToAction("Menu", "Home");

            var cart = JsonSerializer.Deserialize<List<CartItem>>(cartJson)!;

            // 2) Lấy orderType (table/takeaway)
            var type = HttpContext.Session.GetString("orderType") ?? "takeaway";

            // 3) Lấy userId (bắt buộc phải đăng nhập)
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (claim == null)
                return RedirectToAction("Login", "Account");
            var userId = int.Parse(claim);

            // 4) Tạo Order
            var order = new Order
            {
                UserId = userId,
                OrderType = type == "table" ? "DineIn" : "Takeaway",
                TableId = type == "table" ? 1 : null,        // tùy bạn gán table
                ContactInfo = type == "takeaway" ? /* lấy email/sdt nếu cần */ "" : null,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Paid = false
            };
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            // 5) Tạo OrderItems
            foreach (var ci in cart)
            {
                var mi = await _db.MenuItems.FindAsync(ci.Id);
                if (mi == null) continue;
                _db.OrderItems.Add(new OrderItem
                {
                    OrderId = order.OrderId,
                    MenuItemId = ci.Id,
                    Quantity = ci.Qty,
                    UnitPrice = mi.Price
                });
            }
            await _db.SaveChangesAsync();

            // 6) Clear session
            HttpContext.Session.Remove("cart");

            TempData["Success"] = "Đặt món thành công!";
            return RedirectToAction("OrderResult", new { id = order.OrderId });
        }

        public IActionResult OrderResult(int id)
        {
            ViewBag.OrderId = id;
            return View();
        }
    }

    // helper class
    public class CartItem
    {
        public int Id { get; set; }
        public int Qty { get; set; }
    }
}
