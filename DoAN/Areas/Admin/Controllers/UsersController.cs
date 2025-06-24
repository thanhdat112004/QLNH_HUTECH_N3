using DoAN.Data;
using DoAN.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DoAN.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Users
        public async Task<IActionResult> Index()
        {
            var adminRoleId = await _context.Roles
                .Where(r => r.Name == "Admin")
                .Select(r => r.RoleId)
                .FirstOrDefaultAsync();

            var users = await _context.Users
                .Where(u => !u.UserRoles.Any(ur => ur.RoleId == adminRoleId))
                .ToListAsync();

            return View(users);
        }

        // GET: Admin/Users/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null) return NotFound();

            var roleName = await _context.UserRoles
                .Where(ur => ur.UserId == id)
                .Include(ur => ur.Role)
                .Select(ur => ur.Role.Name)
                .FirstOrDefaultAsync();

            ViewBag.RoleName = roleName;
            return View(user);
        }

        // GET: Admin/Users/Create
        public IActionResult Create()
        {
            ViewBag.Roles = _context.Roles.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User user, string password, int roleId)
        {
            if (ModelState.IsValid && !string.IsNullOrEmpty(password) && roleId > 0)
            {
                using var sha = SHA256.Create();
                var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
                user.PasswordHash = Convert.ToHexString(hashBytes);

                user.CreatedAt = DateTime.Now;
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var userRole = new UserRole
                {
                    UserId = user.UserId,
                    RoleId = roleId,
                    AssignedAt = DateTime.Now
                };
                _context.UserRoles.Add(userRole);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("password", "Mật khẩu là bắt buộc.");
            }
            if (roleId <= 0)
            {
                ModelState.AddModelError("roleId", "Vui lòng chọn một quyền.");
            }

            ViewBag.Roles = _context.Roles.ToList();
            return View(user);
        }

        // GET: Admin/Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            int? roleId = await _context.UserRoles
                .Where(ur => ur.UserId == id)
                .Select(ur => (int?)ur.RoleId)
                .FirstOrDefaultAsync();

            ViewBag.Roles = new SelectList(await _context.Roles.ToListAsync(), "RoleId", "Name", roleId);
            ViewBag.SelectedRoleId = roleId?.ToString();

            return View(user);
        }

        // POST: Admin/Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, User user, string? newPassword, int roleId)
        {
            if (id != user.UserId) return NotFound();

            ModelState.Remove("PasswordHash");

            if (ModelState.IsValid)
            {
                var existingUser = await _context.Users.FindAsync(id);
                if (existingUser == null) return NotFound();

                existingUser.Username = user.Username;
                existingUser.FullName = user.FullName;
                existingUser.Email = user.Email;
                existingUser.EmailConfirmed = user.EmailConfirmed;

                if (!string.IsNullOrEmpty(newPassword))
                {
                    using var sha = SHA256.Create();
                    var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(newPassword));
                    existingUser.PasswordHash = Convert.ToHexString(hashBytes);
                }

                var currentRole = await _context.UserRoles
                    .FirstOrDefaultAsync(ur => ur.UserId == id);
                if (currentRole != null && currentRole.RoleId != roleId)
                {
                    currentRole.RoleId = roleId;
                    currentRole.AssignedAt = DateTime.Now;
                }
                else if (currentRole == null && roleId > 0)
                {
                    var userRole = new UserRole
                    {
                        UserId = id,
                        RoleId = roleId,
                        AssignedAt = DateTime.Now
                    };
                    _context.UserRoles.Add(userRole);
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Roles = new SelectList(await _context.Roles.ToListAsync(), "RoleId", "Name", roleId);
            ViewBag.SelectedRoleId = roleId.ToString();
            return View(user);
        }

        // GET: Admin/Users/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            return View(user);
        }

        // POST: Admin/Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                var userRoles = _context.UserRoles.Where(ur => ur.UserId == id);
                _context.UserRoles.RemoveRange(userRoles);

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}