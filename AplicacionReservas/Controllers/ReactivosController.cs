using AplicacionReservas.Data;
using AplicacionReservas.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AplicacionReservas.Controllers
{
    public class ReactivosController : Controller
    {
        private readonly AppDbContext _context;

        public ReactivosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Reactivos
        public async Task<IActionResult> Index()
        {
            var reactivos = await _context.Reactivos.ToListAsync();
            return View(reactivos);
        }

        // GET: Reactivos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var reactivo = await _context.Reactivos.FirstOrDefaultAsync(r => r.Id == id);
            if (reactivo == null) return NotFound();

            return View(reactivo);
        }

        // GET: Reactivos/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Reactivos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Reactivo reactivo)
        {
            if (ModelState.IsValid)
            {
                _context.Add(reactivo);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(reactivo);
        }

        // GET: Reactivos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var reactivo = await _context.Reactivos.FindAsync(id);
            if (reactivo == null) return NotFound();

            return View(reactivo);
        }

        // POST: Reactivos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Reactivo reactivo)
        {
            if (id != reactivo.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(reactivo);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReactivoExists(reactivo.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(reactivo);
        }

        // POST: Reactivos/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var reactivo = await _context.Reactivos.FindAsync(id);
            if (reactivo != null)
            {
                _context.Reactivos.Remove(reactivo);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ReactivoExists(int id)
        {
            return _context.Reactivos.Any(r => r.Id == id);
        }
    }
}
