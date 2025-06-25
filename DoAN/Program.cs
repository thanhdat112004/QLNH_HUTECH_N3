using DoAN.Data;
using DoAN.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1) Kết nối CSDL SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// 2) Cấu hình Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 3) Cấu hình Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opts =>
    {
        opts.LoginPath = "/Account/Login";
        opts.LogoutPath = "/Account/Logout";
        opts.AccessDeniedPath = "/Account/AccessDenied";
        opts.ExpireTimeSpan = TimeSpan.FromHours(2);
    });

// 4) Cấu hình gửi Email (SMTP)
builder.Services.Configure<SmtpSettings>(
    builder.Configuration.GetSection("Smtp")
);

// 5) Đăng ký dịch vụ gửi email
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();

// 6) Đăng ký MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// 7) Exception handler & HSTS
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// 8) Middleware pipeline
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();           // phải trước Auth nếu dùng session lưu thông tin đăng nhập
app.UseAuthentication();
app.UseAuthorization();

// 9) Cấu hình route hỗ trợ Area (QUAN TRỌNG CHO ADMIN)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}"
);

// 10) Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();
