using System.Security.Claims;
using KutuphaneOtomasyonu.ViewModels;
using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KutuphaneOtomasyonu.Controllers;

public class HesapController : Controller
{
    private readonly KutuphaneContext _context;
    private readonly PasswordHasher<Uyeler> _passwordHasher;

    public HesapController(KutuphaneContext context)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<Uyeler>();
    }

    // Giriş sayfasını açar
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Giris(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Home");
            }

            return RedirectToAction("Index", "Kitaplar");
        }

        ViewBag.ReturnUrl = returnUrl;

        return View(new GirisViewModel());
    }

    // Giriş işlemini gerçekleştirir
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Giris(
        GirisViewModel model,
        string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        string eposta = model.Eposta.Trim();

        var uye = await _context.Uyelers
            .FirstOrDefaultAsync(u => u.Eposta == eposta);

        if (uye == null ||
            string.IsNullOrWhiteSpace(uye.Sifre))
        {
            ModelState.AddModelError(
                "",
                "E-posta veya şifre hatalı.");

            return View(model);
        }

        var sifreSonucu =
            _passwordHasher.VerifyHashedPassword(
                uye,
                uye.Sifre,
                model.Sifre);

        if (sifreSonucu ==
            PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(
                "",
                "E-posta veya şifre hatalı.");

            return View(model);
        }

        // Pasif üyelerin girişini engeller.
        // Admin hesabı bu kontrolden etkilenmez.
        if (uye.Rol != "Admin" && !uye.AktifMi)
        {
            ModelState.AddModelError(
                "",
                "Üyeliğiniz pasif durumdadır. Kütüphane yönetimiyle iletişime geçiniz.");

            return View(model);
        }

        // Şifreleme yöntemi güncellendiyse şifreyi yeniden hashler.
        if (sifreSonucu ==
            PasswordVerificationResult.SuccessRehashNeeded)
        {
            uye.Sifre = _passwordHasher.HashPassword(
                uye,
                model.Sifre);

            await _context.SaveChangesAsync();
        }

        string rol = string.IsNullOrWhiteSpace(uye.Rol)
            ? "Uye"
            : uye.Rol;

        var claims = new List<Claim>
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                uye.UyeId.ToString()),

            new Claim(
                ClaimTypes.Name,
                uye.AdSoyad),

            new Claim(
                ClaimTypes.Email,
                uye.Eposta),

            new Claim(
                ClaimTypes.Role,
                rol)
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        var authenticationProperties =
            new AuthenticationProperties
            {
                IsPersistent = model.BeniHatirla,

                ExpiresUtc = model.BeniHatirla
                    ? DateTimeOffset.UtcNow.AddDays(7)
                    : DateTimeOffset.UtcNow.AddHours(2)
            };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            authenticationProperties);

        if (rol == "Admin")
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        return RedirectToAction("Index", "Kitaplar");
    }

    // Hem eski GET bağlantılarından hem POST formundan çıkış yapılabilir.
    [AcceptVerbs("GET", "POST")]
    [Authorize]
    public async Task<IActionResult> Cikis()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction(nameof(Giris));
    }

    // Yetkisiz erişim sayfası
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Yetkisiz()
    {
        return View();
    }
}