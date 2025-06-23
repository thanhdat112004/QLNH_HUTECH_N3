using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DoAN.Data;
using DoAN.Models;
using DoAN.Services;
using DoAN.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAN.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IEmailSender _emailSender;
        private readonly PasswordHasher<User> _pwHasher;

        public AccountController(AppDbContext db, IEmailSender emailSender)
        {
            _db = db;
            _emailSender = emailSender;
            _pwHasher = new PasswordHasher<User>();
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterVm());
        }

        // POST: /Account/Register
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVm vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            // Kiểm tra tồn tại
            if (await _db.Users.AnyAsync(u => u.Username == vm.Username || u.Email == vm.Email))
            {
                ModelState.AddModelError(string.Empty, "Username hoặc Email đã tồn tại.");
                return View(vm);
            }

            // Tạo user
            var user = new User
            {
                Username = vm.Username,
                Email = vm.Email,
                PasswordHash = _pwHasher.HashPassword(null, vm.Password),
                EmailConfirmed = false,
                CreatedAt = DateTime.UtcNow
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // Gán role "User"
            var defaultRole = await _db.Roles.SingleAsync(r => r.Name == "User");
            _db.UserRoles.Add(new UserRole
            {
                UserId = user.UserId,
                RoleId = defaultRole.RoleId,
                AssignedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            // Tạo token xác thực
            var token = Guid.NewGuid();
            _db.EmailVerifications.Add(new EmailVerification
            {
                UserId = user.UserId,
                Token = token,
                Type = "Registration",
                Expiration = DateTime.UtcNow.AddHours(24),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            // Gửi email xác thực
            var confirmLink = Url.Action("ConfirmEmail", "Account", new { token }, Request.Scheme);
            var html = $"Vui lòng bấm <a href=\"{confirmLink}\">vào đây</a> để xác thực email (hết hạn sau 24h).";
            await _emailSender.SendEmailAsync(user.Email, "Xác thực tài khoản", html);

            return RedirectToAction("RegistrationSuccess");
        }

        // GET: /Account/RegistrationSuccess
        [HttpGet]
        public IActionResult RegistrationSuccess()
        {
            return View();
        }

        // GET: /Account/ConfirmEmail?token=...
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(Guid token)
        {
            var ev = await _db.EmailVerifications
                .Include(e => e.User)
                .SingleOrDefaultAsync(e =>
                    e.Token == token &&
                    e.Type == "Registration" &&
                    !e.IsUsed &&
                    e.Expiration >= DateTime.UtcNow);

            if (ev == null)
                return View("ConfirmEmailFailed");

            ev.IsUsed = true;
            ev.User.EmailConfirmed = true;
            await _db.SaveChangesAsync();

            return View("ConfirmEmailSuccess");
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginVm());
        }

        // POST: /Account/Login
        
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVm vm, string returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(vm);

            // 1) Tìm user trong database
            var user = await _db.Users
                .SingleOrDefaultAsync(u =>
                    u.Username == vm.UsernameOrEmail ||
                    u.Email == vm.UsernameOrEmail);

            if (user == null ||
                _pwHasher.VerifyHashedPassword(user, user.PasswordHash, vm.Password)
                    == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(string.Empty, "Sai tên đăng nhập hoặc mật khẩu.");
                return View(vm);
            }

            if (!user.EmailConfirmed)
            {
                ModelState.AddModelError(string.Empty, "Bạn phải xác thực email trước khi đăng nhập.");
                return View(vm);
            }

            // 2) Tạo claims & sign-in
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        new Claim(ClaimTypes.Name, user.Username)
    };

            var roles = await _db.UserRoles
                                 .Where(ur => ur.UserId == user.UserId)
                                 .Select(ur => ur.Role.Name)
                                 .ToListAsync();
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var principal = new ClaimsPrincipal(
                new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)
            );
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);

            // 3) Chuyển hướng dựa vào vai trò
            if (roles.Contains("Admin"))
                return Redirect("/Admin/Dashboard/");

            if (roles.Contains("Employee"))
                return Redirect("/Admin/Order/");

            // 4) Khách thường (User) sẽ được chuyển tới trang Menu
            return RedirectToAction("Menu", "Home");
        }

    }
}
