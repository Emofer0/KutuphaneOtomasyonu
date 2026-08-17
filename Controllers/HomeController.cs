using System.Diagnostics;
using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace KutuphaneOtomasyonu.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly KutuphaneContext _context;

    public HomeController(KutuphaneContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.ToplamKitap = await _context.Kitaplars.CountAsync();

        ViewBag.ToplamUye = await _context.Uyelers.CountAsync();

        ViewBag.OdunctekiKitap = await _context.OduncIslemleris
            .CountAsync(o => !o.TeslimEdildiMi);

        ViewBag.GecikenKitap = await _context.OduncIslemleris
            .CountAsync(o =>
                !o.TeslimEdildiMi &&
                o.SonTeslimTarihi < DateTime.Now);

        var sonIslemler = await _context.OduncIslemleris
            .Include(o => o.Kitap)
            .Include(o => o.Uye)
            .OrderByDescending(o => o.VerilisTarihi)
            .Take(5)
            .ToListAsync();

        return View(sonIslemler);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(
        Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ??
                        HttpContext.TraceIdentifier
        });
    }
}