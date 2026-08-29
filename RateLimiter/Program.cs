using APIRateLimiter.Models;
using APIRateLimiter.Services;
using APIRateLimiter.Models;
using APIRateLimiter.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.Configure<RateLimitOptions>(
    builder.Configuration.GetSection("RateLimiting"));

builder.Services.AddSingleton<IRateLimiter, FixedWindowRateLimiter>();

var app = builder.Build();

app.UseStaticFiles();

app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
