using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KutuphaneOtomasyonu.Controllers;

[Authorize(Roles = "Admin")]
public class UyelerController : Controller
{
    private readonly KutuphaneContext _context;

    public UyelerController(KutuphaneContext context)
    {
        _context = context;
    }

    // Üyeleri listeleme ve arama
    public async Task<IActionResult> Index(string? arama)
    {
        var sorgu = _context.Uyelers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(arama))
        {
            arama = arama.Trim();

            sorgu = sorgu.Where(u =>
                u.AdSoyad.Contains(arama) ||
                u.Eposta.Contains(arama) ||
                (u.Telefon != null && u.Telefon.Contains(arama)));
        }

        var uyeler = await sorgu
            .OrderByDescending(u => u.AktifMi)
            .ThenBy(u => u.AdSoyad)
            .ToListAsync();

        return View(uyeler);
    }

    // Üye detayı
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var uye = await _context.Uyelers
            .Include(u => u.OduncIslemleris)
                .ThenInclude(o => o.Kitap)
            .FirstOrDefaultAsync(u => u.UyeId == id);

        if (uye == null)
        {
            return NotFound();
        }

        return View(uye);
    }

    // Yeni üye formu
    public IActionResult Create()
    {
        return View();
    }

    // Yeni üye kaydetme
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Uyeler uye)
    {
        bool epostaKullaniliyor = await _context.Uyelers
            .AnyAsync(u => u.Eposta == uye.Eposta);

        if (epostaKullaniliyor)
        {
            ModelState.AddModelError(
                nameof(uye.Eposta),
                "Bu e-posta adresi başka bir hesap tarafından kullanılıyor."
            );
        }

        if (!ModelState.IsValid)
        {
            return View(uye);
        }

        uye.KayitTarihi = DateTime.Now;
        uye.AktifMi = true;

        if (string.IsNullOrWhiteSpace(uye.Rol))
        {
            uye.Rol = "Uye";
        }

        _context.Uyelers.Add(uye);
        await _context.SaveChangesAsync();

        TempData["Basarili"] = "Yeni üye başarıyla oluşturuldu.";

        return RedirectToAction(nameof(Index));
    }

    // Üye düzenleme formu
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var uye = await _context.Uyelers.FindAsync(id);

        if (uye == null)
        {
            return NotFound();
        }

        return View(uye);
    }

    // Üye bilgilerini güncelleme
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Uyeler uye)
    {
        if (id != uye.UyeId)
        {
            return NotFound();
        }

        bool epostaKullaniliyor = await _context.Uyelers
            .AnyAsync(u =>
                u.Eposta == uye.Eposta &&
                u.UyeId != uye.UyeId);

        if (epostaKullaniliyor)
        {
            ModelState.AddModelError(
                nameof(uye.Eposta),
                "Bu e-posta adresi başka bir hesap tarafından kullanılıyor."
            );
        }

        if (!ModelState.IsValid)
        {
            return View(uye);
        }

        var mevcutUye = await _context.Uyelers
            .FirstOrDefaultAsync(u => u.UyeId == id);

        if (mevcutUye == null)
        {
            return NotFound();
        }

        mevcutUye.AdSoyad = uye.AdSoyad;
        mevcutUye.Eposta = uye.Eposta;
        mevcutUye.Telefon = uye.Telefon;

        // Rol ve kayıt tarihi mevcut hâliyle korunur.
        await _context.SaveChangesAsync();

        TempData["Basarili"] = "Üye bilgileri güncellendi.";

        return RedirectToAction(nameof(Index));
    }

    // Üyeyi pasife alma
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PasifeAl(int id)
    {
        var uye = await _context.Uyelers
            .FirstOrDefaultAsync(u => u.UyeId == id);

        if (uye == null)
        {
            TempData["Hata"] = "Üye bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        if (uye.Rol == "Admin")
        {
            TempData["Hata"] = "Admin hesabı pasife alınamaz.";
            return RedirectToAction(nameof(Index));
        }

        if (!uye.AktifMi)
        {
            TempData["Hata"] = "Bu üye zaten pasif durumda.";
            return RedirectToAction(nameof(Index));
        }

        uye.AktifMi = false;

        // Üyenin bekleyen rezervasyonlarını iptal eder.
        var bekleyenRezervasyonlar =
            await _context.Rezervasyonlars
                .Where(r =>
                    r.UyeId == id &&
                    r.Durum == "Bekliyor")
                .ToListAsync();

        foreach (var rezervasyon in bekleyenRezervasyonlar)
        {
            rezervasyon.Durum = "İptal";
        }

        await _context.SaveChangesAsync();

        TempData["Basarili"] =
            "Üye pasife alındı. Bekleyen rezervasyonları iptal edildi.";

        return RedirectToAction(nameof(Index));
    }

    // Pasif üyeyi yeniden aktifleştirme
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AktifEt(int id)
    {
        var uye = await _context.Uyelers
            .FirstOrDefaultAsync(u => u.UyeId == id);

        if (uye == null)
        {
            TempData["Hata"] = "Üye bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        if (uye.AktifMi)
        {
            TempData["Hata"] = "Bu üye zaten aktif durumda.";
            return RedirectToAction(nameof(Index));
        }

        uye.AktifMi = true;

        await _context.SaveChangesAsync();

        TempData["Basarili"] = "Üye yeniden aktif edildi.";

        return RedirectToAction(nameof(Index));
    }

    private bool UyeVarMi(int id)
    {
        return _context.Uyelers.Any(u => u.UyeId == id);
    }
}