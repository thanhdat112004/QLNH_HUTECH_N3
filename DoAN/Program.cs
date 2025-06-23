using DoAN.Data;
using DoAN.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1) Cấu hình EF Core với SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// 2) Cấu hình Session (nếu cần lưu giỏ hàng / đặt bàn tạm thời)
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

// 4) Bind cấu hình SMTP từ appsettings.json vào SmtpSettings
builder.Services.Configure<SmtpSettings>(
    builder.Configuration.GetSection("Smtp")
);

// 5) Đăng ký Email sender
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();

// 6) Đăng ký MVC (Controllers + Views)
builder.Services.AddControllersWithViews();

var app = builder.Build();

// 7) Exception handler & HSTS (chỉ bật HSTS khi production)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// 8) Middleware pipeline
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// 9) Session phải nằm trước Authentication nếu bạn dùng Session trong Auth flow
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// 10) Map default controller route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();
