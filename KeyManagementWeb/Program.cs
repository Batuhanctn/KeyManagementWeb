using Microsoft.EntityFrameworkCore;
using KeyManagementWeb.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using KeyManagementWeb.Helpers;
using KeyManagementWeb.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// Þifreli connection string'i çöz
var encryptedConnectionString = builder.Configuration.GetConnectionString("KeyManagementDB");
var connectionString = StringCipher.Decrypt(encryptedConnectionString);

builder.Services.AddDbContext<KeyManagementContext>(options =>
    options.UseSqlServer(connectionString));

// Email service'i ekle
builder.Services.AddScoped<EmailService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.Configure<IISServerOptions>(options =>
{
    options.AllowSynchronousIO = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// Content Security Policy ayarlarý
app.Use(async (context, next) =>
{
    var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    context.Items["csp-nonce"] = nonce;

    var headers = context.Response.Headers;

    if (app.Environment.IsDevelopment())
    {
        // Geliþtirme ortamýnda daha esnek CSP kurallarý
        headers.ContentSecurityPolicy =
             "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://code.jquery.com https://cdnjs.cloudflare.com; " +
            "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com https://cdnjs.cloudflare.com; " +
            "img-src 'self' data:; " +
            "font-src 'self' https://fonts.gstatic.com https://cdnjs.cloudflare.com; " +
            "connect-src 'self' ws: wss:; " +
            "frame-ancestors 'none'; " +
            "form-action 'self';";
    }
    else
    {
        // Prodüksiyon ortamýnda daha sýký CSP kurallarý
        headers.ContentSecurityPolicy =
            "default-src 'self'; " +
            $"script-src 'self' 'nonce-{nonce}' https://cdn.jsdelivr.net https://code.jquery.com https://cdnjs.cloudflare.com; " +
            "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com https://cdnjs.cloudflare.com; " +
            "img-src 'self' data:; " +
            "font-src 'self' https://fonts.gstatic.com https://cdnjs.cloudflare.com; " +
            "connect-src 'self' ws: wss:; " +
            "frame-ancestors 'none'; " +
            "form-action 'self';";
    }

    headers["X-Content-Type-Options"] = "nosniff";

    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();