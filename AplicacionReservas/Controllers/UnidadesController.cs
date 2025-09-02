using AplicacionReservas.Data;
using AplicacionReservas.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AplicacionReservas.Controllers
{
    public class UnidadesController : Controller
    {
        private readonly AppDbContext _context;

        public UnidadesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Unidades
        public async Task<IActionResult> Index()
        {
            var unidades = await _context.Unidades.ToListAsync();
            return View(unidades);
        }

        // GET: Unidades/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var unidad = await _context.Unidades.FirstOrDefaultAsync(u => u.Id == id);
            if (unidad == null) return NotFound();

            return View(unidad);
        }

        // GET: Unidades/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Unidades/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Unidad unidad)
        {
            if (ModelState.IsValid)
            {
                _context.Add(unidad);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(unidad);
        }

        // GET: Unidades/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var unidad = await _context.Unidades.FindAsync(id);
            if (unidad == null) return NotFound();

            return View(unidad);
        }

        // POST: Unidades/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Unidad unidad)
        {
            if (id != unidad.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(unidad);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Unidades.Any(u => u.Id == unidad.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(unidad);
        }

        // GET: Unidades/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var unidad = await _context.Unidades.FirstOrDefaultAsync(u => u.Id == id);
            if (unidad == null) return NotFound();

            return View(unidad);
        }

        // POST: Unidades/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var unidad = await _context.Unidades.FindAsync(id);
            if (unidad != null)
            {
                _context.Unidades.Remove(unidad);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
