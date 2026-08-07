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
    public class YazarlarController : Controller
    {
        private readonly KutuphaneContext _context;

        public YazarlarController(KutuphaneContext context)
        {
            _context = context;
        }

        // GET: Yazarlar
        public async Task<IActionResult> Index()
        {
            return View(await _context.Yazarlars.ToListAsync());
        }

        // GET: Yazarlar/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var yazarlar = await _context.Yazarlars
                .FirstOrDefaultAsync(m => m.YazarId == id);
            if (yazarlar == null)
            {
                return NotFound();
            }

            return View(yazarlar);
        }

        // GET: Yazarlar/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Yazarlar/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("YazarId,AdSoyad")] Yazarlar yazarlar)
        {
            if (ModelState.IsValid)
            {
                _context.Add(yazarlar);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(yazarlar);
        }

        // GET: Yazarlar/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var yazarlar = await _context.Yazarlars.FindAsync(id);
            if (yazarlar == null)
            {
                return NotFound();
            }
            return View(yazarlar);
        }

        // POST: Yazarlar/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("YazarId,AdSoyad")] Yazarlar yazarlar)
        {
            if (id != yazarlar.YazarId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(yazarlar);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!YazarlarExists(yazarlar.YazarId))
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
            return View(yazarlar);
        }

        // GET: Yazarlar/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var yazarlar = await _context.Yazarlars
                .FirstOrDefaultAsync(m => m.YazarId == id);
            if (yazarlar == null)
            {
                return NotFound();
            }

            return View(yazarlar);
        }

        // POST: Yazarlar/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var yazarlar = await _context.Yazarlars.FindAsync(id);
            if (yazarlar != null)
            {
                _context.Yazarlars.Remove(yazarlar);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool YazarlarExists(int id)
        {
            return _context.Yazarlars.Any(e => e.YazarId == id);
        }
    }
}
