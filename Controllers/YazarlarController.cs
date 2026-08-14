using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KutuphaneOtomasyonu.Controllers
{
    public class YazarlarController : Controller
    {
        private readonly KutuphaneContext _context;

        public YazarlarController(KutuphaneContext context)
        {
            _context = context;
        }

        // Yazarları listeler ve arama yapar
        public async Task<IActionResult> Index(string? arama)
        {
            var yazarlar = _context.Yazarlars.AsQueryable();

            if (!string.IsNullOrWhiteSpace(arama))
            {
                yazarlar = yazarlar.Where(y =>
                    y.AdSoyad.Contains(arama));
            }

            ViewBag.Arama = arama;

            return View(await yazarlar
                .OrderBy(y => y.AdSoyad)
                .ToListAsync());
        }

        // Yazar detayını ve yazara ait kitapları gösterir
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var yazar = await _context.Yazarlars
                .Include(y => y.Kitaplars)
                .ThenInclude(kitap => kitap.Kategori)
                .FirstOrDefaultAsync(y => y.YazarId == id);

            if (yazar == null)
            {
                return NotFound();
            }

            return View(yazar);
        }

        // Yazar ekleme sayfası
        public IActionResult Create()
        {
            return View();
        }

        // Yeni yazar ekler
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("AdSoyad")] Yazarlar yazar)
        {
            ModelState.Remove("Kitaplars");

            var yazarVar = await _context.Yazarlars
                .AnyAsync(y => y.AdSoyad == yazar.AdSoyad);

            if (yazarVar)
            {
                ModelState.AddModelError(
                    "AdSoyad",
                    "Bu isimde bir yazar zaten kayıtlı.");
            }

            if (!ModelState.IsValid)
            {
                return View(yazar);
            }

            _context.Yazarlars.Add(yazar);
            await _context.SaveChangesAsync();

            TempData["BasariliMesaj"] =
                "Yazar başarıyla eklendi.";

            return RedirectToAction(nameof(Index));
        }

        // Yazar düzenleme sayfası
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var yazar = await _context.Yazarlars.FindAsync(id);

            if (yazar == null)
            {
                return NotFound();
            }

            return View(yazar);
        }

        // Yazarı günceller
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("YazarId,AdSoyad")] Yazarlar yazar)
        {
            if (id != yazar.YazarId)
            {
                return NotFound();
            }

            ModelState.Remove("Kitaplars");

            var yazarVar = await _context.Yazarlars
                .AnyAsync(y =>
                    y.AdSoyad == yazar.AdSoyad &&
                    y.YazarId != yazar.YazarId);

            if (yazarVar)
            {
                ModelState.AddModelError(
                    "AdSoyad",
                    "Bu isimde başka bir yazar bulunuyor.");
            }

            if (!ModelState.IsValid)
            {
                return View(yazar);
            }

            try
            {
                _context.Yazarlars.Update(yazar);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!YazarExists(yazar.YazarId))
                {
                    return NotFound();
                }

                throw;
            }

            TempData["BasariliMesaj"] =
                "Yazar bilgileri güncellendi.";

            return RedirectToAction(nameof(Index));
        }

        // Yazar silme onay sayfası
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var yazar = await _context.Yazarlars
                .Include(y => y.Kitaplars)
                .FirstOrDefaultAsync(y => y.YazarId == id);

            if (yazar == null)
            {
                return NotFound();
            }

            return View(yazar);
        }

        // Yazarı siler
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var yazar = await _context.Yazarlars
                .Include(y => y.Kitaplars)
                .FirstOrDefaultAsync(y => y.YazarId == id);

            if (yazar == null)
            {
                return RedirectToAction(nameof(Index));
            }

            if (yazar.Kitaplars.Any())
            {
                TempData["HataMesaji"] =
                    "Bu yazara bağlı kitaplar olduğu için yazar silinemez.";

                return RedirectToAction(nameof(Index));
            }

            _context.Yazarlars.Remove(yazar);
            await _context.SaveChangesAsync();

            TempData["BasariliMesaj"] =
                "Yazar başarıyla silindi.";

            return RedirectToAction(nameof(Index));
        }

        private bool YazarExists(int id)
        {
            return _context.Yazarlars
                .Any(y => y.YazarId == id);
        }
    }
}
