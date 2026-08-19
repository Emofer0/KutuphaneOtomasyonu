using System.Security.Claims;
using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KutuphaneOtomasyonu.Controllers;

[Authorize(Roles = "Admin,Uye")]
public class RezervasyonlarController : Controller
{
    private readonly KutuphaneContext _context;

    public RezervasyonlarController(KutuphaneContext context)
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

            sorgu = sorgu.Where(r => r.UyeId == uyeId.Value);
        }

        var rezervasyonlar = await sorgu
            .OrderBy(r => r.Durum == "Bekliyor" ? 0 : 1)
            .ThenByDescending(r => r.RezervasyonTarihi)
            .ToListAsync();

        return View(rezervasyonlar);
    }

    // Üyenin kitap ayırtması
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Uye")]
    public async Task<IActionResult> Ayirt(int kitapId)
    {
        int? uyeId = GirisYapanUyeId();

        if (uyeId == null)
        {
            return Forbid();
        }

        var kitap = await _context.Kitaplars
            .FirstOrDefaultAsync(k => k.KitapId == kitapId);

        if (kitap == null)
        {
            return NotFound();
        }

        if (kitap.MevcutAdet > 0)
        {
            TempData["Hata"] =
                "Bu kitap şu anda mevcut olduğu için rezervasyon yapılamaz.";

            return RedirectToAction(
                "Details",
                "Kitaplar",
                new { id = kitapId }
            );
        }

        bool aktifRezervasyonVar = await _context.Rezervasyonlars.AnyAsync(r =>
            r.KitapId == kitapId &&
            r.UyeId == uyeId.Value &&
            r.Durum == "Bekliyor");

        if (aktifRezervasyonVar)
        {
            TempData["Hata"] =
                "Bu kitap için zaten bekleyen bir rezervasyonunuz var.";

            return RedirectToAction(
                "Details",
                "Kitaplar",
                new { id = kitapId }
            );
        }

        var rezervasyon = new Rezervasyonlar
        {
            KitapId = kitapId,
            UyeId = uyeId.Value,
            RezervasyonTarihi = DateTime.Now,
            Durum = "Bekliyor"
        };

        _context.Rezervasyonlars.Add(rezervasyon);

        try
        {
            await _context.SaveChangesAsync();
            TempData["Basarili"] = "Kitap başarıyla ayırtıldı.";
        }
        catch (DbUpdateException)
        {
            TempData["Hata"] =
                "Rezervasyon kaydedilemedi veya bu kitap zaten ayırtılmış.";
        }

        return RedirectToAction(nameof(Index));
    }

    // Üye kendi rezervasyonunu, admin istediği rezervasyonu iptal edebilir.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Iptal(int id)
    {
        var rezervasyon = await _context.Rezervasyonlars
            .FirstOrDefaultAsync(r => r.RezervasyonId == id);

        if (rezervasyon == null)
        {
            return NotFound();
        }

        if (User.IsInRole("Uye"))
        {
            int? uyeId = GirisYapanUyeId();

            if (uyeId == null || rezervasyon.UyeId != uyeId.Value)
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

        TempData["Basarili"] = "Rezervasyon iptal edildi.";

        return RedirectToAction(nameof(Index));
    }

    // Rezervasyonu tamamlandı olarak işaretleme yalnızca admine açıktır.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Tamamla(int id)
    {
        var rezervasyon = await _context.Rezervasyonlars
            .FirstOrDefaultAsync(r => r.RezervasyonId == id);

        if (rezervasyon == null)
        {
            return NotFound();
        }

        if (rezervasyon.Durum != "Bekliyor")
        {
            TempData["Hata"] =
                "Yalnızca bekleyen rezervasyonlar tamamlanabilir.";

            return RedirectToAction(nameof(Index));
        }

        rezervasyon.Durum = "Tamamlandı";
        await _context.SaveChangesAsync();

        TempData["Basarili"] =
            "Rezervasyon tamamlandı olarak işaretlendi.";

        return RedirectToAction(nameof(Index));
    }

    private int? GirisYapanUyeId()
    {
        string? uyeIdDegeri =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (int.TryParse(uyeIdDegeri, out int uyeId))
        {
            return uyeId;
        }

        return null;
    }
}