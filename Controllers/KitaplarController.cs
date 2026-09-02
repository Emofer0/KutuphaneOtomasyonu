using System.Linq;
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

    // Kitap detayları
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

        // Üyeler pasif kitabın detayına giremez.
        if (User.IsInRole("Uye") &&
            !kitap.AktifMi)
        {
            return NotFound();
        }

        return View(kitap);
    }

    // Stok yönetimi sayfası
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> StokYonetimi(
        int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var kitap = await _context.Kitaplars
            .Include(k => k.Yazar)
            .Include(k => k.Kategori)
            .FirstOrDefaultAsync(k =>
                k.KitapId == id.Value);

        if (kitap == null)
        {
            return NotFound();
        }

        ViewBag.OdunctekiAdet =
            await _context.OduncIslemleris
                .CountAsync(o =>
                    o.KitapId == kitap.KitapId &&
                    !o.TeslimEdildiMi);

        ViewBag.BekleyenRezervasyon =
            await _context.Rezervasyonlars
                .CountAsync(r =>
                    r.KitapId == kitap.KitapId &&
                    r.Durum == "Bekliyor");

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
            "KitapId,Isbn,Baslik,YazarId," +
            "KategoriId,RafKonumu")]
        Kitaplar formKitap)
    {
        if (id != formKitap.KitapId)
        {
            return NotFound();
        }

        ModelState.Remove("Yazar");
        ModelState.Remove("Kategori");
        ModelState.Remove("OduncIslemleris");
        ModelState.Remove("Sifre");
        ModelState.Remove("AktifMi");
        ModelState.Remove("ToplamAdet");
        ModelState.Remove("MevcutAdet");
        ModelState.Remove("PasifeAlmaNedeni");
        ModelState.Remove("PasifeAlmaTarihi");

        bool isbnKullaniliyor =
            await _context.Kitaplars.AnyAsync(k =>
                k.Isbn == formKitap.Isbn &&
                k.KitapId != formKitap.KitapId);

        if (isbnKullaniliyor)
        {
            ModelState.AddModelError(
                "Isbn",
                "Bu ISBN numarası başka bir kitapta kullanılıyor.");
        }

        bool yazarVar =
            await _context.Yazarlars.AnyAsync(y =>
                y.YazarId == formKitap.YazarId);

        if (!yazarVar)
        {
            ModelState.AddModelError(
                "YazarId",
                "Geçerli bir yazar seçiniz.");
        }

        bool kategoriVar =
            await _context.Kategorilers.AnyAsync(k =>
                k.KategoriId == formKitap.KategoriId);

        if (!kategoriVar)
        {
            ModelState.AddModelError(
                "KategoriId",
                "Geçerli bir kategori seçiniz.");
        }

        var mevcutKitap =
            await _context.Kitaplars
                .FirstOrDefaultAsync(k =>
                    k.KitapId == id);

        if (mevcutKitap == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            formKitap.ToplamAdet =
                mevcutKitap.ToplamAdet;

            formKitap.MevcutAdet =
                mevcutKitap.MevcutAdet;

            formKitap.AktifMi =
                mevcutKitap.AktifMi;

            formKitap.PasifeAlmaNedeni =
                mevcutKitap.PasifeAlmaNedeni;

            formKitap.PasifeAlmaTarihi =
                mevcutKitap.PasifeAlmaTarihi;

            ListeleriHazirla(
                formKitap.KategoriId,
                formKitap.YazarId);

            return View(formKitap);
        }

        mevcutKitap.Isbn =
            formKitap.Isbn;

        mevcutKitap.Baslik =
            formKitap.Baslik;

        mevcutKitap.YazarId =
            formKitap.YazarId;

        mevcutKitap.KategoriId =
            formKitap.KategoriId;

        mevcutKitap.RafKonumu =
            formKitap.RafKonumu;

        await _context.SaveChangesAsync();

        TempData["BasariliMesaj"] =
            "Kitap bilgileri güncellendi.";

        return RedirectToAction(nameof(Index));
    }

    // Kitabın tamamını pasife alma
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

            return RedirectToAction(
                nameof(StokYonetimi),
                new { id });
        }

        if (kitap.MevcutAdet !=
            kitap.ToplamAdet)
        {
            int odunctekiAdet =
                kitap.ToplamAdet -
                kitap.MevcutAdet;

            TempData["HataMesaji"] =
                $"Kitabın {odunctekiAdet} kopyası ödünçte olduğu için pasife alınamaz.";

            return RedirectToAction(
                nameof(StokYonetimi),
                new { id });
        }

        bool aktifOduncVar =
            await _context.OduncIslemleris
                .AnyAsync(o =>
                    o.KitapId == id &&
                    !o.TeslimEdildiMi);

        if (aktifOduncVar)
        {
            TempData["HataMesaji"] =
                "Teslim edilmemiş ödünç işlemi bulunduğu için kitap pasife alınamaz.";

            return RedirectToAction(
                nameof(StokYonetimi),
                new { id });
        }

        string[] gecerliNedenler =
        {
            "Yasaklandı",
            "Katalogdan çıkarıldı"
        };

        if (string.IsNullOrWhiteSpace(neden) ||
            !gecerliNedenler.Contains(neden))
        {
            TempData["HataMesaji"] =
                "Geçerli bir pasife alma nedeni seçiniz.";

            return RedirectToAction(
                nameof(StokYonetimi),
                new { id });
        }

        kitap.AktifMi = false;
        kitap.MevcutAdet = 0;
        kitap.PasifeAlmaNedeni = neden;
        kitap.PasifeAlmaTarihi = DateTime.Now;

        var bekleyenRezervasyonlar =
            await _context.Rezervasyonlars
                .Where(r =>
                    r.KitapId == id &&
                    r.Durum == "Bekliyor")
                .ToListAsync();

        foreach (var rezervasyon in
                 bekleyenRezervasyonlar)
        {
            rezervasyon.Durum = "İptal";
        }

        await _context.SaveChangesAsync();

        TempData["BasariliMesaj"] =
            bekleyenRezervasyonlar.Any()
                ? $"Kitap pasife alındı ve {bekleyenRezervasyonlar.Count} rezervasyon iptal edildi."
                : "Kitap başarıyla pasife alındı.";

        return RedirectToAction(
            nameof(StokYonetimi),
            new { id });
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

            return RedirectToAction(
                nameof(StokYonetimi),
                new { id });
        }

        kitap.AktifMi = true;
        kitap.MevcutAdet =
            kitap.ToplamAdet;
        kitap.PasifeAlmaNedeni = null;
        kitap.PasifeAlmaTarihi = null;

        await _context.SaveChangesAsync();

        TempData["BasariliMesaj"] =
            "Kitap yeniden aktif edildi.";

        return RedirectToAction(
            nameof(StokYonetimi),
            new { id });
    }

    // Yeni fiziksel kopya ekleme
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> KopyaArtir(
        int id,
        int adet)
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
                "Pasif kitaba kopya eklenemez. Önce kitabı aktif etmelisiniz.";

            return RedirectToAction(
                nameof(StokYonetimi),
                new { id });
        }

        if (adet <= 0)
        {
            TempData["HataMesaji"] =
                "Artırılacak kopya sayısı sıfırdan büyük olmalıdır.";

            return RedirectToAction(
                nameof(StokYonetimi),
                new { id });
        }

        kitap.ToplamAdet += adet;
        kitap.MevcutAdet += adet;

        await _context.SaveChangesAsync();

        TempData["BasariliMesaj"] =
            $"{kitap.Baslik} kitabına {adet} yeni kopya eklendi.";

        return RedirectToAction(
            nameof(StokYonetimi),
            new { id });
    }

    // Raftaki fiziksel kopyaları azaltma
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> KopyaAzalt(
        int id,
        int adet,
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
                "Pasif kitabın kopya sayısı değiştirilemez.";

            return RedirectToAction(
                nameof(StokYonetimi),
                new { id });
        }

        if (adet <= 0)
        {
            TempData["HataMesaji"] =
                "Azaltılacak kopya sayısı sıfırdan büyük olmalıdır.";

            return RedirectToAction(
                nameof(StokYonetimi),
                new { id });
        }

        // Yalnızca raftaki kopyalar azaltılabilir.
        if (adet > kitap.MevcutAdet)
        {
            int odunctekiAdet =
                kitap.ToplamAdet -
                kitap.MevcutAdet;

            TempData["HataMesaji"] =
                $"Rafta yalnızca {kitap.MevcutAdet} kopya bulunuyor. " +
                $"{odunctekiAdet} kopya ödünçte.";

            return RedirectToAction(
                nameof(StokYonetimi),
                new { id });
        }

        string[] gecerliNedenler =
        {
            "Kayıp",
            "Hasarlı",
            "Fiziksel stoktan çıkarıldı"
        };

        if (string.IsNullOrWhiteSpace(neden) ||
            !gecerliNedenler.Contains(neden))
        {
            TempData["HataMesaji"] =
                "Geçerli bir kopya azaltma nedeni seçiniz.";

            return RedirectToAction(
                nameof(StokYonetimi),
                new { id });
        }

        kitap.ToplamAdet -= adet;
        kitap.MevcutAdet -= adet;

        // Son kopya da çıkarılırsa kitap pasif olur.
        if (kitap.ToplamAdet == 0)
        {
            kitap.AktifMi = false;
            kitap.MevcutAdet = 0;
            kitap.PasifeAlmaNedeni =
                $"Tüm kopyalar stoktan çıkarıldı: {neden}";
            kitap.PasifeAlmaTarihi =
                DateTime.Now;

            var bekleyenRezervasyonlar =
                await _context.Rezervasyonlars
                    .Where(r =>
                        r.KitapId == id &&
                        r.Durum == "Bekliyor")
                    .ToListAsync();

            foreach (var rezervasyon in
                     bekleyenRezervasyonlar)
            {
                rezervasyon.Durum = "İptal";
            }
        }

        await _context.SaveChangesAsync();

        TempData["BasariliMesaj"] =
            kitap.ToplamAdet == 0
                ? $"{adet} kopya '{neden}' nedeniyle çıkarıldı. Son kopya da kaldırıldığı için kitap pasife alındı."
                : $"{adet} kopya '{neden}' nedeniyle stoktan çıkarıldı.";

        return RedirectToAction(
            nameof(StokYonetimi),
            new { id });
    }

    // Silme onay sayfası
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

            return RedirectToAction(
                nameof(StokYonetimi),
                new { id = kitap.KitapId });
        }

        if (kitap.MevcutAdet !=
            kitap.ToplamAdet)
        {
            TempData["HataMesaji"] =
                "Bütün kopyalar mevcut olmadan kitap silinemez.";

            return RedirectToAction(
                nameof(StokYonetimi),
                new { id = kitap.KitapId });
        }

        return View(kitap);
    }

    // Kitabı tamamen silme
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
                "İşlem geçmişi bulunan kitap tamamen silinemez.";

            return RedirectToAction(
                nameof(StokYonetimi),
                new { id });
        }

        if (kitap.MevcutAdet !=
            kitap.ToplamAdet)
        {
            TempData["HataMesaji"] =
                "Bütün kopyalar mevcut olmadan kitap silinemez.";

            return RedirectToAction(
                nameof(StokYonetimi),
                new { id });
        }

        _context.Kitaplars.Remove(kitap);

        await _context.SaveChangesAsync();

        TempData["BasariliMesaj"] =
            "İşlem geçmişi olmayan kitap tamamen silindi.";

        return RedirectToAction(nameof(Index));
    }

    // Kitap adetlerini doğrular
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

    // Kategori ve yazar listelerini hazırlar
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
}