using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KutuphaneOtomasyonu.Controllers
{
    public class KategorilerController : Controller
    {
        private readonly KutuphaneContext _context;

        public KategorilerController(KutuphaneContext context)
        {
            _context = context;
        }

        // Kategorileri listeler ve arama yapar
        public async Task<IActionResult> Index(string? arama)
        {
            var kategoriler = _context.Kategorilers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(arama))
            {
                kategoriler = kategoriler.Where(k =>
                    k.Baslik.Contains(arama));
            }

            ViewBag.Arama = arama;

            return View(await kategoriler
                .OrderBy(k => k.Baslik)
                .ToListAsync());
        }

        // Kategori detayını ve kategoriye ait kitapları gösterir
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var kategori = await _context.Kategorilers
                .Include(k => k.Kitaplars)
                .ThenInclude(kitap => kitap.Yazar)
                .FirstOrDefaultAsync(k => k.KategoriId == id);

            if (kategori == null)
            {
                return NotFound();
            }

            return View(kategori);
        }

        // Kategori ekleme sayfası
        public IActionResult Create()
        {
            return View();
        }

        // Yeni kategori ekler
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Baslik")] Kategoriler kategori)
        {
            ModelState.Remove("Kitaplars");

            var kategoriVar = await _context.Kategorilers
                .AnyAsync(k => k.Baslik == kategori.Baslik);

            if (kategoriVar)
            {
                ModelState.AddModelError(
                    "Baslik",
                    "Bu isimde bir kategori zaten kayıtlı.");
            }

            if (!ModelState.IsValid)
            {
                return View(kategori);
            }

            _context.Kategorilers.Add(kategori);
            await _context.SaveChangesAsync();

            TempData["BasariliMesaj"] =
                "Kategori başarıyla eklendi.";

            return RedirectToAction(nameof(Index));
        }

        // Kategori düzenleme sayfası
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var kategori = await _context.Kategorilers
                .FindAsync(id);

            if (kategori == null)
            {
                return NotFound();
            }

            return View(kategori);
        }

        // Kategoriyi günceller
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("KategoriId,Baslik")] Kategoriler kategori)
        {
            if (id != kategori.KategoriId)
            {
                return NotFound();
            }

            ModelState.Remove("Kitaplars");

            var kategoriVar = await _context.Kategorilers
                .AnyAsync(k =>
                    k.Baslik == kategori.Baslik &&
                    k.KategoriId != kategori.KategoriId);

            if (kategoriVar)
            {
                ModelState.AddModelError(
                    "Baslik",
                    "Bu isimde başka bir kategori bulunuyor.");
            }

            if (!ModelState.IsValid)
            {
                return View(kategori);
            }

            try
            {
                _context.Kategorilers.Update(kategori);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!KategoriExists(kategori.KategoriId))
                {
                    return NotFound();
                }

                throw;
            }

            TempData["BasariliMesaj"] =
                "Kategori bilgileri güncellendi.";

            return RedirectToAction(nameof(Index));
        }

        // Kategori silme onay sayfası
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var kategori = await _context.Kategorilers
                .Include(k => k.Kitaplars)
                .FirstOrDefaultAsync(k => k.KategoriId == id);

            if (kategori == null)
            {
                return NotFound();
            }

            return View(kategori);
        }

        // Kategoriyi siler
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var kategori = await _context.Kategorilers
                .Include(k => k.Kitaplars)
                .FirstOrDefaultAsync(k => k.KategoriId == id);

            if (kategori == null)
            {
                return RedirectToAction(nameof(Index));
            }

            if (kategori.Kitaplars.Any())
            {
                TempData["HataMesaji"] =
                    "Bu kategoriye bağlı kitaplar olduğu için kategori silinemez.";

                return RedirectToAction(nameof(Index));
            }

            _context.Kategorilers.Remove(kategori);
            await _context.SaveChangesAsync();

            TempData["BasariliMesaj"] =
                "Kategori başarıyla silindi.";

            return RedirectToAction(nameof(Index));
        }

        private bool KategoriExists(int id)
        {
            return _context.Kategorilers
                .Any(k => k.KategoriId == id);
        }
    }
}