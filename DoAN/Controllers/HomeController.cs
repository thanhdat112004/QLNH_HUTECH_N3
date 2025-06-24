using System.Diagnostics;
using DoAN.Data;
using DoAN.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAN.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _db;

        // Inject AppDbContext vào constructor
        public HomeController(ILogger<HomeController> logger, AppDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // Menu - lấy danh sách các món ăn
        public async Task<IActionResult> Menu()
        {
            // 1) Đổ danh sách category lên ViewBag để vẽ các filter button
            ViewBag.Categories = await _db.Categories
                                         .OrderBy(c => c.Name)
                                         .ToListAsync();

            var items = await _db.MenuItems
                                 .Include(m => m.Category)
                                 .ToListAsync();

            return View(items);
        }

        // Bàn trống - lấy danh sách các bàn có trạng thái "Available"
        [HttpGet]
        public async Task<IActionResult> GetAvailableTables()
        {
            // Lấy danh sách bàn trống (status = Available)
            var tables = await _db.RestaurantTables
                                   .Where(t => t.Status == "Available")
                                   .Select(t => new { t.TableId, t.TableNumber })
                                   .ToListAsync();

            return Json(tables);
        }

        // Đặt bàn - hiển thị form đặt bàn
        public IActionResult Book()
        {
            return View();
        }

        // Giới thiệu - trang giới thiệu
        public IActionResult About()
        {
            return View();
        }

        // Xử lý lỗi
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
