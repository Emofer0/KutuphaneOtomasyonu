using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KutuphaneOtomasyonu.Controllers
{
    public class KitaplarController : Controller
    {
        private readonly KutuphaneContext _context;

        public KitaplarController(KutuphaneContext context)
        {
            _context = context;
        }

        // Kitapları listeler ve arama yapar
        public async Task<IActionResult> Index(string? arama)
        {
            var kitaplar = _context.Kitaplars
                .Include(k => k.Kategori)
                .Include(k => k.Yazar)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(arama))
            {
                kitaplar = kitaplar.Where(k =>
                    k.Baslik.Contains(arama) ||
                    k.Isbn.Contains(arama) ||
                    (k.RafKonumu != null &&
                     k.RafKonumu.Contains(arama)) ||
                    k.Yazar.AdSoyad.Contains(arama) ||
                    k.Kategori.Baslik.Contains(arama));
            }

            ViewBag.Arama = arama;

            return View(await kitaplar
                .OrderBy(k => k.Baslik)
                .ToListAsync());
        }

        // Kitap detayını ve ödünç geçmişini gösterir
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var kitap = await _context.Kitaplars
                .Include(k => k.Kategori)
                .Include(k => k.Yazar)
                .Include(k => k.OduncIslemleris)
                .ThenInclude(o => o.Uye)
                .FirstOrDefaultAsync(k => k.KitapId == id);

            if (kitap == null)
            {
                return NotFound();
            }

            return View(kitap);
        }

        // Kitap ekleme sayfası
        public IActionResult Create()
        {
            ListeleriHazirla();
            return View();
        }

        // Yeni kitap ekler
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind(
                "KitapId,Isbn,Baslik,YazarId,KategoriId," +
                "RafKonumu,ToplamAdet,MevcutAdet")]
            Kitaplar kitap)
        {
            ModelState.Remove("Yazar");
            ModelState.Remove("Kategori");
            ModelState.Remove("OduncIslemleris");

            KitapBilgileriniDogrula(kitap);

            var isbnKullaniliyor = await _context.Kitaplars
                .AnyAsync(k => k.Isbn == kitap.Isbn);

            if (isbnKullaniliyor)
            {
                ModelState.AddModelError(
                    "Isbn",
                    "Bu ISBN numarasıyla kayıtlı bir kitap bulunuyor.");
            }

            var yazarVar = await _context.Yazarlars
                .AnyAsync(y => y.YazarId == kitap.YazarId);

            if (!yazarVar)
            {
                ModelState.AddModelError(
                    "YazarId",
                    "Geçerli bir yazar seçiniz.");
            }

            var kategoriVar = await _context.Kategorilers
                .AnyAsync(k => k.KategoriId == kitap.KategoriId);

            if (!kategoriVar)
            {
                ModelState.AddModelError(
                    "KategoriId",
                    "Geçerli bir kategori seçiniz.");
            }

            if (!ModelState.IsValid)
            {
                ListeleriHazirla(
                    kitap.KategoriId,
                    kitap.YazarId);

                return View(kitap);
            }

            _context.Kitaplars.Add(kitap);
            await _context.SaveChangesAsync();

            TempData["BasariliMesaj"] =
                "Kitap başarıyla eklendi.";

            return RedirectToAction(nameof(Index));
        }

        // Kitap düzenleme sayfası
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var kitap = await _context.Kitaplars.FindAsync(id);

            if (kitap == null)
            {
                return NotFound();
            }

            ListeleriHazirla(
                kitap.KategoriId,
                kitap.YazarId);

            return View(kitap);
        }

        // Kitabı günceller
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind(
                "KitapId,Isbn,Baslik,YazarId,KategoriId," +
                "RafKonumu,ToplamAdet,MevcutAdet")]
            Kitaplar kitap)
        {
            if (id != kitap.KitapId)
            {
                return NotFound();
            }

            ModelState.Remove("Yazar");
            ModelState.Remove("Kategori");
            ModelState.Remove("OduncIslemleris");

            KitapBilgileriniDogrula(kitap);

            var isbnKullaniliyor = await _context.Kitaplars
                .AnyAsync(k =>
                    k.Isbn == kitap.Isbn &&
                    k.KitapId != kitap.KitapId);

            if (isbnKullaniliyor)
            {
                ModelState.AddModelError(
                    "Isbn",
                    "Bu ISBN numarası başka bir kitapta kullanılıyor.");
            }

            var yazarVar = await _context.Yazarlars
                .AnyAsync(y => y.YazarId == kitap.YazarId);

            if (!yazarVar)
            {
                ModelState.AddModelError(
                    "YazarId",
                    "Geçerli bir yazar seçiniz.");
            }

            var kategoriVar = await _context.Kategorilers
                .AnyAsync(k => k.KategoriId == kitap.KategoriId);

            if (!kategoriVar)
            {
                ModelState.AddModelError(
                    "KategoriId",
                    "Geçerli bir kategori seçiniz.");
            }

            if (!ModelState.IsValid)
            {
                ListeleriHazirla(
                    kitap.KategoriId,
                    kitap.YazarId);

                return View(kitap);
            }

            try
            {
                _context.Kitaplars.Update(kitap);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!KitapExists(kitap.KitapId))
                {
                    return NotFound();
                }

                throw;
            }

            TempData["BasariliMesaj"] =
                "Kitap bilgileri güncellendi.";

            return RedirectToAction(nameof(Index));
        }

        // Kitap silme onay sayfası
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var kitap = await _context.Kitaplars
                .Include(k => k.Kategori)
                .Include(k => k.Yazar)
                .Include(k => k.OduncIslemleris)
                .FirstOrDefaultAsync(k => k.KitapId == id);

            if (kitap == null)
            {
                return NotFound();
            }

            return View(kitap);
        }

        // Kitabı siler
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var kitap = await _context.Kitaplars
                .Include(k => k.OduncIslemleris)
                .FirstOrDefaultAsync(k => k.KitapId == id);

            if (kitap == null)
            {
                return RedirectToAction(nameof(Index));
            }

            if (kitap.OduncIslemleris.Any())
            {
                TempData["HataMesaji"] =
                    "Ödünç işlem geçmişi bulunan kitap silinemez.";

                return RedirectToAction(nameof(Index));
            }

            _context.Kitaplars.Remove(kitap);
            await _context.SaveChangesAsync();

            TempData["BasariliMesaj"] =
                "Kitap başarıyla silindi.";

            return RedirectToAction(nameof(Index));
        }

        // Stok bilgilerini kontrol eder
        private void KitapBilgileriniDogrula(Kitaplar kitap)
        {
            if (kitap.ToplamAdet < 0)
            {
                ModelState.AddModelError(
                    "ToplamAdet",
                    "Toplam adet sıfırdan küçük olamaz.");
            }

            if (kitap.MevcutAdet < 0)
            {
                ModelState.AddModelError(
                    "MevcutAdet",
                    "Mevcut adet sıfırdan küçük olamaz.");
            }

            if (kitap.MevcutAdet > kitap.ToplamAdet)
            {
                ModelState.AddModelError(
                    "MevcutAdet",
                    "Mevcut adet toplam adetten fazla olamaz.");
            }
        }

        // Kategori ve yazar seçim listelerini hazırlar
        private void ListeleriHazirla(
            int? kategoriId = null,
            int? yazarId = null)
        {
            ViewData["KategoriId"] = new SelectList(
                _context.Kategorilers
                    .OrderBy(k => k.Baslik),
                "KategoriId",
                "Baslik",
                kategoriId);

            ViewData["YazarId"] = new SelectList(
                _context.Yazarlars
                    .OrderBy(y => y.AdSoyad),
                "YazarId",
                "AdSoyad",
                yazarId);
        }

        private bool KitapExists(int id)
        {
            return _context.Kitaplars
                .Any(k => k.KitapId == id);
        }
    }
}