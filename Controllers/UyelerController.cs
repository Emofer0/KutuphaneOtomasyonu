using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KutuphaneOtomasyonu.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UyelerController : Controller
    {
        private readonly KutuphaneContext _context;

        private readonly IPasswordHasher<Uyeler>
            _passwordHasher;

        public UyelerController(
            KutuphaneContext context,
            IPasswordHasher<Uyeler> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<IActionResult> Index(string? arama)
        {
            var uyeler = _context.Uyelers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(arama))
            {
                uyeler = uyeler.Where(u =>
                    u.AdSoyad.Contains(arama) ||
                    u.Eposta.Contains(arama) ||
                    (u.Telefon != null &&
                     u.Telefon.Contains(arama)));
            }

            ViewBag.Arama = arama;

            return View(await uyeler
                .OrderBy(u => u.AdSoyad)
                .ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var uye = await _context.Uyelers
                .Include(u => u.OduncIslemleris)
                .ThenInclude(o => o.Kitap)
                .FirstOrDefaultAsync(u => u.UyeId == id);

            if (uye == null)
            {
                return NotFound();
            }

            return View(uye);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("AdSoyad,Eposta,Telefon,Sifre")] Uyeler uye)
        {
            ModelState.Remove("Rol");
            ModelState.Remove("OduncIslemleris");

            uye.AdSoyad = uye.AdSoyad?.Trim() ?? "";
            uye.Eposta = uye.Eposta?.Trim() ?? "";

            var epostaKullaniliyor = await _context.Uyelers
                .AnyAsync(u => u.Eposta == uye.Eposta);

            if (epostaKullaniliyor)
            {
                ModelState.AddModelError(
                    "Eposta",
                    "Bu e-posta adresiyle kayıtlı bir üye bulunuyor.");
            }

            if (string.IsNullOrWhiteSpace(uye.Sifre))
            {
                ModelState.AddModelError(
                    "Sifre",
                    "Şifre zorunludur.");
            }
            else if (uye.Sifre.Length < 6)
            {
                ModelState.AddModelError(
                    "Sifre",
                    "Şifre en az 6 karakter olmalıdır.");
            }

            if (!ModelState.IsValid)
            {
                return View(uye);
            }

            uye.KayitTarihi = DateTime.Now;
            uye.Rol = "Uye";

            var acikSifre = uye.Sifre;

            uye.Sifre = _passwordHasher.HashPassword(
                uye,
                acikSifre);

            _context.Uyelers.Add(uye);
            await _context.SaveChangesAsync();

            TempData["BasariliMesaj"] =
                "Üye hesabı başarıyla oluşturuldu.";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var uye = await _context.Uyelers.FindAsync(id);

            if (uye == null)
            {
                return NotFound();
            }

            return View(uye);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("UyeId,AdSoyad,Eposta,Telefon")] Uyeler form)
        {
            if (id != form.UyeId)
            {
                return NotFound();
            }

            ModelState.Remove("Sifre");
            ModelState.Remove("Rol");
            ModelState.Remove("OduncIslemleris");

            form.AdSoyad = form.AdSoyad?.Trim() ?? "";
            form.Eposta = form.Eposta?.Trim() ?? "";

            var epostaKullaniliyor = await _context.Uyelers
                .AnyAsync(u =>
                    u.Eposta == form.Eposta &&
                    u.UyeId != form.UyeId);

            if (epostaKullaniliyor)
            {
                ModelState.AddModelError(
                    "Eposta",
                    "Bu e-posta adresi başka bir üyede kullanılıyor.");
            }

            if (!ModelState.IsValid)
            {
                return View(form);
            }

            var uye = await _context.Uyelers
                .FirstOrDefaultAsync(u => u.UyeId == id);

            if (uye == null)
            {
                return NotFound();
            }

            uye.AdSoyad = form.AdSoyad;
            uye.Eposta = form.Eposta;
            uye.Telefon = form.Telefon;

            await _context.SaveChangesAsync();

            TempData["BasariliMesaj"] =
                "Üye bilgileri güncellendi.";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var uye = await _context.Uyelers
                .FirstOrDefaultAsync(u => u.UyeId == id);

            if (uye == null)
            {
                return NotFound();
            }

            return View(uye);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var uye = await _context.Uyelers
                .Include(u => u.OduncIslemleris)
                .FirstOrDefaultAsync(u => u.UyeId == id);

            if (uye == null)
            {
                return RedirectToAction(nameof(Index));
            }

            if (uye.Rol == "Admin")
            {
                TempData["HataMesaji"] =
                    "Admin hesabı üye ekranından silinemez.";

                return RedirectToAction(nameof(Index));
            }

            if (uye.OduncIslemleris.Any())
            {
                TempData["HataMesaji"] =
                    "Ödünç işlem geçmişi bulunan üye silinemez.";

                return RedirectToAction(nameof(Index));
            }

            _context.Uyelers.Remove(uye);
            await _context.SaveChangesAsync();

            TempData["BasariliMesaj"] =
                "Üye başarıyla silindi.";

            return RedirectToAction(nameof(Index));
        }
    }
}