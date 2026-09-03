using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KutuphaneOtomasyonu.Controllers;

[Authorize(Roles = "Admin")]
public class OduncIslemleriController : Controller
{
    private readonly KutuphaneContext _context;

    public OduncIslemleriController(
        KutuphaneContext context)
    {
        _context = context;
    }

    // Ödünç işlemlerini listeler.
    public async Task<IActionResult> Index(
        string? durum,
        string? arama)
    {
        var islemler = _context.OduncIslemleris
            .Include(o => o.Kitap)
            .Include(o => o.Uye)
            .Include(o => o.Kopya)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(arama))
        {
            arama = arama.Trim();

            islemler = islemler.Where(o =>
                o.Kitap.Baslik.Contains(arama) ||
                o.Uye.AdSoyad.Contains(arama) ||
                (o.Kopya != null &&
                 o.Kopya.Barkod.Contains(arama)));
        }

        switch (durum)
        {
            case "oduncte":
                islemler = islemler.Where(o =>
                    !o.TeslimEdildiMi &&
                    o.SonTeslimTarihi.Date >=
                    DateTime.Today);
                break;

            case "geciken":
                islemler = islemler.Where(o =>
                    !o.TeslimEdildiMi &&
                    o.SonTeslimTarihi.Date <
                    DateTime.Today);
                break;

            case "iade":
                islemler = islemler.Where(o =>
                    o.TeslimEdildiMi);
                break;
        }

        ViewBag.Durum = durum;
        ViewBag.Arama = arama;

        var sonuc = await islemler
            .OrderByDescending(o =>
                o.VerilisTarihi)
            .ToListAsync();

        return View(sonuc);
    }

    // Ödünç işlemi detayı
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var islem = await _context.OduncIslemleris
            .Include(o => o.Kitap)
            .Include(o => o.Uye)
            .Include(o => o.Kopya)
            .FirstOrDefaultAsync(o =>
                o.OduncId == id.Value);

        if (islem == null)
        {
            return NotFound();
        }

        return View(islem);
    }

    // Yeni ödünç işlemi sayfası
    [HttpGet]
    public IActionResult Create()
    {
        UyeListesiniHazirla();

        return View();
    }

    // Barkod okutularak kitabı üyeye verir.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string? Barkod,
        int UyeId)
    {
        ViewBag.Barkod = Barkod;

        if (string.IsNullOrWhiteSpace(Barkod))
        {
            ModelState.AddModelError(
                "Barkod",
                "Kitap barkodunu giriniz veya okutunuz.");
        }

        if (UyeId <= 0)
        {
            ModelState.AddModelError(
                "UyeId",
                "Geçerli bir üye seçiniz.");
        }

        KitapKopyalari? kopya = null;

        if (!string.IsNullOrWhiteSpace(Barkod))
        {
            string temizBarkod =
                Barkod.Trim().ToUpperInvariant();

            kopya = await _context.KitapKopyalaris
                .Include(k => k.Kitap)
                .FirstOrDefaultAsync(k =>
                    k.Barkod == temizBarkod);

            if (kopya == null)
            {
                ModelState.AddModelError(
                    "Barkod",
                    "Bu barkoda ait fiziksel kitap kopyası bulunamadı.");
            }
            else if (!kopya.Kitap.AktifMi)
            {
                ModelState.AddModelError(
                    "Barkod",
                    "Bu kitap pasif durumda olduğu için ödünç verilemez.");
            }
            else if (kopya.Durum != "Rafta")
            {
                ModelState.AddModelError(
                    "Barkod",
                    $"Bu kopya ödünç verilemez. Güncel durumu: {kopya.Durum}.");
            }
        }

        var uye = await _context.Uyelers
            .FirstOrDefaultAsync(u =>
                u.UyeId == UyeId);

        if (uye == null)
        {
            ModelState.AddModelError(
                "UyeId",
                "Seçilen üye bulunamadı.");
        }
        else if (uye.Rol != "Uye")
        {
            ModelState.AddModelError(
                "UyeId",
                "Kitap yalnızca üye hesabına verilebilir.");
        }
        else if (!uye.AktifMi)
        {
            ModelState.AddModelError(
                "UyeId",
                "Pasif üyeye kitap ödünç verilemez.");
        }

        if (kopya != null && uye != null)
        {
            bool ayniKitapUyede =
                await _context.OduncIslemleris
                    .AnyAsync(o =>
                        o.KitapId == kopya.KitapId &&
                        o.UyeId == UyeId &&
                        !o.TeslimEdildiMi);

            if (ayniKitapUyede)
            {
                ModelState.AddModelError(
                    "Barkod",
                    "Bu kitabın başka bir kopyası zaten seçilen üyede bulunuyor.");
            }
        }

        if (!ModelState.IsValid ||
            kopya == null ||
            uye == null)
        {
            UyeListesiniHazirla(UyeId);

            return View(new OduncIslemleri
            {
                UyeId = UyeId
            });
        }

        if (kopya.Kitap.MevcutAdet <= 0)
        {
            ModelState.AddModelError(
                "Barkod",
                "Kitabın kullanılabilir stoğu bulunmuyor.");

            UyeListesiniHazirla(UyeId);

            return View(new OduncIslemleri
            {
                UyeId = UyeId
            });
        }

        DateTime verilisTarihi = DateTime.Now;

        var yeniIslem = new OduncIslemleri
        {
            KitapId = kopya.KitapId,
            KopyaId = kopya.KopyaId,
            UyeId = UyeId,
            VerilisTarihi = verilisTarihi,
            SonTeslimTarihi =
                verilisTarihi.AddDays(14),
            IadeTarihi = null,
            CezaTutari = 0,
            TeslimEdildiMi = false
        };

        // Fiziksel kopyayı ödünçte yapar.
        kopya.Durum = "Ödünçte";

        // Kitabın kullanılabilir sayısını azaltır.
        kopya.Kitap.MevcutAdet--;

        _context.OduncIslemleris.Add(
            yeniIslem);

        try
        {
            await _context.SaveChangesAsync();

            TempData["BasariliMesaj"] =
                $"{kopya.Barkod} barkodlu {kopya.Kitap.Baslik} kitabı üyeye verildi.";

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                "",
                "Ödünç işlemi kaydedilemedi. Bu kopya başka bir aktif işlemde kullanılıyor olabilir.");

            UyeListesiniHazirla(UyeId);

            return View(new OduncIslemleri
            {
                UyeId = UyeId
            });
        }
    }

    // Kitabı ve fiziksel kopyayı iade alır.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IadeEt(int id)
    {
        var islem = await _context.OduncIslemleris
            .Include(o => o.Kitap)
            .Include(o => o.Kopya)
            .FirstOrDefaultAsync(o =>
                o.OduncId == id);

        if (islem == null)
        {
            TempData["HataMesaji"] =
                "Ödünç işlemi bulunamadı.";

            return RedirectToAction(nameof(Index));
        }

        if (islem.TeslimEdildiMi)
        {
            TempData["HataMesaji"] =
                "Bu kitap daha önce iade edilmiş.";

            return RedirectToAction(nameof(Index));
        }

        DateTime iadeTarihi = DateTime.Now;

        islem.IadeTarihi = iadeTarihi;
        islem.TeslimEdildiMi = true;

        if (iadeTarihi.Date >
            islem.SonTeslimTarihi.Date)
        {
            int gecikmeGunSayisi =
                (iadeTarihi.Date -
                 islem.SonTeslimTarihi.Date).Days;

            islem.CezaTutari =
                gecikmeGunSayisi * 5;
        }
        else
        {
            islem.CezaTutari = 0;
        }

        // Barkodlu fiziksel kopyayı yeniden rafa alır.
        if (islem.Kopya != null)
        {
            islem.Kopya.Durum = "Rafta";
        }

        if (islem.Kitap.AktifMi)
        {
            islem.Kitap.MevcutAdet++;
        }

        await _context.SaveChangesAsync();

        TempData["BasariliMesaj"] =
            islem.Kopya != null
                ? $"{islem.Kopya.Barkod} barkodlu kitap başarıyla iade edildi."
                : "Kitap başarıyla iade edildi.";

        return RedirectToAction(nameof(Index));
    }

    // Silme onay sayfası
    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var islem = await _context.OduncIslemleris
            .Include(o => o.Kitap)
            .Include(o => o.Uye)
            .Include(o => o.Kopya)
            .FirstOrDefaultAsync(o =>
                o.OduncId == id.Value);

        if (islem == null)
        {
            return NotFound();
        }

        if (!islem.TeslimEdildiMi)
        {
            bool geciktiMi =
                islem.SonTeslimTarihi.Date <
                DateTime.Today;

            TempData["HataMesaji"] = geciktiMi
                ? "Geciken işlem iade edilmeden silinemez."
                : "Ödünçteki işlem iade edilmeden silinemez.";

            return RedirectToAction(nameof(Index));
        }

        bool gecikmeliIade =
            islem.IadeTarihi.HasValue &&
            islem.IadeTarihi.Value.Date >
            islem.SonTeslimTarihi.Date;

        if (gecikmeliIade)
        {
            TempData["HataMesaji"] =
                "Gecikmeli iade edilen ödünç işlemleri silinemez.";

            return RedirectToAction(nameof(Index));
        }

        return View(islem);
    }

    // Yalnızca zamanında iade edilen işlemi siler.
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(
        int id)
    {
        var islem = await _context.OduncIslemleris
            .FirstOrDefaultAsync(o =>
                o.OduncId == id);

        if (islem == null)
        {
            TempData["HataMesaji"] =
                "Silinmek istenen işlem bulunamadı.";

            return RedirectToAction(nameof(Index));
        }

        if (!islem.TeslimEdildiMi)
        {
            TempData["HataMesaji"] =
                "İade edilmemiş ödünç işlemi silinemez.";

            return RedirectToAction(nameof(Index));
        }

        bool gecikmeliIade =
            islem.IadeTarihi.HasValue &&
            islem.IadeTarihi.Value.Date >
            islem.SonTeslimTarihi.Date;

        if (gecikmeliIade)
        {
            TempData["HataMesaji"] =
                "Gecikmeli iade edilen ödünç işlemleri silinemez.";

            return RedirectToAction(nameof(Index));
        }

        _context.OduncIslemleris.Remove(islem);
        await _context.SaveChangesAsync();

        TempData["BasariliMesaj"] =
            "Ödünç işlemi başarıyla silindi.";

        return RedirectToAction(nameof(Index));
    }

    // Yalnızca aktif üyeleri listeler.
    private void UyeListesiniHazirla(
        int? secilenUyeId = null)
    {
        var uyeler = _context.Uyelers
            .Where(u =>
                u.Rol == "Uye" &&
                u.AktifMi)
            .OrderBy(u => u.AdSoyad)
            .Select(u => new
            {
                u.UyeId,

                GorunenAd =
                    "ID: " + u.UyeId +
                    " - " + u.AdSoyad +
                    " (" + u.Eposta + ")"
            })
            .ToList();

        ViewData["UyeId"] =
            new SelectList(
                uyeler,
                "UyeId",
                "GorunenAd",
                secilenUyeId);
    }
}