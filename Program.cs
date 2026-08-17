using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<KutuphaneContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "DefaultConnection")
    )
);

builder.Services
    .AddAuthentication(
        CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Hesap/Giris";
        options.AccessDeniedPath = "/Hesap/Yetkisiz";

        options.Cookie.Name =
            "KutuphaneOtomasyonu.Oturum";

        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;

        options.ExpireTimeSpan =
            TimeSpan.FromHours(2);

        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<
    IPasswordHasher<Uyeler>,
    PasswordHasher<Uyeler>>();

var app = builder.Build();

// İlk admin hesabını güvenli şekilde oluşturur
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var context = services
        .GetRequiredService<KutuphaneContext>();

    var passwordHasher = services
        .GetRequiredService<IPasswordHasher<Uyeler>>();

    var adminEposta =
        builder.Configuration["AdminSeed:Eposta"];

    var adminSifre =
        builder.Configuration["AdminSeed:Sifre"];

    var adminAdSoyad =
        builder.Configuration["AdminSeed:AdSoyad"]
        ?? "Sistem Yöneticisi";

    if (!string.IsNullOrWhiteSpace(adminEposta) &&
        !string.IsNullOrWhiteSpace(adminSifre))
    {
        var admin = await context.Uyelers
            .FirstOrDefaultAsync(u =>
                u.Eposta == adminEposta);

        if (admin == null)
        {
            admin = new Uyeler
            {
                AdSoyad = adminAdSoyad,
                Eposta = adminEposta,
                Telefon = null,
                KayitTarihi = DateTime.Now,
                Rol = "Admin",
                Sifre = ""
            };

            admin.Sifre = passwordHasher.HashPassword(
                admin,
                adminSifre);

            context.Uyelers.Add(admin);
            await context.SaveChangesAsync();
        }
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();