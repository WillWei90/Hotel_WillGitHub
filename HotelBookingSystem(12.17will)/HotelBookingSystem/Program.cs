using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using HotelBookingSystem.Models;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddSession();
builder.Services.AddDistributedMemoryCache();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Member/SignIn";
        options.AccessDeniedPath = "/Member/SignIn";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });

// 添加 Session 支援
builder.Services.AddDistributedMemoryCache(); // 使用內存作為 Session 存儲
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // 設置 Session 超時時間
    options.Cookie.HttpOnly = true; // 僅允許 HTTP 使用 Cookie
    options.Cookie.IsEssential = true; // 標記為必要 Cookie
});

// 設置資料庫連接
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<HotelDbContext>(options =>
   options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 添加 HttpContextAccessor 支援
builder.Services.AddHttpContextAccessor();

// 添加日誌記錄
builder.Services.AddLogging();

// 註冊控制器和視圖服務
builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

void ConfigureServices(IServiceCollection services)
{
    services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
            options.SlidingExpiration = true; // ¨C¦¸¬¡°Ê³£·|­«¸m­p®É
            options.LoginPath = "/Member/SignIn"; // ¥¼±ÂÅv®É¾É¦Vµn¤J­¶
        });

    services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.Cookie.IsEssential = true;
    });

    // ¨ä¥LªA°È°t¸m...
}

void Configure(IApplicationBuilder app)
{
    // ½T«O²K¥[³o¨Ç¤¤¤¶³nÅéªº¶¶§Ç
    app.UseAuthentication();
    app.UseAuthorization();
}

app.UseHttpsRedirection();
// 啟用靜態文件和 Session
app.UseStaticFiles();
app.UseRouting();
app.UseSession(); // 啟用 Session 中間件
app.UseAuthorization();

// 配置路由
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Member}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "Home",
    pattern: "{controller=ConsoleHome}/{action=Home}/{id?}");

app.MapControllerRoute(
    name: "HomeLogin",
    pattern: "{controller=ConsoleHome}/{action=Login}/{id?}");

app.MapControllerRoute(
    name: "member",
    pattern: "{controller=ConsoleMember}/{action=MemberManagement}/{id?}");

app.MapControllerRoute(
    name: "Order",
    pattern: "{controller=ConsoleOrder}/{action=OrderManagement}/{id?}");

app.MapControllerRoute(
    name: "Room",
    pattern: "{controller=ConsoleRoom}/{action=Room}/{id?}");

app.MapControllerRoute(
    name: "member",
    pattern: "{controller=ConsoleUser}/{action=UserManagement}/{id?}");

app.Run();
