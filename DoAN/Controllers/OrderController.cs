using DoAN.Data;
using DoAN.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
<<<<<<< HEAD
using System.Linq;
using System.Security.Claims;
=======
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
>>>>>>> UI

public class OrderController : Controller
{
<<<<<<< HEAD
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
=======
    [Route("[controller]/[action]")]
    public class OrderController : Controller
    {
        private readonly AppDbContext _context;

        public OrderController(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Lấy danh sách bàn còn trống để sử dụng trong giao diện (AJAX).
        /// </summary>
        [HttpGet]
        public IActionResult GetAvailableTables()
        {
            var tables = _context.RestaurantTables
                .Where(t => t.Status == "Available")
                .Select(t => new { t.TableId, t.TableNumber, t.Seats })
                .ToList();
            return Json(tables);
        }

        /// <summary>
        /// Lấy đơn DineIn chưa thanh toán của user hiện tại (nếu có).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCurrentUnpaidOrder()
        {
            if (!User.Identity.IsAuthenticated)
                return Json(null);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var order = await _context.Orders
                .Where(o => o.UserId == userId && o.OrderType == "DineIn" && !o.Paid)
                .Select(o => new { o.OrderId, o.TableId })
                .FirstOrDefaultAsync();

            return Json(order);
        }

        /// <summary>
        /// Tạo hoặc cập nhật đơn hàng (DineIn hoặc Takeaway). Nếu DineIn đã có đơn chưa thanh toán, thêm món vào đơn hiện tại.
        /// </summary>
        // Controllers/OrderController.cs

        [HttpPost]
        public async Task<IActionResult> Create(
    string cartData,
    string type,
    string customerName,
    string phone,
    string address,
    int? tableId)
        {
            if (string.IsNullOrEmpty(cartData))
                return BadRequest("Giỏ hàng trống");

            var cartItems = JsonConvert.DeserializeObject<List<CartItem>>(cartData);
            if (cartItems == null || !cartItems.Any())
                return BadRequest("Dữ liệu giỏ hàng không hợp lệ");

            // Yêu cầu đăng nhập cho DineIn
            if (type == "DineIn" && !User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account", new
                {
                    returnUrl = Url.Action("Create", new { cartData, type, customerName, phone, address, tableId })
                });
            }

            int? userId = User.Identity.IsAuthenticated
                ? int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier))
                : null;

            // **KHỞI TẠO order** ngay từ đầu để tránh unassigned-local-variable
            Order order = null;

            if (type == "DineIn")
            {
                // Tìm order DineIn chưa thanh toán
                var existing = await _context.Orders
                    .Where(o => o.UserId == userId && o.OrderType == "DineIn" && !o.Paid)
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync();

                if (existing != null && existing.TableId.HasValue)
                {
                    // Dùng lại đơn hiện có
                    order = existing;
                }
                else
                {
                    // Chọn bàn mới nếu cần
                    var assignedTableId = tableId;
                    if (!assignedTableId.HasValue)
                    {
                        var free = await _context.RestaurantTables
                            .Where(t => t.Status == "Available")
                            .FirstOrDefaultAsync();
                        if (free == null)
                            return BadRequest("Không còn bàn trống.");
                        assignedTableId = free.TableId;
                    }

                    var table = await _context.RestaurantTables.FindAsync(assignedTableId.Value);
                    if (table == null || table.Status != "Available")
                        return BadRequest("Bàn không hợp lệ.");

                    table.Status = "Occupied";
                    _context.Update(table);
                    await _context.SaveChangesAsync();

                    // Tạo order mới
                    order = new Order
                    {
                        UserId = userId,
                        OrderType = "DineIn",
                        TableId = assignedTableId,
                        Status = "Pending",
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                        Paid = false
                    };
                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();
                }
            }
            else // Takeaway
            {
                order = new Order
                {
                    UserId = userId,
                    OrderType = "Takeaway",
                    ContactInfo = $"{customerName} - {phone} - {address}",
                    Status = "Pending",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Paid = false
                };
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
            }

            // Bây giờ order đã chắc chắn được gán
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

            // Nếu là AJAX, trả về JSON
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { orderId = order.OrderId });
            }

            // Còn bình thường thì redirect như trước
            return RedirectToAction("Confirmation", new { id = order.OrderId });
        }


        /// <summary>
        /// Hiển thị chi tiết đơn hàng để xác nhận.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> Confirmation(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .Include(o => o.Table)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
                return NotFound();

            return View(order);
        }

        /// <summary>
        /// Hiển thị trang thanh toán với tổng số tiền.
        /// Nếu truyền id hoặc tableId sẽ load đúng đơn DineIn chưa thanh toán.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Payment(int? id, int? tableId)
        {
            Order order = null;

            if (id.HasValue)
            {
                order = await _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.MenuItem)
                    .FirstOrDefaultAsync(o => o.OrderId == id.Value);
            }
            else if (tableId.HasValue)
            {
                order = await _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.MenuItem)
                    .Where(o => o.OrderType == "DineIn"
                             && o.TableId == tableId.Value
                             && !o.Paid)
                    .FirstOrDefaultAsync();
            }

            if (order == null)
                return NotFound();

            ViewBag.Total = order.OrderItems.Sum(i => i.Quantity * i.UnitPrice);
            return View(order);
        }

        /// <summary>
        /// Xử lý thanh toán cho đơn hàng.
        /// </summary>
        [HttpPost("{id}")]
        public async Task<IActionResult> Pay(int id, string paymentMethod)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
                return NotFound();

            var amount = order.OrderItems.Sum(i => i.Quantity * i.UnitPrice);
            _context.Payments.Add(new Payment
            {
                OrderId = id,
                Amount = amount,
                PaymentMethod = paymentMethod,
                PaidAt = DateTime.Now
            });

            order.Paid = true;
            order.Status = "Paid";
            order.UpdatedAt = DateTime.Now;

            if (order.OrderType == "DineIn" && order.TableId.HasValue)
            {
                var table = await _context.RestaurantTables.FindAsync(order.TableId.Value);
                if (table != null)
                    table.Status = "Available";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("ThankYou");
        }

        /// <summary>
        /// Thêm món mới vào đơn hàng DineIn đang mở (hỗ trợ gọi món nhiều lần).
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CallItem(string cartData, int tableId)
        {
            if (string.IsNullOrEmpty(cartData))
                return BadRequest("Giỏ hàng trống");

            var cartItems = JsonConvert.DeserializeObject<List<CartItem>>(cartData);
            if (cartItems == null || !cartItems.Any())
                return BadRequest("Dữ liệu giỏ hàng không hợp lệ");

            var order = await _context.Orders
                .Where(o => o.TableId == tableId && o.OrderType == "DineIn" && !o.Paid)
                .FirstOrDefaultAsync();

            if (order == null)
            {
                if (!User.Identity.IsAuthenticated)
                    return Unauthorized();

                order = new Order
                {
                    UserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)),
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

            var table = await _context.RestaurantTables.FindAsync(tableId);
            if (table != null && table.Status != "Occupied")
            {
                table.Status = "Occupied";
                _context.RestaurantTables.Update(table);
                await _context.SaveChangesAsync();
            }

            return Ok(new { success = true, message = "Gọi món thành công", orderId = order.OrderId });
        }

        /// <summary>
        /// Hiển thị trang cảm ơn sau khi thanh toán.
        /// </summary>
        [HttpGet]
        public IActionResult ThankYou()
        {
            return View();
        }


        /// <summary>
        /// Định nghĩa mô hình giỏ hàng.
        /// </summary>
        public class CartItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public decimal Price { get; set; }
            public int Quantity { get; set; }
            public string Image { get; set; }
        }

    }
   
    // helper class
    public class CartItem
    {
        public int Id { get; set; }
        public int Qty { get; set; }

>>>>>>> UI
    }
}
