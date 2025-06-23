using DoAN.Data;
using DoAN.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1) Cấu hình EF Core với SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// 2) Cấu hình Session (nếu cần dùng cho giỏ hàng hoặc đặt bàn tạm thời)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opts =>
{
    opts.IdleTimeout = TimeSpan.FromHours(1);
    opts.Cookie.HttpOnly = true;
    opts.Cookie.IsEssential = true;
});

// 3) Cấu hình Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opts =>
    {
        opts.LoginPath = "/Account/Login";          // Trang đăng nhập
        opts.LogoutPath = "/Account/Logout";        // Trang đăng xuất
        opts.AccessDeniedPath = "/Account/AccessDenied"; // Trang truy cập bị từ chối
        opts.ExpireTimeSpan = TimeSpan.FromHours(2); // Thời gian hết hạn của cookie
    });

// 4) Cấu hình SMTP cho email gửi đi (nếu có sử dụng)
builder.Services.Configure<SmtpSettings>(
    builder.Configuration.GetSection("Smtp")
);
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();

// 5) Đăng ký MVC và cấu hình Razor View Location
builder.Services
    .AddControllersWithViews()
    .AddRazorOptions(opts =>
    {
        // Tìm các view trong thư mục Admin/{Controller}/{View}.cshtml
        opts.ViewLocationFormats.Insert(0, "/Views/Admin/{1}/{0}.cshtml");
        // Tìm các layout hoặc partials trong thư mục Admin/Shared/{View}.cshtml
        opts.ViewLocationFormats.Insert(1, "/Views/Admin/Shared/{0}.cshtml");
    });

var app = builder.Build();

// 6) Cấu hình Exception Handling và HSTS (chỉ dùng khi không phải môi trường phát triển)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();  // HTTP Strict Transport Security (Chỉ bật khi môi trường là production)
}

// 7) Middleware pipeline
app.UseHttpsRedirection();  // Chuyển hướng tất cả truy cập HTTP sang HTTPS
app.UseStaticFiles();       // Cung cấp các tệp tĩnh (CSS, JS, hình ảnh)
app.UseRouting();           // Cho phép định tuyến (routing) trong ứng dụng

app.UseSession();           // Sử dụng session nếu cần thiết (đối với giỏ hàng, đặt bàn...)
app.UseAuthentication();    // Kích hoạt xác thực
app.UseAuthorization();     // Kích hoạt phân quyền

// 8) Cấu hình route cho Admin
// Đảm bảo chỉ những controller thuộc admin mới được vào route /Admin/{controller}/{action}
app.MapControllerRoute(
    name: "admin", 
    pattern: "Admin/{controller}/{action}/{id?}",
    constraints: new
    {
        controller = "Dashboard|Employee|Menu|Reservation|Order|Invoice"  // Các controller quản lý admin
    }
);

// 9) Route mặc định cho các controller khác (Home, Account,...)
app.MapControllerRoute(
    name: "default", 
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();
