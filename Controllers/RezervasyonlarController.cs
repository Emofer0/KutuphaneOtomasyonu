using System.Security.Claims;
using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KutuphaneOtomasyonu.Controllers;

[Authorize(Roles = "Admin,Uye")]
public class RezervasyonlarController : Controller
{
    private readonly KutuphaneContext _context;

    public RezervasyonlarController(
        KutuphaneContext context)
    {
        _context = context;
    }

    // Admin bütün rezervasyonları,
    // üye yalnızca kendi rezervasyonlarını görür.
    public async Task<IActionResult> Index()
    {
        var sorgu = _context.Rezervasyonlars
            .Include(r => r.Kitap)
                .ThenInclude(k => k.Yazar)
            .Include(r => r.Uye)
            .AsQueryable();

        if (User.IsInRole("Uye"))
        {
            int? uyeId = GirisYapanUyeId();

            if (uyeId == null)
            {
                return Forbid();
            }

            sorgu = sorgu.Where(r =>
                r.UyeId == uyeId.Value);
        }

        var rezervasyonlar = await sorgu
            .OrderBy(r =>
                r.Durum == "Bekliyor" ? 0 : 1)
            .ThenByDescending(r =>
                r.RezervasyonTarihi)
            .ToListAsync();

        return View(rezervasyonlar);
    }

    // Admin rezervasyon oluşturma sayfası
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        await FormListeleriniHazirla();

        return View();
    }

    // Admin seçilen üye adına rezervasyon oluşturur.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(
        int kitapId,
        int uyeId)
    {
        var uye = await _context.Uyelers
            .FirstOrDefaultAsync(u =>
                u.UyeId == uyeId);

        if (uye == null)
        {
            TempData["Hata"] =
                "Seçilen üye bulunamadı.";

            await FormListeleriniHazirla(
                kitapId,
                uyeId);

            return View();
        }

        if (uye.Rol != "Uye")
        {
            TempData["Hata"] =
                "Admin hesabı adına rezervasyon oluşturulamaz.";

            await FormListeleriniHazirla(
                kitapId,
                uyeId);

            return View();
        }

        if (!uye.AktifMi)
        {
            TempData["Hata"] =
                "Pasif üyeler adına rezervasyon oluşturulamaz.";

            await FormListeleriniHazirla(
                kitapId,
                uyeId);

            return View();
        }

        var kitap = await _context.Kitaplars
            .FirstOrDefaultAsync(k =>
                k.KitapId == kitapId);

        if (kitap == null)
        {
            TempData["Hata"] =
                "Seçilen kitap bulunamadı.";

            await FormListeleriniHazirla(
                kitapId,
                uyeId);

            return View();
        }

        if (!kitap.AktifMi)
        {
            TempData["Hata"] =
                "Pasif kitap için rezervasyon oluşturulamaz.";

            await FormListeleriniHazirla(
                kitapId,
                uyeId);

            return View();
        }

        if (kitap.MevcutAdet > 0)
        {
            TempData["Hata"] =
                "Kitap rafta bulunduğu için rezervasyon oluşturulamaz.";

            await FormListeleriniHazirla(
                kitapId,
                uyeId);

            return View();
        }

        bool bekleyenRezervasyonVar =
            await _context.Rezervasyonlars
                .AnyAsync(r =>
                    r.KitapId == kitapId &&
                    r.UyeId == uyeId &&
                    r.Durum == "Bekliyor");

        if (bekleyenRezervasyonVar)
        {
            TempData["Hata"] =
                "Bu üyenin kitap için zaten bekleyen rezervasyonu var.";

            await FormListeleriniHazirla(
                kitapId,
                uyeId);

            return View();
        }

        bool kitapZatenUyede =
            await _context.OduncIslemleris
                .AnyAsync(o =>
                    o.KitapId == kitapId &&
                    o.UyeId == uyeId &&
                    !o.TeslimEdildiMi);

        if (kitapZatenUyede)
        {
            TempData["Hata"] =
                "Bu kitabın bir kopyası zaten ilgili üyede bulunuyor.";

            await FormListeleriniHazirla(
                kitapId,
                uyeId);

            return View();
        }

        var rezervasyon = new Rezervasyonlar
        {
            KitapId = kitapId,
            UyeId = uyeId,
            RezervasyonTarihi = DateTime.Now,
            Durum = "Bekliyor"
        };

        _context.Rezervasyonlars.Add(
            rezervasyon);

        try
        {
            await _context.SaveChangesAsync();

            TempData["Basarili"] =
                "Rezervasyon başarıyla oluşturuldu.";

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            TempData["Hata"] =
                "Rezervasyon kaydedilemedi. Aynı kitap için bekleyen rezervasyon olabilir.";

            await FormListeleriniHazirla(
                kitapId,
                uyeId);

            return View();
        }
    }

    // Üye kitap detayından rezervasyon oluşturur.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Uye")]
    public async Task<IActionResult> Ayirt(
        int kitapId)
    {
        int? uyeId = GirisYapanUyeId();

        if (uyeId == null)
        {
            return Forbid();
        }

        var uye = await _context.Uyelers
            .FirstOrDefaultAsync(u =>
                u.UyeId == uyeId.Value);

        if (uye == null || !uye.AktifMi)
        {
            TempData["Hata"] =
                "Pasif üyeler rezervasyon oluşturamaz.";

            return RedirectToAction(
                "Index",
                "Kitaplar");
        }

        var kitap = await _context.Kitaplars
            .FirstOrDefaultAsync(k =>
                k.KitapId == kitapId);

        if (kitap == null)
        {
            return NotFound();
        }

        if (!kitap.AktifMi)
        {
            TempData["Hata"] =
                "Bu kitap pasif durumda olduğu için rezervasyon yapılamaz.";

            return RedirectToAction(
                "Index",
                "Kitaplar");
        }

        if (kitap.MevcutAdet > 0)
        {
            TempData["Hata"] =
                "Kitap rafta bulunduğu için rezervasyon yapılamaz.";

            return RedirectToAction(
                "Details",
                "Kitaplar",
                new { id = kitapId });
        }

        bool bekleyenRezervasyonVar =
            await _context.Rezervasyonlars
                .AnyAsync(r =>
                    r.KitapId == kitapId &&
                    r.UyeId == uyeId.Value &&
                    r.Durum == "Bekliyor");

        if (bekleyenRezervasyonVar)
        {
            TempData["Hata"] =
                "Bu kitap için zaten bekleyen bir rezervasyonunuz var.";

            return RedirectToAction(
                "Details",
                "Kitaplar",
                new { id = kitapId });
        }

        bool kitapZatenUyede =
            await _context.OduncIslemleris
                .AnyAsync(o =>
                    o.KitapId == kitapId &&
                    o.UyeId == uyeId.Value &&
                    !o.TeslimEdildiMi);

        if (kitapZatenUyede)
        {
            TempData["Hata"] =
                "Bu kitabın başka bir kopyası zaten sizde bulunuyor.";

            return RedirectToAction(
                "Details",
                "Kitaplar",
                new { id = kitapId });
        }

        var rezervasyon = new Rezervasyonlar
        {
            KitapId = kitapId,
            UyeId = uyeId.Value,
            RezervasyonTarihi = DateTime.Now,
            Durum = "Bekliyor"
        };

        _context.Rezervasyonlars.Add(
            rezervasyon);

        try
        {
            await _context.SaveChangesAsync();

            TempData["Basarili"] =
                "Kitap başarıyla ayırtıldı.";

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            TempData["Hata"] =
                "Rezervasyon kaydedilemedi veya bu kitap zaten ayırtılmış.";

            return RedirectToAction(
                "Details",
                "Kitaplar",
                new { id = kitapId });
        }
    }

    // Üye kendi rezervasyonunu,
    // admin istediği rezervasyonu iptal edebilir.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Iptal(int id)
    {
        var rezervasyon =
            await _context.Rezervasyonlars
                .FirstOrDefaultAsync(r =>
                    r.RezervasyonId == id);

        if (rezervasyon == null)
        {
            TempData["Hata"] =
                "Rezervasyon bulunamadı.";

            return RedirectToAction(nameof(Index));
        }

        if (User.IsInRole("Uye"))
        {
            int? uyeId = GirisYapanUyeId();

            if (uyeId == null ||
                rezervasyon.UyeId != uyeId.Value)
            {
                return Forbid();
            }
        }

        if (rezervasyon.Durum != "Bekliyor")
        {
            TempData["Hata"] =
                "Yalnızca bekleyen rezervasyonlar iptal edilebilir.";

            return RedirectToAction(nameof(Index));
        }

        rezervasyon.Durum = "İptal";

        await _context.SaveChangesAsync();

        TempData["Basarili"] =
            "Rezervasyon iptal edildi.";

        return RedirectToAction(nameof(Index));
    }

    // Rezervasyonu barkodlu fiziksel kopyayla
    // ödünç işlemine dönüştürür.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Tamamla(int id)
    {
        var rezervasyon = await _context.Rezervasyonlars
            .Include(r => r.Kitap)
            .Include(r => r.Uye)
            .FirstOrDefaultAsync(r =>
                r.RezervasyonId == id);

        if (rezervasyon == null)
        {
            TempData["Hata"] =
                "Rezervasyon bulunamadı.";

            return RedirectToAction(nameof(Index));
        }

        if (rezervasyon.Durum != "Bekliyor")
        {
            TempData["Hata"] =
                "Yalnızca bekleyen rezervasyonlar ödünç işlemine dönüştürülebilir.";

            return RedirectToAction(nameof(Index));
        }

        if (!rezervasyon.Uye.AktifMi)
        {
            TempData["Hata"] =
                "Üye pasif durumda olduğu için kitap ödünç verilemez.";

            return RedirectToAction(nameof(Index));
        }

        if (!rezervasyon.Kitap.AktifMi)
        {
            rezervasyon.Durum = "İptal";

            await _context.SaveChangesAsync();

            TempData["Hata"] =
                "Kitap pasif olduğu için rezervasyon iptal edildi.";

            return RedirectToAction(nameof(Index));
        }

        if (rezervasyon.Kitap.MevcutAdet <= 0)
        {
            TempData["Hata"] =
                "Kitap henüz stokta bulunmuyor. İade sonrasında tekrar deneyiniz.";

            return RedirectToAction(nameof(Index));
        }

        var raftakiKopya =
            await _context.KitapKopyalaris
                .Where(k =>
                    k.KitapId ==
                    rezervasyon.KitapId &&
                    k.Durum == "Rafta")
                .OrderBy(k => k.KopyaId)
                .FirstOrDefaultAsync();

        if (raftakiKopya == null)
        {
            TempData["Hata"] =
                "Rafta barkodlu fiziksel kopya bulunamadı.";

            return RedirectToAction(nameof(Index));
        }

        bool kitapZatenUyede =
            await _context.OduncIslemleris
                .AnyAsync(o =>
                    o.UyeId ==
                    rezervasyon.UyeId &&
                    o.KitapId ==
                    rezervasyon.KitapId &&
                    !o.TeslimEdildiMi);

        if (kitapZatenUyede)
        {
            TempData["Hata"] =
                "Bu kitabın başka bir kopyası zaten ilgili üyede bulunuyor.";

            return RedirectToAction(nameof(Index));
        }

        DateTime verilisTarihi = DateTime.Now;

        var yeniOduncIslemi =
            new OduncIslemleri
            {
                KitapId =
                    rezervasyon.KitapId,

                KopyaId =
                    raftakiKopya.KopyaId,

                UyeId =
                    rezervasyon.UyeId,

                VerilisTarihi =
                    verilisTarihi,

                SonTeslimTarihi =
                    verilisTarihi.AddDays(14),

                IadeTarihi = null,
                TeslimEdildiMi = false,
                CezaTutari = 0
            };

        raftakiKopya.Durum = "Ödünçte";

        rezervasyon.Kitap.MevcutAdet--;

        rezervasyon.Durum = "Tamamlandı";

        _context.OduncIslemleris.Add(
            yeniOduncIslemi);

        try
        {
            await _context.SaveChangesAsync();

            TempData["BasariliMesaj"] =
                $"Rezervasyon tamamlandı. {raftakiKopya.Barkod} barkodlu kopya üyeye 14 gün süreyle verildi.";

            return RedirectToAction(
                "Details",
                "OduncIslemleri",
                new
                {
                    id = yeniOduncIslemi.OduncId
                });
        }
        catch (DbUpdateException)
        {
            TempData["Hata"] =
                "Rezervasyon ödünç işlemine dönüştürülürken veritabanı hatası oluştu.";

            return RedirectToAction(nameof(Index));
        }
    }

    // Admin rezervasyon formundaki listeleri hazırlar.
    private async Task FormListeleriniHazirla(
        int? secilenKitapId = null,
        int? secilenUyeId = null)
    {
        var kitaplar =
            await _context.Kitaplars
                .Where(k =>
                    k.AktifMi &&
                    k.MevcutAdet <= 0)
                .OrderBy(k =>
                    k.Baslik)
                .Select(k => new
                {
                    k.KitapId,

                    GorunenAd =
                        "ID: " + k.KitapId +
                        " - " + k.Baslik
                })
                .ToListAsync();

        var uyeler =
            await _context.Uyelers
                .Where(u =>
                    u.Rol == "Uye" &&
                    u.AktifMi)
                .OrderBy(u =>
                    u.AdSoyad)
                .Select(u => new
                {
                    u.UyeId,

                    GorunenAd =
                        "ID: " + u.UyeId +
                        " - " + u.AdSoyad +
                        " (" + u.Eposta + ")"
                })
                .ToListAsync();

        ViewData["KitapId"] =
            new SelectList(
                kitaplar,
                "KitapId",
                "GorunenAd",
                secilenKitapId);

        ViewData["UyeId"] =
            new SelectList(
                uyeler,
                "UyeId",
                "GorunenAd",
                secilenUyeId);
    }

    // Giriş yapan üyenin ID bilgisini getirir.
    private int? GirisYapanUyeId()
    {
        string? uyeIdDegeri =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (int.TryParse(
            uyeIdDegeri,
            out int uyeId))
        {
            return uyeId;
        }

        return null;
    }
}