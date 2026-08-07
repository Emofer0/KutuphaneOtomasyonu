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
    public class KategorilerController : Controller
    {
        private readonly KutuphaneContext _context;

        public KategorilerController(KutuphaneContext context)
        {
            _context = context;
        }

        // GET: Kategoriler
        public async Task<IActionResult> Index()
        {
            return View(await _context.Kategorilers.ToListAsync());
        }

        // GET: Kategoriler/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var kategoriler = await _context.Kategorilers
                .FirstOrDefaultAsync(m => m.KategoriId == id);
            if (kategoriler == null)
            {
                return NotFound();
            }

            return View(kategoriler);
        }

        // GET: Kategoriler/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Kategoriler/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("KategoriId,Baslik")] Kategoriler kategoriler)
        {
            if (ModelState.IsValid)
            {
                _context.Add(kategoriler);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(kategoriler);
        }

        // GET: Kategoriler/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var kategoriler = await _context.Kategorilers.FindAsync(id);
            if (kategoriler == null)
            {
                return NotFound();
            }
            return View(kategoriler);
        }

        // POST: Kategoriler/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("KategoriId,Baslik")] Kategoriler kategoriler)
        {
            if (id != kategoriler.KategoriId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(kategoriler);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!KategorilerExists(kategoriler.KategoriId))
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
            return View(kategoriler);
        }

        // GET: Kategoriler/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var kategoriler = await _context.Kategorilers
                .FirstOrDefaultAsync(m => m.KategoriId == id);
            if (kategoriler == null)
            {
                return NotFound();
            }

            return View(kategoriler);
        }

        // POST: Kategoriler/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var kategoriler = await _context.Kategorilers.FindAsync(id);
            if (kategoriler != null)
            {
                _context.Kategorilers.Remove(kategoriler);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool KategorilerExists(int id)
        {
            return _context.Kategorilers.Any(e => e.KategoriId == id);
        }
    }
}
