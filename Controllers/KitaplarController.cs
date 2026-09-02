using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KutuphaneOtomasyonu.Controllers;

[Authorize(Roles = "Admin,Uye")]
public class KitaplarController : Controller
{
    private readonly KutuphaneContext _context;

    public KitaplarController(
        KutuphaneContext context)
    {
        _context = context;
    }

    // Admin bütün kitapları,
    // üyeler yalnızca aktif kitapları görür.
    public async Task<IActionResult> Index(
        string? arama)
    {
        var kitaplar = _context.Kitaplars
            .Include(k => k.Kategori)
            .Include(k => k.Yazar)
            .AsQueryable();

        if (User.IsInRole("Uye"))
        {
            kitaplar = kitaplar.Where(k =>
                k.AktifMi);
        }

        if (!string.IsNullOrWhiteSpace(arama))
        {
            arama = arama.Trim();

            kitaplar = kitaplar.Where(k =>
                k.Baslik.Contains(arama) ||
                k.Isbn.Contains(arama) ||
                (k.RafKonumu != null &&
                 k.RafKonumu.Contains(arama)) ||
                k.Yazar.AdSoyad.Contains(arama) ||
                k.Kategori.Baslik.Contains(arama));
        }

        ViewBag.Arama = arama;

        var sonuc = await kitaplar
            .OrderByDescending(k => k.AktifMi)
            .ThenBy(k => k.Baslik)
            .ToListAsync();

        return View(sonuc);
    }

    // Kitap detayı
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
            .FirstOrDefaultAsync(k =>
                k.KitapId == id.Value);

        if (kitap == null)
        {
            return NotFound();
        }

        // Üyeler pasif kitabın detayını
        // adres üzerinden de açamaz.
        if (User.IsInRole("Uye") &&
            !kitap.AktifMi)
        {
            return NotFound();
        }

        return View(kitap);
    }

    // Yeni kitap sayfası
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        ListeleriHazirla();

        return View();
    }

    // Yeni kitap kaydetme
    [HttpPost]
    [Authorize(Roles = "Admin")]
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
        ModelState.Remove("AktifMi");
        ModelState.Remove("PasifeAlmaNedeni");
        ModelState.Remove("PasifeAlmaTarihi");

        KitapBilgileriniDogrula(kitap);

        bool isbnKullaniliyor =
            await _context.Kitaplars.AnyAsync(k =>
                k.Isbn == kitap.Isbn);

        if (isbnKullaniliyor)
        {
            ModelState.AddModelError(
                "Isbn",
                "Bu ISBN numarasıyla kayıtlı bir kitap bulunuyor.");
        }

        bool yazarVar =
            await _context.Yazarlars.AnyAsync(y =>
                y.YazarId == kitap.YazarId);

        if (!yazarVar)
        {
            ModelState.AddModelError(
                "YazarId",
                "Geçerli bir yazar seçiniz.");
        }

        bool kategoriVar =
            await _context.Kategorilers.AnyAsync(k =>
                k.KategoriId == kitap.KategoriId);

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

        kitap.AktifMi = true;
        kitap.PasifeAlmaNedeni = null;
        kitap.PasifeAlmaTarihi = null;

        _context.Kitaplars.Add(kitap);

        await _context.SaveChangesAsync();

        TempData["BasariliMesaj"] =
            "Kitap başarıyla eklendi.";

        return RedirectToAction(nameof(Index));
    }

    // Kitap düzenleme sayfası
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var kitap = await _context.Kitaplars
            .FindAsync(id.Value);

        if (kitap == null)
        {
            return NotFound();
        }

        ListeleriHazirla(
            kitap.KategoriId,
            kitap.YazarId);

        return View(kitap);
    }

    // Kitap bilgilerini güncelleme
    [HttpPost]
    [Authorize(Roles = "Admin")]
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
        ModelState.Remove("AktifMi");
        ModelState.Remove("PasifeAlmaNedeni");
        ModelState.Remove("PasifeAlmaTarihi");

        KitapBilgileriniDogrula(kitap);

        bool isbnKullaniliyor =
            await _context.Kitaplars.AnyAsync(k =>
                k.Isbn == kitap.Isbn &&
                k.KitapId != kitap.KitapId);

        if (isbnKullaniliyor)
        {
            ModelState.AddModelError(
                "Isbn",
                "Bu ISBN numarası başka bir kitapta kullanılıyor.");
        }

        bool yazarVar =
            await _context.Yazarlars.AnyAsync(y =>
                y.YazarId == kitap.YazarId);

        if (!yazarVar)
        {
            ModelState.AddModelError(
                "YazarId",
                "Geçerli bir yazar seçiniz.");
        }

        bool kategoriVar =
            await _context.Kategorilers.AnyAsync(k =>
                k.KategoriId == kitap.KategoriId);

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

        var mevcutKitap =
            await _context.Kitaplars
                .FirstOrDefaultAsync(k =>
                    k.KitapId == id);

        if (mevcutKitap == null)
        {
            return NotFound();
        }

        // Pasif kitabın stok bilgisi değiştirilmez.
        if (!mevcutKitap.AktifMi)
        {
            kitap.MevcutAdet = 0;
        }

        mevcutKitap.Isbn = kitap.Isbn;
        mevcutKitap.Baslik = kitap.Baslik;
        mevcutKitap.YazarId = kitap.YazarId;
        mevcutKitap.KategoriId = kitap.KategoriId;
        mevcutKitap.RafKonumu = kitap.RafKonumu;
        mevcutKitap.ToplamAdet = kitap.ToplamAdet;

        if (mevcutKitap.AktifMi)
        {
            mevcutKitap.MevcutAdet =
                kitap.MevcutAdet;
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!KitapExists(id))
            {
                return NotFound();
            }

            throw;
        }

        TempData["BasariliMesaj"] =
            "Kitap bilgileri güncellendi.";

        return RedirectToAction(nameof(Index));
    }

    // Kitabı pasife alma
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PasifeAl(
        int id,
        string? neden)
    {
        var kitap = await _context.Kitaplars
            .FirstOrDefaultAsync(k =>
                k.KitapId == id);

        if (kitap == null)
        {
            TempData["HataMesaji"] =
                "Kitap bulunamadı.";

            return RedirectToAction(nameof(Index));
        }

        if (!kitap.AktifMi)
        {
            TempData["HataMesaji"] =
                "Bu kitap zaten pasif durumda.";

            return RedirectToAction(nameof(Index));
        }

        // Bir veya daha fazla kopya üyelerdeyse
        // kitap pasife alınamaz.
        if (kitap.MevcutAdet != kitap.ToplamAdet)
        {
            int odunctekiAdet =
                kitap.ToplamAdet -
                kitap.MevcutAdet;

            TempData["HataMesaji"] =
                $"Kitabın {odunctekiAdet} kopyası ödünçte olduğu için pasife alınamaz.";

            return RedirectToAction(nameof(Index));
        }

        bool aktifOduncVar =
            await _context.OduncIslemleris
                .AnyAsync(o =>
                    o.KitapId == id &&
                    !o.TeslimEdildiMi);

        if (aktifOduncVar)
        {
            TempData["HataMesaji"] =
                "Kitaba ait teslim edilmemiş ödünç işlemi bulunduğu için pasife alınamaz.";

            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(neden))
        {
            neden = "Katalogdan çıkarıldı";
        }

        kitap.AktifMi = false;
        kitap.PasifeAlmaNedeni = neden.Trim();
        kitap.PasifeAlmaTarihi = DateTime.Now;

        // Fiziksel olarak kullanılabilir stok kalmadığını gösterir.
        kitap.MevcutAdet = 0;

        // Bekleyen rezervasyonlar iptal edilir.
        var bekleyenRezervasyonlar =
            await _context.Rezervasyonlars
                .Where(r =>
                    r.KitapId == id &&
                    r.Durum == "Bekliyor")
                .ToListAsync();

        foreach (var rezervasyon in bekleyenRezervasyonlar)
        {
            rezervasyon.Durum = "İptal";
        }

        await _context.SaveChangesAsync();

        TempData["BasariliMesaj"] =
            bekleyenRezervasyonlar.Any()
                ? $"Kitap pasife alındı ve {bekleyenRezervasyonlar.Count} bekleyen rezervasyon iptal edildi."
                : "Kitap başarıyla pasife alındı.";

        return RedirectToAction(nameof(Index));
    }

    // Pasif kitabı yeniden aktifleştirme
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AktifEt(int id)
    {
        var kitap = await _context.Kitaplars
            .FirstOrDefaultAsync(k =>
                k.KitapId == id);

        if (kitap == null)
        {
            TempData["HataMesaji"] =
                "Kitap bulunamadı.";

            return RedirectToAction(nameof(Index));
        }

        if (kitap.AktifMi)
        {
            TempData["HataMesaji"] =
                "Bu kitap zaten aktif durumda.";

            return RedirectToAction(nameof(Index));
        }

        kitap.AktifMi = true;
        kitap.MevcutAdet = kitap.ToplamAdet;
        kitap.PasifeAlmaNedeni = null;
        kitap.PasifeAlmaTarihi = null;

        await _context.SaveChangesAsync();

        TempData["BasariliMesaj"] =
            "Kitap yeniden aktif edildi.";

        return RedirectToAction(nameof(Index));
    }

    // Yalnızca hiçbir işlem geçmişi bulunmayan,
    // yanlışlıkla oluşturulmuş kitap tamamen silinebilir.
    [HttpGet]
    [Authorize(Roles = "Admin")]
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
            .FirstOrDefaultAsync(k =>
                k.KitapId == id.Value);

        if (kitap == null)
        {
            return NotFound();
        }

        bool rezervasyonGecmisiVar =
            await _context.Rezervasyonlars
                .AnyAsync(r =>
                    r.KitapId == kitap.KitapId);

        if (kitap.OduncIslemleris.Any() ||
            rezervasyonGecmisiVar)
        {
            TempData["HataMesaji"] =
                "İşlem geçmişi bulunan kitap tamamen silinemez. Kitabı pasife alabilirsiniz.";

            return RedirectToAction(nameof(Index));
        }

        if (kitap.MevcutAdet != kitap.ToplamAdet)
        {
            TempData["HataMesaji"] =
                "Bütün kopyalar mevcut olmadan kitap silinemez.";

            return RedirectToAction(nameof(Index));
        }

        return View(kitap);
    }

    // İşlem geçmişi olmayan kitabı tamamen silme
    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(
        int id)
    {
        var kitap = await _context.Kitaplars
            .Include(k => k.OduncIslemleris)
            .FirstOrDefaultAsync(k =>
                k.KitapId == id);

        if (kitap == null)
        {
            TempData["HataMesaji"] =
                "Kitap bulunamadı.";

            return RedirectToAction(nameof(Index));
        }

        bool rezervasyonGecmisiVar =
            await _context.Rezervasyonlars
                .AnyAsync(r =>
                    r.KitapId == id);

        if (kitap.OduncIslemleris.Any() ||
            rezervasyonGecmisiVar)
        {
            TempData["HataMesaji"] =
                "İşlem geçmişi bulunan kitap tamamen silinemez. Kitabı pasife alabilirsiniz.";

            return RedirectToAction(nameof(Index));
        }

        if (kitap.MevcutAdet != kitap.ToplamAdet)
        {
            TempData["HataMesaji"] =
                "Bütün kopyalar mevcut olmadan kitap silinemez.";

            return RedirectToAction(nameof(Index));
        }

        _context.Kitaplars.Remove(kitap);

        await _context.SaveChangesAsync();

        TempData["BasariliMesaj"] =
            "İşlem geçmişi olmayan kitap tamamen silindi.";

        return RedirectToAction(nameof(Index));
    }

    private void KitapBilgileriniDogrula(
        Kitaplar kitap)
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

        if (kitap.MevcutAdet >
            kitap.ToplamAdet)
        {
            ModelState.AddModelError(
                "MevcutAdet",
                "Mevcut adet toplam adetten fazla olamaz.");
        }
    }

    private void ListeleriHazirla(
        int? kategoriId = null,
        int? yazarId = null)
    {
        ViewData["KategoriId"] =
            new SelectList(
                _context.Kategorilers
                    .OrderBy(k => k.Baslik),
                "KategoriId",
                "Baslik",
                kategoriId);

        ViewData["YazarId"] =
            new SelectList(
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