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

    // Ödünç işlemlerini listeler, arar ve filtreler.
    public async Task<IActionResult> Index(
        string? durum,
        string? arama)
    {
        var islemler = _context.OduncIslemleris
            .Include(o => o.Kitap)
            .Include(o => o.Uye)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(arama))
        {
            arama = arama.Trim();

            islemler = islemler.Where(o =>
                o.Kitap.Baslik.Contains(arama) ||
                o.Uye.AdSoyad.Contains(arama));
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

    // Ödünç işlemi detay sayfası
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var islem = await _context.OduncIslemleris
            .Include(o => o.Kitap)
            .Include(o => o.Uye)
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
        FormListeleriniHazirla();

        return View();
    }

    // Kitabı üyeye ödünç verme
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        int KitapId,
        int UyeId)
    {
        var kitap = await _context.Kitaplars
            .FirstOrDefaultAsync(k =>
                k.KitapId == KitapId);

        var uye = await _context.Uyelers
            .FirstOrDefaultAsync(u =>
                u.UyeId == UyeId);

        if (kitap == null)
        {
            ModelState.AddModelError(
                "KitapId",
                "Geçerli bir kitap seçiniz.");
        }
        else if (kitap.MevcutAdet <= 0)
        {
            ModelState.AddModelError(
                "KitapId",
                "Bu kitabın mevcut stoğu bulunmamaktadır.");
        }

        if (uye == null)
        {
            ModelState.AddModelError(
                "UyeId",
                "Geçerli bir üye seçiniz.");
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

        bool ayniKitapUyede =
            await _context.OduncIslemleris
                .AnyAsync(o =>
                    o.KitapId == KitapId &&
                    o.UyeId == UyeId &&
                    !o.TeslimEdildiMi);

        if (ayniKitapUyede)
        {
            ModelState.AddModelError(
                "KitapId",
                "Bu kitap zaten seçilen üyede ödünç olarak bulunuyor.");
        }

        if (!ModelState.IsValid)
        {
            FormListeleriniHazirla(
                KitapId,
                UyeId);

            return View(new OduncIslemleri
            {
                KitapId = KitapId,
                UyeId = UyeId
            });
        }

        DateTime verilisTarihi = DateTime.Now;

        var yeniIslem = new OduncIslemleri
        {
            KitapId = KitapId,
            UyeId = UyeId,
            VerilisTarihi = verilisTarihi,
            SonTeslimTarihi =
                verilisTarihi.AddDays(14),
            IadeTarihi = null,
            CezaTutari = 0,
            TeslimEdildiMi = false
        };

        kitap!.MevcutAdet--;

        _context.OduncIslemleris.Add(
            yeniIslem);

        await _context.SaveChangesAsync();

        TempData["BasariliMesaj"] =
            "Kitap başarıyla ödünç verildi.";

        return RedirectToAction(nameof(Index));
    }

    // Kitabı iade alma
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IadeEt(int id)
    {
        var islem = await _context.OduncIslemleris
            .Include(o => o.Kitap)
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

        if (islem.Kitap != null)
        {
            islem.Kitap.MevcutAdet++;
        }

        await _context.SaveChangesAsync();

        TempData["BasariliMesaj"] =
            "Kitap başarıyla iade edildi.";

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
            .FirstOrDefaultAsync(o =>
                o.OduncId == id.Value);

        if (islem == null)
        {
            return NotFound();
        }

        // Ödünçte veya geciken kitap silinemez.
        if (!islem.TeslimEdildiMi)
        {
            bool geciktiMi =
                islem.SonTeslimTarihi.Date <
                DateTime.Today;

            TempData["HataMesaji"] = geciktiMi
                ? "Geciken ödünç işlemi iade edilmeden silinemez."
                : "Ödünçte olan işlem kitap iade edilmeden silinemez.";

            return RedirectToAction(nameof(Index));
        }

        // Gecikmeli iade edilen kayıt silinemez.
        bool gecikmeliIadeEdildi =
            islem.IadeTarihi.HasValue &&
            islem.IadeTarihi.Value.Date >
            islem.SonTeslimTarihi.Date;

        if (gecikmeliIadeEdildi)
        {
            TempData["HataMesaji"] =
                "Gecikmeli iade edilen ödünç işlemleri silinemez.";

            return RedirectToAction(nameof(Index));
        }

        return View(islem);
    }

    // Yalnızca zamanında iade edilmiş işlemi siler.
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

        // Ödünçte veya geciken kitap silinemez.
        if (!islem.TeslimEdildiMi)
        {
            bool geciktiMi =
                islem.SonTeslimTarihi.Date <
                DateTime.Today;

            TempData["HataMesaji"] = geciktiMi
                ? "Geciken ödünç işlemi iade edilmeden silinemez."
                : "Ödünçte olan işlem kitap iade edilmeden silinemez.";

            return RedirectToAction(nameof(Index));
        }

        // Gecikmeli iade edilen kayıt silinemez.
        bool gecikmeliIadeEdildi =
            islem.IadeTarihi.HasValue &&
            islem.IadeTarihi.Value.Date >
            islem.SonTeslimTarihi.Date;

        if (gecikmeliIadeEdildi)
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

    // Kitap ve üye seçim listelerini hazırlar
    private void FormListeleriniHazirla(
        int? secilenKitapId = null,
        int? secilenUyeId = null)
    {
        var kitaplar = _context.Kitaplars
            .Where(k =>
                k.MevcutAdet > 0)
            .OrderBy(k =>
                k.Baslik)
            .Select(k => new
            {
                k.KitapId,

                GorunenAd =
                    "ID: " + k.KitapId +
                    " - " + k.Baslik +
                    " (Mevcut: " +
                    k.MevcutAdet + ")"
            })
            .ToList();

        var uyeler = _context.Uyelers
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
            .ToList();

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
}