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
    public class OduncIslemleriController : Controller
    {
        private readonly KutuphaneContext _context;

        public OduncIslemleriController(KutuphaneContext context)
        {
            _context = context;
        }

        // GET: OduncIslemleri
        public async Task<IActionResult> Index()
        {
            var kutuphaneContext = _context.OduncIslemleris.Include(o => o.Kitap).Include(o => o.Uye);
            return View(await kutuphaneContext.ToListAsync());
        }

        // GET: OduncIslemleri/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var oduncIslemleri = await _context.OduncIslemleris
                .Include(o => o.Kitap)
                .Include(o => o.Uye)
                .FirstOrDefaultAsync(m => m.OduncId == id);
            if (oduncIslemleri == null)
            {
                return NotFound();
            }

            return View(oduncIslemleri);
        }

        // GET: OduncIslemleri/Create
        public IActionResult Create()
        {
            ViewData["KitapId"] = new SelectList(_context.Kitaplars, "KitapId", "KitapId");
            ViewData["UyeId"] = new SelectList(_context.Uyelers, "UyeId", "UyeId");
            return View();
        }

        // POST: OduncIslemleri/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OduncId,KitapId,UyeId,VerilisTarihi,SonTeslimTarihi,IadeTarihi,CezaTutari,TeslimEdildiMi")] OduncIslemleri oduncIslemleri)
        {
            if (ModelState.IsValid)
            {
                _context.Add(oduncIslemleri);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["KitapId"] = new SelectList(_context.Kitaplars, "KitapId", "KitapId", oduncIslemleri.KitapId);
            ViewData["UyeId"] = new SelectList(_context.Uyelers, "UyeId", "UyeId", oduncIslemleri.UyeId);
            return View(oduncIslemleri);
        }

        // GET: OduncIslemleri/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var oduncIslemleri = await _context.OduncIslemleris.FindAsync(id);
            if (oduncIslemleri == null)
            {
                return NotFound();
            }
            ViewData["KitapId"] = new SelectList(_context.Kitaplars, "KitapId", "KitapId", oduncIslemleri.KitapId);
            ViewData["UyeId"] = new SelectList(_context.Uyelers, "UyeId", "UyeId", oduncIslemleri.UyeId);
            return View(oduncIslemleri);
        }

        // POST: OduncIslemleri/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("OduncId,KitapId,UyeId,VerilisTarihi,SonTeslimTarihi,IadeTarihi,CezaTutari,TeslimEdildiMi")] OduncIslemleri oduncIslemleri)
        {
            if (id != oduncIslemleri.OduncId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(oduncIslemleri);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OduncIslemleriExists(oduncIslemleri.OduncId))
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
            ViewData["KitapId"] = new SelectList(_context.Kitaplars, "KitapId", "KitapId", oduncIslemleri.KitapId);
            ViewData["UyeId"] = new SelectList(_context.Uyelers, "UyeId", "UyeId", oduncIslemleri.UyeId);
            return View(oduncIslemleri);
        }

        // GET: OduncIslemleri/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var oduncIslemleri = await _context.OduncIslemleris
                .Include(o => o.Kitap)
                .Include(o => o.Uye)
                .FirstOrDefaultAsync(m => m.OduncId == id);
            if (oduncIslemleri == null)
            {
                return NotFound();
            }

            return View(oduncIslemleri);
        }

        // POST: OduncIslemleri/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var oduncIslemleri = await _context.OduncIslemleris.FindAsync(id);
            if (oduncIslemleri != null)
            {
                _context.OduncIslemleris.Remove(oduncIslemleri);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OduncIslemleriExists(int id)
        {
            return _context.OduncIslemleris.Any(e => e.OduncId == id);
        }
    }
}
