using ClosedXML.Excel;
using DoAN.Data;
using DoAN.Models;

using DoAN.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DoAN.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class InvoicesController : Controller
    {
        private readonly AppDbContext _context;

        public InvoicesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Invoices
        public async Task<IActionResult> Index()
        {
            var invoices = await _context.Invoices
                .Include(i => i.Order)
                .Include(i => i.InvoiceDetails)
                    .ThenInclude(d => d.MenuItem)
                .ToListAsync();
            return View(invoices);
        }

        // GET: Admin/Invoices/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var invoice = await _context.Invoices
                .Include(i => i.Order)
                .Include(i => i.InvoiceDetails)
                    .ThenInclude(d => d.MenuItem)
                .FirstOrDefaultAsync(i => i.InvoiceId == id);

            if (invoice == null) return NotFound();

            return View(invoice);
        }

        // GET: Admin/Invoices/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Invoices/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Invoice invoice)
        {
            ModelState.Remove("Order");
            ModelState.Remove("InvoiceDetails");

            if (ModelState.IsValid)
            {
                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(invoice);
        }

        // GET: Admin/Invoices/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null) return NotFound();

            var vm = new InvoiceEditViewModel
            {
                InvoiceId = invoice.InvoiceId,
                CustomerName = invoice.CustomerName,
                CustomerContact = invoice.CustomerContact,
                InvoiceDate = invoice.InvoiceDate,
                TotalAmount = invoice.TotalAmount,
                PaymentMethod = invoice.PaymentMethod
            };

            return View(vm);
        }

        // POST: Admin/Invoices/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, InvoiceEditViewModel vm)
        {
            if (id != vm.InvoiceId) return NotFound();

            if (ModelState.IsValid)
            {
                var invoice = await _context.Invoices.FindAsync(id);
                if (invoice == null) return NotFound();

                invoice.CustomerName = vm.CustomerName;
                invoice.CustomerContact = vm.CustomerContact;
                invoice.InvoiceDate = vm.InvoiceDate;
                invoice.TotalAmount = vm.TotalAmount;
                invoice.PaymentMethod = vm.PaymentMethod;

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(vm);
        }

        // GET: Admin/Invoices/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var invoice = await _context.Invoices
                .Include(i => i.InvoiceDetails)
                .FirstOrDefaultAsync(i => i.InvoiceId == id);

            if (invoice == null) return NotFound();

            return View(invoice);
        }

        // POST: Admin/Invoices/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice != null)
            {
                var details = _context.InvoiceDetails.Where(d => d.InvoiceId == id);
                _context.InvoiceDetails.RemoveRange(details);
                _context.Invoices.Remove(invoice);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Invoices/ExportToExcel
        public async Task<IActionResult> ExportToExcel()
        {
            var invoices = await _context.Invoices
                .Include(i => i.InvoiceDetails)
                    .ThenInclude(d => d.MenuItem)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Invoices");

            sheet.Cell(1, 1).Value = "Invoice ID";
            sheet.Cell(1, 2).Value = "Customer";
            sheet.Cell(1, 3).Value = "Contact";
            sheet.Cell(1, 4).Value = "Order Type";
            sheet.Cell(1, 5).Value = "Table ID";
            sheet.Cell(1, 6).Value = "Date";
            sheet.Cell(1, 7).Value = "Total";
            sheet.Cell(1, 8).Value = "Payment";

            int row = 2;
            foreach (var inv in invoices)
            {
                sheet.Cell(row, 1).Value = inv.InvoiceId;
                sheet.Cell(row, 2).Value = inv.CustomerName;
                sheet.Cell(row, 3).Value = inv.CustomerContact;
                sheet.Cell(row, 4).Value = inv.OrderType;
                sheet.Cell(row, 5).Value = inv.TableId.HasValue ? inv.TableId.ToString() : "N/A";
                sheet.Cell(row, 6).Value = inv.InvoiceDate.ToString("dd/MM/yyyy");
                sheet.Cell(row, 7).Value = inv.TotalAmount;
                sheet.Cell(row, 8).Value = inv.PaymentMethod;
                row++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Invoices.xlsx");
        }
    }
}
