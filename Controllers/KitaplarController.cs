using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;

namespace KutuphaneOtomasyonu.Controllers
{
    public class KitaplarController : Controller
    {
        private readonly KutuphaneContext _context;

        public KitaplarController(KutuphaneContext context)
        {
            _context = context;
        }

        // GET: Kitaplar
        public async Task<IActionResult> Index()
        {
            var kutuphaneContext = _context.Kitaplars.Include(k => k.Kategori).Include(k => k.Yazar);
            return View(await kutuphaneContext.ToListAsync());
        }

        // GET: Kitaplar/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var kitaplar = await _context.Kitaplars
                .Include(k => k.Kategori)
                .Include(k => k.Yazar)
                .FirstOrDefaultAsync(m => m.KitapId == id);
            if (kitaplar == null)
            {
                return NotFound();
            }

            return View(kitaplar);
        }

        // GET: Kitaplar/Create
        public IActionResult Create()
        {
            ViewData["KategoriId"] = new SelectList(_context.Kategorilers, "KategoriId", "KategoriId");
            ViewData["YazarId"] = new SelectList(_context.Yazarlars, "YazarId", "YazarId");
            return View();
        }

        // POST: Kitaplar/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("KitapId,Isbn,Baslik,YazarId,KategoriId,RafKonumu,ToplamAdet,MevcutAdet")] Kitaplar kitaplar)
        {
            if (ModelState.IsValid)
            {
                _context.Add(kitaplar);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["KategoriId"] = new SelectList(_context.Kategorilers, "KategoriId", "KategoriId", kitaplar.KategoriId);
            ViewData["YazarId"] = new SelectList(_context.Yazarlars, "YazarId", "YazarId", kitaplar.YazarId);
            return View(kitaplar);
        }

        // GET: Kitaplar/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var kitaplar = await _context.Kitaplars.FindAsync(id);
            if (kitaplar == null)
            {
                return NotFound();
            }
            ViewData["KategoriId"] = new SelectList(_context.Kategorilers, "KategoriId", "KategoriId", kitaplar.KategoriId);
            ViewData["YazarId"] = new SelectList(_context.Yazarlars, "YazarId", "YazarId", kitaplar.YazarId);
            return View(kitaplar);
        }

        // POST: Kitaplar/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("KitapId,Isbn,Baslik,YazarId,KategoriId,RafKonumu,ToplamAdet,MevcutAdet")] Kitaplar kitaplar)
        {
            if (id != kitaplar.KitapId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(kitaplar);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!KitaplarExists(kitaplar.KitapId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["KategoriId"] = new SelectList(_context.Kategorilers, "KategoriId", "KategoriId", kitaplar.KategoriId);
            ViewData["YazarId"] = new SelectList(_context.Yazarlars, "YazarId", "YazarId", kitaplar.YazarId);
            return View(kitaplar);
        }

        // GET: Kitaplar/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var kitaplar = await _context.Kitaplars
                .Include(k => k.Kategori)
                .Include(k => k.Yazar)
                .FirstOrDefaultAsync(m => m.KitapId == id);
            if (kitaplar == null)
            {
                return NotFound();
            }

            return View(kitaplar);
        }

        // POST: Kitaplar/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var kitaplar = await _context.Kitaplars.FindAsync(id);
            if (kitaplar != null)
            {
                _context.Kitaplars.Remove(kitaplar);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool KitaplarExists(int id)
        {
            return _context.Kitaplars.Any(e => e.KitapId == id);
        }
    }
}
