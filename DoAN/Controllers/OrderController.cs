using DoAN.Data;
using DoAN.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DoAN.Controllers
{
    [Route("[controller]/[action]")]
    public class OrderController : Controller
    {
        private readonly AppDbContext _context;
        public OrderController(AppDbContext context)
            => _context = context ?? throw new ArgumentNullException(nameof(context));

        [HttpGet]
        public IActionResult GetAvailableTables()
        {
            var tables = _context.RestaurantTables
                .Where(t => t.Status == "Available")
                .Select(t => new { t.TableId, t.TableNumber, t.Seats })
                .ToList();
            return Json(tables);
        }

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

        [HttpPost]
        public async Task<IActionResult> Create(
            string cartData,
            string type,
            string customerName,
            string phone,
            string address,
            int? tableId)
        {
            if (string.IsNullOrWhiteSpace(cartData))
                return BadRequest("Giỏ hàng trống");

            var cartItems = JsonConvert.DeserializeObject<List<CartItem>>(cartData);
            if (cartItems == null || !cartItems.Any())
                return BadRequest("Dữ liệu giỏ hàng không hợp lệ");

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

            Order order = null;

            if (type == "DineIn")
            {
                var existing = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o =>
                        o.UserId == userId &&
                        o.OrderType == "DineIn" &&
                        !o.Paid);

                if (existing != null && existing.TableId.HasValue)
                {
                    order = existing;
                }
                else
                {
                    var assignedTableId = tableId;
                    if (!assignedTableId.HasValue)
                    {
                        var free = await _context.RestaurantTables
                            .FirstOrDefaultAsync(t => t.Status == "Available");
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
            else
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

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { orderId = order.OrderId });
            }

            return RedirectToAction("Confirmation", new { id = order.OrderId });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Confirmation(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.MenuItem)
                .Include(o => o.Table)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null) return NotFound();
            return View(order);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Payment(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.MenuItem)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null) return NotFound();
            ViewBag.Total = order.OrderItems.Sum(i => i.Quantity * i.UnitPrice);
            return View(order);
        }

        [HttpPost("{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(int id, string paymentMethod)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == id);
            if (order == null) return NotFound();

            var amount = order.OrderItems.Sum(i => i.Quantity * i.UnitPrice);
            _context.Payments.Add(new Payment
            {
                OrderId = id,
                Amount = amount,
                PaymentMethod = paymentMethod,
                PaidAt = DateTime.Now
            });

            // Tạo hóa đơn
            var contactParts = (order.ContactInfo ?? "").Split(" - ");
            var customerName = contactParts.ElementAtOrDefault(0) ?? "";
            var customerContact = contactParts.ElementAtOrDefault(1) ?? "";

            var invoice = new Invoice
            {
                OrderId = order.OrderId,
                UserId = order.UserId ?? 0,
                CustomerName = customerName,
                CustomerContact = customerContact,
                OrderType = order.OrderType,
                TableId = order.TableId,
                InvoiceDate = DateTime.Now,
                TotalAmount = amount,
                PaymentMethod = paymentMethod
            };

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            foreach (var item in order.OrderItems)
            {
                _context.InvoiceDetails.Add(new InvoiceDetail
                {
                    InvoiceId = invoice.InvoiceId,
                    MenuItemId = item.MenuItemId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    LineTotal = item.Quantity * item.UnitPrice
                });
            }

            order.Paid = true;
            order.Status = "Paid";
            order.UpdatedAt = DateTime.Now;

            if (order.OrderType == "DineIn" && order.TableId.HasValue)
            {
                var table = await _context.RestaurantTables.FindAsync(order.TableId.Value);
                if (table != null) table.Status = "Available";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("ThankYou");
        }

        [HttpPost]
        public async Task<IActionResult> CallItem(string cartData, int tableId)
        {
            if (string.IsNullOrWhiteSpace(cartData))
                return BadRequest("Giỏ hàng trống");

            var items = JsonConvert.DeserializeObject<List<CartItem>>(cartData);
            if (items == null || !items.Any())
                return BadRequest("Dữ liệu giỏ hàng không hợp lệ");

            var order = await _context.Orders
                .FirstOrDefaultAsync(o =>
                    o.TableId == tableId &&
                    o.OrderType == "DineIn" &&
                    !o.Paid);

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

            foreach (var item in items)
            {
                _context.OrderItems.Add(new OrderItem
                {
                    OrderId = order.OrderId,
                    MenuItemId = item.Id,
                    Quantity = item.Quantity,
                    UnitPrice = item.Price
                });
            }

            var table = await _context.RestaurantTables.FindAsync(tableId);
            if (table != null && table.Status != "Occupied")
            {
                table.Status = "Occupied";
                _context.Update(table);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, orderId = order.OrderId });
        }

        [HttpGet]
        public IActionResult ThankYou() => View();
    }

    public class CartItem
    {
        public int Id { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
