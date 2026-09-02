using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KutuphaneOtomasyonu.Controllers;

[Authorize(Roles = "Admin")]
public class UyelerController : Controller
{
    private readonly KutuphaneContext _context;
    private readonly PasswordHasher<Uyeler> _passwordHasher;

    public UyelerController(KutuphaneContext context)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<Uyeler>();
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
                (u.Telefon != null &&
                 u.Telefon.Contains(arama)));
        }

        var uyeler = await sorgu
            .OrderByDescending(u => u.AktifMi)
            .ThenBy(u => u.AdSoyad)
            .ToListAsync();

        return View(uyeler);
    }

    // Üye detayları
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var uye = await _context.Uyelers
            .Include(u => u.OduncIslemleris)
                .ThenInclude(o => o.Kitap)
            .FirstOrDefaultAsync(u =>
                u.UyeId == id.Value);

        if (uye == null)
        {
            return NotFound();
        }

        return View(uye);
    }

    // Yeni üye sayfası
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    // Yeni üyeyi kaydetme
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string? adSoyad,
        string? eposta,
        string? telefon,
        string? sifre)
    {
        var formModeli = new Uyeler
        {
            AdSoyad = adSoyad?.Trim() ?? "",
            Eposta = eposta?.Trim() ?? "",
            Telefon = telefon?.Trim(),
            Sifre = "",
            Rol = "Uye",
            KayitTarihi = DateTime.Now,
            AktifMi = true
        };

        if (string.IsNullOrWhiteSpace(adSoyad))
        {
            ModelState.AddModelError(
                "AdSoyad",
                "Ad soyad alanı zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(eposta))
        {
            ModelState.AddModelError(
                "Eposta",
                "E-posta alanı zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(sifre))
        {
            ModelState.AddModelError(
                "Sifre",
                "Şifre alanı zorunludur.");
        }
        else if (sifre.Length < 6)
        {
            ModelState.AddModelError(
                "Sifre",
                "Şifre en az 6 karakter olmalıdır.");
        }

        if (!string.IsNullOrWhiteSpace(eposta))
        {
            string temizEposta = eposta.Trim();

            bool epostaKullaniliyor =
                await _context.Uyelers.AnyAsync(u =>
                    u.Eposta == temizEposta);

            if (epostaKullaniliyor)
            {
                ModelState.AddModelError(
                    "Eposta",
                    "Bu e-posta adresi başka bir hesap tarafından kullanılıyor.");
            }
        }

        if (!ModelState.IsValid)
        {
            return View(formModeli);
        }

        var yeniUye = new Uyeler
        {
            AdSoyad = adSoyad!.Trim(),
            Eposta = eposta!.Trim(),
            Telefon = string.IsNullOrWhiteSpace(telefon)
                ? null
                : telefon.Trim(),
            KayitTarihi = DateTime.Now,
            Rol = "Uye",
            AktifMi = true,
            Sifre = ""
        };

        yeniUye.Sifre = _passwordHasher.HashPassword(
            yeniUye,
            sifre!);

        _context.Uyelers.Add(yeniUye);

        try
        {
            await _context.SaveChangesAsync();

            TempData["Basarili"] =
                "Yeni üye hesabı başarıyla oluşturuldu.";

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                "",
                "Üye kaydedilemedi. E-posta adresinin daha önce kullanılmadığından emin olun.");

            return View(formModeli);
        }
    }

    // Üye düzenleme sayfası
    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var uye = await _context.Uyelers
            .FindAsync(id.Value);

        if (uye == null)
        {
            return NotFound();
        }

        return View(uye);
    }

    // Üye bilgilerini güncelleme
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        string? adSoyad,
        string? eposta,
        string? telefon)
    {
        var mevcutUye = await _context.Uyelers
            .FirstOrDefaultAsync(u =>
                u.UyeId == id);

        if (mevcutUye == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(adSoyad))
        {
            ModelState.AddModelError(
                "AdSoyad",
                "Ad soyad alanı zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(eposta))
        {
            ModelState.AddModelError(
                "Eposta",
                "E-posta alanı zorunludur.");
        }

        if (!string.IsNullOrWhiteSpace(eposta))
        {
            string temizEposta = eposta.Trim();

            bool epostaKullaniliyor =
                await _context.Uyelers.AnyAsync(u =>
                    u.Eposta == temizEposta &&
                    u.UyeId != id);

            if (epostaKullaniliyor)
            {
                ModelState.AddModelError(
                    "Eposta",
                    "Bu e-posta adresi başka bir hesap tarafından kullanılıyor.");
            }
        }

        if (!ModelState.IsValid)
        {
            mevcutUye.AdSoyad = adSoyad?.Trim() ?? "";
            mevcutUye.Eposta = eposta?.Trim() ?? "";
            mevcutUye.Telefon = telefon?.Trim();

            return View(mevcutUye);
        }

        mevcutUye.AdSoyad = adSoyad!.Trim();
        mevcutUye.Eposta = eposta!.Trim();
        mevcutUye.Telefon =
            string.IsNullOrWhiteSpace(telefon)
                ? null
                : telefon.Trim();

        await _context.SaveChangesAsync();

        TempData["Basarili"] =
            "Üye bilgileri başarıyla güncellendi.";

        return RedirectToAction(nameof(Index));
    }

    // Üyeyi pasife alma
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PasifeAl(int id)
    {
        var uye = await _context.Uyelers
            .FirstOrDefaultAsync(u =>
                u.UyeId == id);

        if (uye == null)
        {
            TempData["Hata"] = "Üye bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        if (uye.Rol == "Admin")
        {
            TempData["Hata"] =
                "Admin hesabı pasife alınamaz.";

            return RedirectToAction(nameof(Index));
        }

        if (!uye.AktifMi)
        {
            TempData["Hata"] =
                "Bu üye zaten pasif durumda.";

            return RedirectToAction(nameof(Index));
        }

        var teslimEdilmemisIslemler =
            await _context.OduncIslemleris
                .Where(o =>
                    o.UyeId == id &&
                    !o.TeslimEdildiMi)
                .ToListAsync();

        if (teslimEdilmemisIslemler.Any())
        {
            bool gecikenKitapVar =
                teslimEdilmemisIslemler.Any(o =>
                    o.SonTeslimTarihi.Date <
                    DateTime.Today);

            TempData["Hata"] = gecikenKitapVar
                ? "Üyenin gecikmiş ve teslim edilmemiş kitabı bulunduğu için pasife alınamaz."
                : "Üyenin teslim edilmemiş kitabı bulunduğu için pasife alınamaz.";

            return RedirectToAction(nameof(Index));
        }

        uye.AktifMi = false;

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
            bekleyenRezervasyonlar.Any()
                ? $"Üye pasife alındı ve {bekleyenRezervasyonlar.Count} bekleyen rezervasyonu iptal edildi."
                : "Üye başarıyla pasife alındı.";

        return RedirectToAction(nameof(Index));
    }

    // Pasif üyeyi yeniden aktifleştirme
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AktifEt(int id)
    {
        var uye = await _context.Uyelers
            .FirstOrDefaultAsync(u =>
                u.UyeId == id);

        if (uye == null)
        {
            TempData["Hata"] = "Üye bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        if (uye.AktifMi)
        {
            TempData["Hata"] =
                "Bu üye zaten aktif durumda.";

            return RedirectToAction(nameof(Index));
        }

        uye.AktifMi = true;

        await _context.SaveChangesAsync();

        TempData["Basarili"] =
            "Üye yeniden aktif edildi.";

        return RedirectToAction(nameof(Index));
    }
}