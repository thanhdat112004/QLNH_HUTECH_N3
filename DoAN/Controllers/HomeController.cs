using System.Diagnostics;
using DoAN.Data;                   // thêm
using DoAN.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // thêm

namespace DoAN.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _db;  // thêm

        // inject AppDbContext vào đây
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

        // sửa lại Menu thành async, load MenuItems kèm Category
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

        public IActionResult Book()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

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
