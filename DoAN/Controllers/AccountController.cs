using DoAN.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using DoAN.Data;
using DoAN.Models;
using DoAN.ViewModels;

namespace DoAN.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IEmailSender _emailSender;    // ← interface của bạn
        private readonly PasswordHasher<User> _pwHasher;

        public AccountController(
            AppDbContext db,
            IEmailSender emailSender           // ← phải là DoAN.Services.IEmailSender
        )
        {
            _db = db;
            _emailSender = emailSender;
            _pwHasher = new PasswordHasher<User>();
        }
        [HttpGet]
        public IActionResult CheckLoginStatus()
        {
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var username = User.FindFirstValue(ClaimTypes.Name);
                return Content($"Chào mừng {username}, bạn đã đăng nhập với ID {userId}.");
            }
            else
            {
                return Content("Bạn chưa đăng nhập.");
            }
        }
        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
            => View(new RegisterVm());

        // POST: /Account/Register
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVm vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            // 1) Kiểm tra Username/Email chưa tồn tại
            if (await _db.Users.AnyAsync(u => u.Username == vm.Username || u.Email == vm.Email))
            {
                ModelState.AddModelError(string.Empty, "Username hoặc Email đã tồn tại.");
                return View(vm);
            }

            // 2) Tạo user mới
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

            // 3) Gán role “User” mặc định
            var defaultRole = await _db.Roles.SingleAsync(r => r.Name == "User");
            _db.UserRoles.Add(new UserRole
            {
                UserId = user.UserId,
                RoleId = defaultRole.RoleId,
                AssignedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            // 4) Tạo token xác thực và lưu
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

            // 5) Gửi email xác thực
            var confirmLink = Url.Action("ConfirmEmail", "Account", new { token }, Request.Scheme);
            var html = $"Vui lòng bấm <a href=\"{confirmLink}\">vào đây</a> để xác thực email (hết hạn sau 24h).";
            await _emailSender.SendEmailAsync(user.Email, "Xác thực tài khoản", html);

            return RedirectToAction("RegistrationSuccess");
        }


        // GET: /Account/RegistrationSuccess
        [HttpGet]
        public IActionResult RegistrationSuccess()
            => View();

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
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginVm());
        }

        // POST: /Account/Login
        // POST: /Account/Login
        [HttpPost, ValidateAntiForgeryToken]

        public async Task<IActionResult> Login(LoginVm vm, string returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var user = await _db.Users
                .SingleOrDefaultAsync(u =>
                    u.Username == vm.UsernameOrEmail ||
                    u.Email == vm.UsernameOrEmail);

            if (user == null ||
                _pwHasher.VerifyHashedPassword(user, user.PasswordHash, vm.Password) == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(string.Empty, "Sai tên đăng nhập hoặc mật khẩu.");
                return View(vm);
            }

            if (!user.EmailConfirmed)
            {
                ModelState.AddModelError(string.Empty, "Bạn phải xác thực email trước khi đăng nhập.");
                return View(vm);
            }

            // Lấy danh sách Role
            var roles = await _db.UserRoles
                .Where(ur => ur.UserId == user.UserId)
                .Select(ur => ur.Role.Name)
                .ToListAsync();

            // Tạo claims
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        new Claim(ClaimTypes.Name, user.Username)
    };

            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(claimsIdentity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // Nếu có returnUrl thì ưu tiên redirect về đó
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            // Chuyển hướng theo Role
            if (roles.Contains("Admin"))
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

            if (roles.Contains("Employee"))
                return RedirectToAction("Index", "Home"); // hoặc "Orders", "Manage"

            if (roles.Contains("User"))
                return RedirectToAction("Menu", "Home");

            // Không rõ role thì về trang chính
            return RedirectToAction("Index", "Home");
        }

    }
}
