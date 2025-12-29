using Microsoft.AspNetCore.Authentication.Cookies;
using OfficeOpenXml;
using QcChapWai.Data;
using QcChapWai.Services;
using System.ComponentModel;

var builder = WebApplication.CreateBuilder(args);

// เพิ่มการตั้งค่าเพื่อให้ EPPlus License
ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register SQL Helper and Services
builder.Services.AddSingleton<SqlHelper>();
builder.Services.AddScoped<MaterialService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ChecklistService>();
builder.Services.AddScoped<MaterialMasterService>(); // ✅ เพิ่มใหม่
builder.Services.AddScoped<MaterialMasterLocalService>();
builder.Services.AddScoped<ProductionOrderService>();
builder.Services.AddScoped<MachineService>();
builder.Services.AddScoped<InspectCodeService>();

// Add Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();