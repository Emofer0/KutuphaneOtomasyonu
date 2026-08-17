using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KutuphaneOtomasyonu.Controllers
{
    public class OduncIslemleriController : Controller
    {
        private readonly KutuphaneContext _context;

        public OduncIslemleriController(KutuphaneContext context)
        {
            _context = context;
        }

        // Ödünç işlemlerini listeler, arar ve filtreler
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
                islemler = islemler.Where(o =>
                    o.Kitap.Baslik.Contains(arama) ||
                    o.Uye.AdSoyad.Contains(arama));
            }

            switch (durum)
            {
                case "oduncte":
                    islemler = islemler.Where(o =>
                        !o.TeslimEdildiMi &&
                        o.SonTeslimTarihi.Date >= DateTime.Today);
                    break;

                case "geciken":
                    islemler = islemler.Where(o =>
                        !o.TeslimEdildiMi &&
                        o.SonTeslimTarihi.Date < DateTime.Today);
                    break;

                case "iade":
                    islemler = islemler.Where(o =>
                        o.TeslimEdildiMi);
                    break;
            }

            ViewBag.Durum = durum;
            ViewBag.Arama = arama;

            return View(await islemler
                .OrderByDescending(o => o.VerilisTarihi)
                .ToListAsync());
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
                .FirstOrDefaultAsync(o => o.OduncId == id);

            if (islem == null)
            {
                return NotFound();
            }

            return View(islem);
        }

        // Ödünç verme sayfası
        public IActionResult Create()
        {
            FormListeleriniHazirla();
            return View();
        }

        // Kitabı üyeye ödünç verir
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

            var yeniIslem = new OduncIslemleri
            {
                KitapId = KitapId,
                UyeId = UyeId,
                VerilisTarihi = DateTime.Now,
                SonTeslimTarihi = DateTime.Now.AddDays(14),
                IadeTarihi = null,
                CezaTutari = 0,
                TeslimEdildiMi = false
            };

            kitap!.MevcutAdet--;

            _context.OduncIslemleris.Add(yeniIslem);
            await _context.SaveChangesAsync();

            TempData["BasariliMesaj"] =
                "Kitap başarıyla ödünç verildi.";

            return RedirectToAction(nameof(Index));
        }

        // Kitabı iade eder
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
                return NotFound();
            }

            if (islem.TeslimEdildiMi)
            {
                TempData["HataMesaji"] =
                    "Bu kitap daha önce iade edilmiş.";

                return RedirectToAction(nameof(Index));
            }

            var iadeTarihi = DateTime.Now;

            islem.IadeTarihi = iadeTarihi;
            islem.TeslimEdildiMi = true;

            if (iadeTarihi.Date >
                islem.SonTeslimTarihi.Date)
            {
                var gecikmeGunSayisi =
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
                    o.OduncId == id);

            if (islem == null)
            {
                return NotFound();
            }

            return View(islem);
        }

        // Ödünç işlemini siler
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var islem = await _context.OduncIslemleris
                .Include(o => o.Kitap)
                .FirstOrDefaultAsync(o =>
                    o.OduncId == id);

            if (islem == null)
            {
                return RedirectToAction(nameof(Index));
            }

            // İade edilmemiş kayıt silinirse stok geri eklenir
            if (!islem.TeslimEdildiMi &&
                islem.Kitap != null)
            {
                islem.Kitap.MevcutAdet++;
            }

            _context.OduncIslemleris.Remove(islem);
            await _context.SaveChangesAsync();

            TempData["BasariliMesaj"] =
                "Ödünç işlemi silindi.";

            return RedirectToAction(nameof(Index));
        }

        // Kitap ve üye seçim listelerini hazırlar
        private void FormListeleriniHazirla(
            int? secilenKitapId = null,
            int? secilenUyeId = null)
        {
            ViewData["KitapId"] = new SelectList(
                _context.Kitaplars
                    .Where(k => k.MevcutAdet > 0)
                    .OrderBy(k => k.Baslik),
                "KitapId",
                "Baslik",
                secilenKitapId);

            ViewData["UyeId"] = new SelectList(
                _context.Uyelers
                    .OrderBy(u => u.AdSoyad),
                "UyeId",
                "AdSoyad",
                secilenUyeId);
        }
    }
}