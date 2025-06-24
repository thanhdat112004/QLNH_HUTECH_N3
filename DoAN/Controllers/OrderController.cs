using DoAN.Data;
using DoAN.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Linq;
using System.Security.Claims;

public class OrderController : Controller
{
    private readonly AppDbContext _context;

    public OrderController(AppDbContext context)
    {
        _context = context;
    }

    // Tạo đơn hàng (DineIn hoặc Takeaway)
    [HttpPost]
    public async Task<IActionResult> Create(string cartData, string type, string customerName, string phone, string address, int? tableId)
    {
        if (string.IsNullOrEmpty(cartData)) return BadRequest("Giỏ hàng trống");

        var cartItems = JsonConvert.DeserializeObject<List<CartItem>>(cartData);
        if (cartItems == null || !cartItems.Any()) return BadRequest("Dữ liệu giỏ hàng không hợp lệ");

        int? userId = null;
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        }

        // Nếu là "DineIn", tự động chọn bàn
        if (type == "DineIn" && !tableId.HasValue)
        {
            var availableTable = await _context.RestaurantTables
                .Where(t => t.Status == "Available")
                .FirstOrDefaultAsync();

            if (availableTable != null)
            {
                tableId = availableTable.TableId;
                // Cập nhật trạng thái bàn thành "Occupied"
                availableTable.Status = "Occupied";
                await _context.SaveChangesAsync();
            }
            else
            {
                return BadRequest("Không còn bàn trống.");
            }
        }

        // Tạo đơn hàng
        var order = new Order
        {
            UserId = userId ?? 0,
            OrderType = type,
            TableId = tableId,
            ContactInfo = type == "Takeaway" ? $"{customerName} - {phone} - {address}" : null,
            Status = "Pending",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            Paid = false
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Thêm các món vào OrderItems
        foreach (var item in cartItems)
        {
            _context.OrderItems.Add(new OrderItem
            {
                OrderId = order.OrderId,
                MenuItemId = item.Id,
                Quantity = item.Quantity,
                UnitPrice = item.Price
            });
        }

        await _context.SaveChangesAsync();

        return RedirectToAction("Confirmation", new { id = order.OrderId });
    }

    // Xác nhận đơn hàng
    [HttpGet]
    public IActionResult Confirmation(int id)
    {
        var order = _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.MenuItem)
            .Include(o => o.Table)
            .FirstOrDefault(o => o.OrderId == id);

        if (order == null) return NotFound();

        return View(order);
    }

    // Thanh toán
    [HttpPost]
    public async Task<IActionResult> Pay(int id, string paymentMethod)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound();

        // Thêm bản ghi Payment vào bảng Payments
        var payment = new Payment
        {
            OrderId = order.OrderId,
            Amount = order.OrderItems.Sum(oi => oi.Quantity * oi.UnitPrice),  // Tính tổng tiền
            PaymentMethod = paymentMethod,
            PaidAt = DateTime.Now
        };

        _context.Payments.Add(payment);

        // Cập nhật trạng thái đơn hàng
        order.Paid = true;
        order.Status = "Paid";
        order.UpdatedAt = DateTime.Now;

        // Cập nhật trạng thái bàn nếu là DineIn
        if (order.OrderType == "DineIn" && order.TableId.HasValue)
        {
            var table = await _context.RestaurantTables.FindAsync(order.TableId);
            if (table != null) table.Status = "Available";
        }

        await _context.SaveChangesAsync();
        return RedirectToAction("ThankYou");
    }

    // Trang cảm ơn
    public IActionResult ThankYou()
    {
        return View();
    }

    // Gọi món cho DineIn
    [HttpPost]
    public async Task<IActionResult> CallItem(string cartData, int tableId)
    {
        if (string.IsNullOrEmpty(cartData)) return BadRequest("Giỏ hàng trống");

        var cartItems = JsonConvert.DeserializeObject<List<CartItem>>(cartData);
        if (cartItems == null || !cartItems.Any()) return BadRequest("Dữ liệu giỏ hàng không hợp lệ");

        var order = _context.Orders.FirstOrDefault(o => o.TableId == tableId && o.OrderType == "DineIn" && o.Paid == false);

        if (order == null)
        {
            order = new Order
            {
                UserId = 0,
                OrderType = "DineIn",
                TableId = tableId,
                Status = "Pending",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Paid = false
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
        }

        foreach (var item in cartItems)
        {
            _context.OrderItems.Add(new OrderItem
            {
                OrderId = order.OrderId,
                MenuItemId = item.Id,
                Quantity = item.Quantity,
                UnitPrice = item.Price
            });
        }

        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Gọi món thành công", orderId = order.OrderId });
    }


    // Định nghĩa lớp CartItem để sử dụng trong giỏ hàng
    public class CartItem
    {
        public int Id { get; set; }             // ID của món ăn (MenuItemId)
        public string Name { get; set; }         // Tên món ăn
        public decimal Price { get; set; }       // Giá món ăn
        public int Quantity { get; set; }        // Số lượng món ăn trong giỏ
        public string Image { get; set; }        // Hình ảnh món ăn (nếu có)
    }
}
