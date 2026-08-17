using System.Security.Claims;
using KutuphaneOtomasyonu.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KutuphaneOtomasyonu.Controllers
{
    [Authorize(Roles = "Uye")]
    public class HesabimController : Controller
    {
        private readonly KutuphaneContext _context;

        public HesabimController(KutuphaneContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Islemlerim()
        {
            var uyeIdDegeri = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(uyeIdDegeri, out var uyeId))
            {
                return RedirectToAction(
                    "Giris",
                    "Hesap");
            }

            var islemler = await _context.OduncIslemleris
                .Include(o => o.Kitap)
                .ThenInclude(k => k.Yazar)
                .Where(o => o.UyeId == uyeId)
                .OrderByDescending(o => o.VerilisTarihi)
                .ToListAsync();

            ViewBag.ToplamIslem = islemler.Count;

            ViewBag.AktifIslem = islemler.Count(o =>
                !o.TeslimEdildiMi);

            ViewBag.GecikenIslem = islemler.Count(o =>
                !o.TeslimEdildiMi &&
                o.SonTeslimTarihi.Date < DateTime.Today);

            return View(islemler);
        }
    }
}