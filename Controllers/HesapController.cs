using System.Security.Claims;
using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using KutuphaneOtomasyonu.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KutuphaneOtomasyonu.Controllers
{
    public class HesapController : Controller
    {
        private readonly KutuphaneContext _context;

        private readonly IPasswordHasher<Uyeler>
            _passwordHasher;

        public HesapController(
            KutuphaneContext context,
            IPasswordHasher<Uyeler> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        // Giriş sayfası
        [AllowAnonymous]
        public IActionResult Giris(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return KullaniciAnaSayfasinaYonlendir();
            }

            ViewBag.ReturnUrl = returnUrl;

            return View(new GirisViewModel());
        }

        // Giriş işlemi
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

            var eposta = model.Eposta.Trim();

            var uye = await _context.Uyelers
                .FirstOrDefaultAsync(u =>
                    u.Eposta == eposta);

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

            if (sifreSonucu ==
                PasswordVerificationResult.SuccessRehashNeeded)
            {
                uye.Sifre = _passwordHasher.HashPassword(
                    uye,
                    model.Sifre);

                await _context.SaveChangesAsync();
            }

            var rol = string.IsNullOrWhiteSpace(uye.Rol)
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
                CookieAuthenticationDefaults
                    .AuthenticationScheme);

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
                CookieAuthenticationDefaults
                    .AuthenticationScheme,
                principal,
                authenticationProperties);

            // Admin güvenli bir yerel adrese dönebilir
            if (rol == "Admin")
            {
                if (!string.IsNullOrWhiteSpace(returnUrl) &&
                    Url.IsLocalUrl(returnUrl))
                {
                    return LocalRedirect(returnUrl);
                }

                return RedirectToAction(
                    "Index",
                    "Home");
            }

            // Üyeler her zaman kitaplar sayfasına gider
            return RedirectToAction(
                "Index",
                "Kitaplar");
        }

        // Çıkış işlemi
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cikis()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults
                    .AuthenticationScheme);

            return RedirectToAction(nameof(Giris));
        }

        // Yetkisiz erişim sayfası
        [AllowAnonymous]
        public IActionResult Yetkisiz()
        {
            return View();
        }

        private IActionResult KullaniciAnaSayfasinaYonlendir()
        {
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction(
                    "Index",
                    "Home");
            }

            return RedirectToAction(
                "Index",
                "Kitaplar");
        }
    }
}