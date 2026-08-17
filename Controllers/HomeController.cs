using System.Diagnostics;
using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KutuphaneOtomasyonu.Controllers
{
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
            ViewBag.ToplamKitap =
                await _context.Kitaplars.CountAsync();

            ViewBag.ToplamUye =
                await _context.Uyelers
                    .CountAsync(u => u.Rol == "Uye");

            ViewBag.OdunctekiKitap =
                await _context.OduncIslemleris
                    .CountAsync(o => !o.TeslimEdildiMi);

            ViewBag.GecikenKitap =
                await _context.OduncIslemleris
                    .CountAsync(o =>
                        !o.TeslimEdildiMi &&
                        o.SonTeslimTarihi.Date <
                        DateTime.Today);

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
}