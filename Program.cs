using App.Data;
using App.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

//
// DATABASE
//
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

//
// SESSION
//
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

//
// AUTH
//
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });

builder.Services.AddAuthorization();

//
// MVC
//
builder.Services.AddControllersWithViews();

//
// SERVICES
//
builder.Services.AddScoped<ProductImportService>();

var app = builder.Build();

//
// IMPORT PRODUCTS FROM JSON
//
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var importer =
        services.GetRequiredService<ProductImportService>();

    var env =
        services.GetRequiredService<IWebHostEnvironment>();

    var path = Path.Combine(
        env.ContentRootPath,
        "products.json"
    );

    await importer.ImportProductsAsync(path);
}

//
// PIPELINE
//
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseSession();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();