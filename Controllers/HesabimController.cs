using System.Security.Claims;
using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using KutuphaneOtomasyonu.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KutuphaneOtomasyonu.Controllers
{
    [Authorize(Roles = "Uye")]
    public class HesabimController : Controller
    {
        private readonly KutuphaneContext _context;

        private readonly IPasswordHasher<Uyeler>
            _passwordHasher;

        public HesabimController(
            KutuphaneContext context,
            IPasswordHasher<Uyeler> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        // Üyenin yalnızca kendi ödünç işlemlerini gösterir
        public async Task<IActionResult> Islemlerim()
        {
            var uyeId = KullaniciIdGetir();

            if (uyeId == null)
            {
                return RedirectToAction(
                    "Giris",
                    "Hesap");
            }

            var islemler = await _context.OduncIslemleris
                .Include(o => o.Kitap)
                .ThenInclude(k => k.Yazar)
                .Where(o => o.UyeId == uyeId.Value)
                .OrderByDescending(o => o.VerilisTarihi)
                .ToListAsync();

            ViewBag.ToplamIslem = islemler.Count;

            ViewBag.AktifIslem = islemler.Count(o =>
                !o.TeslimEdildiMi);

            ViewBag.GecikenIslem = islemler.Count(o =>
                !o.TeslimEdildiMi &&
                o.SonTeslimTarihi.Date < DateTime.Today);

            return View(islemler);
        }

        // Şifre değiştirme sayfası
        [HttpGet]
        public IActionResult SifreDegistir()
        {
            return View(new SifreDegistirViewModel());
        }

        // Şifreyi güvenli şekilde değiştirir
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SifreDegistir(
            SifreDegistirViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var uyeId = KullaniciIdGetir();

            if (uyeId == null)
            {
                return RedirectToAction(
                    "Giris",
                    "Hesap");
            }

            var uye = await _context.Uyelers
                .FirstOrDefaultAsync(u =>
                    u.UyeId == uyeId.Value);

            if (uye == null)
            {
                return NotFound();
            }

            var mevcutSifreSonucu =
                _passwordHasher.VerifyHashedPassword(
                    uye,
                    uye.Sifre,
                    model.MevcutSifre);

            if (mevcutSifreSonucu ==
                PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(
                    "MevcutSifre",
                    "Mevcut şifreniz hatalı.");

                return View(model);
            }

            if (model.MevcutSifre == model.YeniSifre)
            {
                ModelState.AddModelError(
                    "YeniSifre",
                    "Yeni şifre mevcut şifreden farklı olmalıdır.");

                return View(model);
            }

            uye.Sifre = _passwordHasher.HashPassword(
                uye,
                model.YeniSifre);

            await _context.SaveChangesAsync();

            TempData["BasariliMesaj"] =
                "Şifreniz başarıyla değiştirildi.";

            return RedirectToAction(nameof(SifreDegistir));
        }

        private int? KullaniciIdGetir()
        {
            var uyeIdDegeri = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (int.TryParse(uyeIdDegeri, out var uyeId))
            {
                return uyeId;
            }

            return null;
        }
    }
}