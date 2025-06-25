using System;
using System.Linq;
using System.Threading.Tasks;
using DoAN.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAN.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Route("~/Admin/Dashboard")]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _db;

        public DashboardController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            // Lấy ngày hiện tại và ngày mai
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            // Lấy danh sách đơn hàng trong ngày
            var orders = await _db.Orders
                .Where(o => o.CreatedAt >= today && o.CreatedAt < tomorrow)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .ToListAsync();

            // Tính toán dữ liệu cho ViewBag
            ViewBag.TodayOrderCount = orders.Count;
            ViewBag.TodayRevenue = orders
                .Where(o => o.Paid)
                .Sum(o => o.OrderItems.Sum(i => i.UnitPrice * i.Quantity));
            ViewBag.AvailableTablesCount = await _db.RestaurantTables
                .CountAsync(t => t.Status == "Available");
            ViewBag.OccupiedTablesCount = await _db.RestaurantTables
                .CountAsync(t => t.Status != "Available");

            // Lấy món ăn bán chạy nhất
            var topMenuItem = await _db.OrderItems
                .GroupBy(i => i.MenuItem.Name)
                .Select(g => new { Name = g.Key, Qty = g.Sum(i => i.Quantity) })
                .OrderByDescending(x => x.Qty)
                .FirstOrDefaultAsync();
            ViewBag.TopMenuItem = topMenuItem?.Name ?? "—";

            return View();
        }
    }
}