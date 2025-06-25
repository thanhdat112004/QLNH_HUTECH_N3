using DoAN.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
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

        public async Task<IActionResult> Index()
        {
            var invoices = await _context.Invoices
                .Include(i => i.Order)
                .Include(i => i.InvoiceDetails)
                .ToListAsync();
            return View(invoices);
        }

        public async Task<IActionResult> Details(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.InvoiceDetails)
                    .ThenInclude(d => d.MenuItem)
                .FirstOrDefaultAsync(i => i.InvoiceId == id);
            if (invoice == null) return NotFound();
            return View(invoice);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null) return NotFound();

            var details = _context.InvoiceDetails.Where(d => d.InvoiceId == id);
            _context.InvoiceDetails.RemoveRange(details);
            _context.Invoices.Remove(invoice);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ExportToExcel()
        {
            var invoices = await _context.Invoices.Include(i => i.InvoiceDetails).ToListAsync();

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Invoices");

            sheet.Cell(1, 1).Value = "Invoice ID";
            sheet.Cell(1, 2).Value = "Customer";
            sheet.Cell(1, 3).Value = "Contact";
            sheet.Cell(1, 4).Value = "Date";
            sheet.Cell(1, 5).Value = "Total";
            sheet.Cell(1, 6).Value = "Method";

            int row = 2;
            foreach (var inv in invoices)
            {
                sheet.Cell(row, 1).Value = inv.InvoiceId;
                sheet.Cell(row, 2).Value = inv.CustomerName;
                sheet.Cell(row, 3).Value = inv.CustomerContact;
                sheet.Cell(row, 4).Value = inv.InvoiceDate.ToString("dd/MM/yyyy");
                sheet.Cell(row, 5).Value = inv.TotalAmount;
                sheet.Cell(row, 6).Value = inv.PaymentMethod;
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
