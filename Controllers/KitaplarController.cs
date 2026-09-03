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
            .Include(k => k.KitapKopyalaris)
            .FirstOrDefaultAsync(k =>
                k.KitapId == id.Value);

        if (kitap == null)
        {
            return NotFound();
        }

        if (User.IsInRole("Uye") &&
            !kitap.AktifMi)
        {
            return NotFound();
        }

        return View(kitap);
    }

    // Stok ve barkod yönetimi
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
            .Include(k => k.KitapKopyalaris)
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

    // Yeni kitap formu
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        ListeleriHazirla();

        return View();
    }

    // Yeni kitap ve fiziksel kopyalarını oluşturur.
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
        ModelState.Remove("KitapKopyalaris");
        ModelState.Remove("AktifMi");
        ModelState.Remove("PasifeAlmaNedeni");
        ModelState.Remove("PasifeAlmaTarihi");

        KitapBilgileriniDogrula(kitap);

        // Yeni eklenen kitabın bütün kopyaları rafta olmalıdır.
        if (kitap.ToplamAdet != kitap.MevcutAdet)
        {
            ModelState.AddModelError(
                "MevcutAdet",
                "Yeni kitap eklenirken mevcut adet toplam adede eşit olmalıdır.");
        }

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

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            _context.Kitaplars.Add(kitap);

            // Önce KitapId oluşturulur.
            await _context.SaveChangesAsync();

            var yeniKopyalar =
                new List<KitapKopyalari>();

            for (int i = 0;
                 i < kitap.ToplamAdet;
                 i++)
            {
                yeniKopyalar.Add(
                    new KitapKopyalari
                    {
                        KitapId = kitap.KitapId,
                        Barkod =
                            "TMP-" +
                            Guid.NewGuid().ToString("N"),
                        Durum = "Rafta",
                        EklenmeTarihi = DateTime.Now
                    });
            }

            _context.KitapKopyalaris.AddRange(
                yeniKopyalar);

            // KopyaId değerleri oluşturulur.
            await _context.SaveChangesAsync();

            foreach (var kopya in yeniKopyalar)
            {
                kopya.Barkod =
                    $"KTP-{kopya.KopyaId:D6}";
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["BasariliMesaj"] =
                "Kitap ve barkodlu fiziksel kopyaları başarıyla oluşturuldu.";

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();

            ModelState.AddModelError(
                "",
                "Kitap veya barkod kayıtları oluşturulurken veritabanı hatası oluştu.");

            ListeleriHazirla(
                kitap.KategoriId,
                kitap.YazarId);

            return View(kitap);
        }
    }

    // Kitap düzenleme formu
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

    // Yalnızca temel kitap bilgilerini günceller.
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
        ModelState.Remove("KitapKopyalaris");
        ModelState.Remove("AktifMi");
        ModelState.Remove("ToplamAdet");
        ModelState.Remove("MevcutAdet");
        ModelState.Remove("PasifeAlmaNedeni");
        ModelState.Remove("PasifeAlmaTarihi");

        bool isbnKullaniliyor =
            await _context.Kitaplars.AnyAsync(k =>
                k.Isbn == formKitap.Isbn &&
                k.KitapId != id);

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

    // Kitabın tamamını pasife alır.
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
                "Kitap zaten pasif durumda.";

            return RedirectToAction(
                nameof(StokYonetimi),
                new { id });
        }

        if (kitap.MevcutAdet !=
            kitap.ToplamAdet)
        {
            TempData["HataMesaji"] =
                "Ödünçte kopyası bulunan kitap pasife alınamaz.";

            return RedirectToAction(
                nameof(StokYonetimi),
                new { id });
        }

        bool aktifOduncVar =
            await _context.OduncIslemleris.AnyAsync(o =>
                o.KitapId == id &&
                !o.TeslimEdildiMi);

        if (aktifOduncVar)
        {
            TempData["HataMesaji"] =
                "Teslim edilmemiş işlem bulunduğu için kitap pasife alınamaz.";

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

        var rezervasyonlar =
            await _context.Rezervasyonlars
                .Where(r =>
                    r.KitapId == id &&
                    r.Durum == "Bekliyor")
                .ToListAsync();

        foreach (var rezervasyon in rezervasyonlar)
        {
            rezervasyon.Durum = "İptal";
        }

        await _context.SaveChangesAsync();

        TempData["BasariliMesaj"] =
            rezervasyonlar.Any()
                ? $"Kitap pasife alındı ve {rezervasyonlar.Count} rezervasyon iptal edildi."
                : "Kitap başarıyla pasife alındı.";

        return RedirectToAction(
            nameof(StokYonetimi),
            new { id });
    }

    // Pasif kitabı yeniden aktifleştirir.
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
                "Kitap zaten aktif durumda.";

            return RedirectToAction(
                nameof(StokYonetimi),
                new { id });
        }

        int raftakiKopyaSayisi =
            await _context.KitapKopyalaris
                .CountAsync(k =>
                    k.KitapId == id &&
                    k.Durum == "Rafta");

        kitap.AktifMi = true;
        kitap.MevcutAdet = raftakiKopyaSayisi;
        kitap.PasifeAlmaNedeni = null;
        kitap.PasifeAlmaTarihi = null;

        await _context.SaveChangesAsync();

        TempData["BasariliMesaj"] =
            "Kitap yeniden aktif edildi.";

        return RedirectToAction(
            nameof(StokYonetimi),
            new { id });
    }

    // Yeni fiziksel kopyalar ekleyip barkod oluşturur.
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
                "Önce kitabı aktif etmelisiniz.";

            return RedirectToAction(
                nameof(StokYonetimi),
                new { id });
        }

        if (adet <= 0 || adet > 100)
        {
            TempData["HataMesaji"] =
                "Tek işlemde 1 ile 100 arasında kopya ekleyebilirsiniz.";

            return RedirectToAction(
                nameof(StokYonetimi),
                new { id });
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var yeniKopyalar =
                new List<KitapKopyalari>();

            for (int i = 0; i < adet; i++)
            {
                yeniKopyalar.Add(
                    new KitapKopyalari
                    {
                        KitapId = id,
                        Barkod =
                            "TMP-" +
                            Guid.NewGuid().ToString("N"),
                        Durum = "Rafta",
                        EklenmeTarihi = DateTime.Now
                    });
            }

            _context.KitapKopyalaris.AddRange(
                yeniKopyalar);

            kitap.ToplamAdet += adet;
            kitap.MevcutAdet += adet;

            await _context.SaveChangesAsync();

            foreach (var kopya in yeniKopyalar)
            {
                kopya.Barkod =
                    $"KTP-{kopya.KopyaId:D6}";
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["BasariliMesaj"] =
                $"{adet} yeni kopya ve barkodu oluşturuldu.";
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();

            TempData["HataMesaji"] =
                "Kopyalar kaydedilirken veritabanı hatası oluştu.";
        }

        return RedirectToAction(
            nameof(StokYonetimi),
            new { id });
    }

    // Raftaki belirli kopyaları stoktan çıkarır.
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
                "Pasif kitabın kopyaları azaltılamaz.";

            return RedirectToAction(
                nameof(StokYonetimi),
                new { id });
        }

        if (adet <= 0 ||
            adet > kitap.MevcutAdet)
        {
            TempData["HataMesaji"] =
                $"Rafta en fazla {kitap.MevcutAdet} kopya azaltılabilir.";

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
                "Geçerli bir azaltma nedeni seçiniz.";

            return RedirectToAction(
                nameof(StokYonetimi),
                new { id });
        }

        var raftakiKopyalar =
            await _context.KitapKopyalaris
                .Where(k =>
                    k.KitapId == id &&
                    k.Durum == "Rafta")
                .OrderBy(k => k.KopyaId)
                .Take(adet)
                .ToListAsync();

        if (raftakiKopyalar.Count < adet)
        {
            TempData["HataMesaji"] =
                "Yeterli sayıda rafta barkodlu kopya bulunamadı.";

            return RedirectToAction(
                nameof(StokYonetimi),
                new { id });
        }

        string yeniDurum = neden switch
        {
            "Kayıp" => "Kayıp",
            "Hasarlı" => "Hasarlı",
            _ => "Pasif"
        };

        foreach (var kopya in raftakiKopyalar)
        {
            kopya.Durum = yeniDurum;
        }

        kitap.ToplamAdet -= adet;
        kitap.MevcutAdet -= adet;

        if (kitap.ToplamAdet == 0)
        {
            kitap.AktifMi = false;
            kitap.MevcutAdet = 0;
            kitap.PasifeAlmaNedeni =
                $"Tüm kopyalar stoktan çıkarıldı: {neden}";
            kitap.PasifeAlmaTarihi = DateTime.Now;

            var rezervasyonlar =
                await _context.Rezervasyonlars
                    .Where(r =>
                        r.KitapId == id &&
                        r.Durum == "Bekliyor")
                    .ToListAsync();

            foreach (var rezervasyon in rezervasyonlar)
            {
                rezervasyon.Durum = "İptal";
            }
        }

        await _context.SaveChangesAsync();

        TempData["BasariliMesaj"] =
            kitap.ToplamAdet == 0
                ? "Son kopya da çıkarıldığı için kitap pasife alındı."
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
            .Include(k => k.KitapKopyalaris)
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
                "İşlem geçmişi bulunan kitap tamamen silinemez.";

            return RedirectToAction(
                nameof(StokYonetimi),
                new { id = kitap.KitapId });
        }

        if (kitap.MevcutAdet !=
            kitap.ToplamAdet)
        {
            TempData["HataMesaji"] =
                "Bütün kopyalar rafta olmadan kitap silinemez.";

            return RedirectToAction(
                nameof(StokYonetimi),
                new { id = kitap.KitapId });
        }

        return View(kitap);
    }

    // Kitabı ve barkodlu kopyalarını tamamen siler.
    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(
        int id)
    {
        var kitap = await _context.Kitaplars
            .Include(k => k.OduncIslemleris)
            .Include(k => k.KitapKopyalaris)
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
                "Bütün kopyalar rafta olmadan kitap silinemez.";

            return RedirectToAction(
                nameof(StokYonetimi),
                new { id });
        }

        _context.KitapKopyalaris.RemoveRange(
            kitap.KitapKopyalaris);

        _context.Kitaplars.Remove(kitap);

        await _context.SaveChangesAsync();

        TempData["BasariliMesaj"] =
            "Kitap ve barkodlu kopyaları tamamen silindi.";

        return RedirectToAction(nameof(Index));
    }

    private void KitapBilgileriniDogrula(
        Kitaplar kitap)
    {
        if (kitap.ToplamAdet <= 0)
        {
            ModelState.AddModelError(
                "ToplamAdet",
                "Toplam adet en az 1 olmalıdır.");
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
}