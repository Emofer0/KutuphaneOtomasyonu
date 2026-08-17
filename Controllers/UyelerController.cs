using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KutuphaneOtomasyonu.Controllers
{
    public class UyelerController : Controller
    {
        private readonly KutuphaneContext _context;

        public UyelerController(KutuphaneContext context)
        {
            _context = context;
        }

        // Üyeleri listeler ve arama yapar
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

        // Üye detayını ve ödünç işlemlerini getirir
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

        // Üye ekleme sayfası
        public IActionResult Create()
        {
            return View();
        }

        // Yeni üye ekler
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("AdSoyad,Eposta,Telefon")] Uyeler uye)
        {
            ModelState.Remove("Sifre");
            ModelState.Remove("Rol");
            ModelState.Remove("OduncIslemleris");

            var epostaKullaniliyor = await _context.Uyelers
                .AnyAsync(u => u.Eposta == uye.Eposta);

            if (epostaKullaniliyor)
            {
                ModelState.AddModelError(
                    "Eposta",
                    "Bu e-posta adresiyle kayıtlı bir üye bulunuyor.");
            }

            if (!ModelState.IsValid)
            {
                return View(uye);
            }

            uye.KayitTarihi = DateTime.Now;
            uye.Sifre = "";
            uye.Rol = "Uye";

            _context.Uyelers.Add(uye);
            await _context.SaveChangesAsync();

            TempData["BasariliMesaj"] =
                "Üye başarıyla eklendi.";

            return RedirectToAction(nameof(Index));
        }

        // Üye düzenleme sayfası
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

        // Üyeyi günceller
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

        // Üye silme onay sayfası
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

        // Üyeyi siler
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

        private bool UyeExists(int id)
        {
            return _context.Uyelers
                .Any(u => u.UyeId == id);
        }
    }
}