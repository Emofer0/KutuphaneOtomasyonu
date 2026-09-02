using System.Diagnostics;
using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KutuphaneOtomasyonu.Controllers;

[Authorize(Roles = "Admin")]
public class HomeController : Controller
{
    private readonly KutuphaneContext _context;

    public HomeController(KutuphaneContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        DateTime bugun = DateTime.Today;

        // Sistemdeki toplam kitap kaydı
        ViewBag.ToplamKitap =
            await _context.Kitaplars.CountAsync();

        // Yalnızca aktif üye sayısı
        ViewBag.ToplamUye =
            await _context.Uyelers.CountAsync(u =>
                u.Rol == "Uye" &&
                u.AktifMi);

        // Teslim edilmemiş fakat henüz gecikmemiş kitaplar
        ViewBag.OdunctekiKitap =
            await _context.OduncIslemleris.CountAsync(o =>
                !o.TeslimEdildiMi &&
                o.SonTeslimTarihi.Date >= bugun);

        // Teslim edilmemiş ve teslim tarihi geçmiş kitaplar
        ViewBag.GecikenKitap =
            await _context.OduncIslemleris.CountAsync(o =>
                !o.TeslimEdildiMi &&
                o.SonTeslimTarihi.Date < bugun);

        // Son beş ödünç işlemi
        var sonIslemler =
            await _context.OduncIslemleris
                .Include(o => o.Kitap)
                .Include(o => o.Uye)
                .OrderByDescending(o =>
                    o.VerilisTarihi)
                .Take(5)
                .ToListAsync();

        return View(sonIslemler);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(
        Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId =
                Activity.Current?.Id ??
                HttpContext.TraceIdentifier
        });
    }
}