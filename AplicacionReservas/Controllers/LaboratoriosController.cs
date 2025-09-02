using AplicacionReservas.Data;
using AplicacionReservas.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AplicacionReservas.Controllers
{
    public class LaboratoriosController : Controller
    {
        private readonly AppDbContext _context;

        public LaboratoriosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Laboratorios
        public async Task<IActionResult> Index()
        {
            var laboratorios = await _context.Laboratorios.ToListAsync();
            return View(laboratorios);
        }

        // GET: Laboratorios/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var laboratorio = await _context.Laboratorios
                .Include(l => l.Reservas)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (laboratorio == null) return NotFound();
            return View(laboratorio);
        }

        // GET: Laboratorios/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Laboratorios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Laboratorio laboratorio)
        {
            if (ModelState.IsValid)
            {
                _context.Add(laboratorio);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(laboratorio);
        }

        // GET: Laboratorios/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var laboratorio = await _context.Laboratorios.FindAsync(id);
            if (laboratorio == null) return NotFound();
            return View(laboratorio);
        }

        // POST: Laboratorios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Laboratorio laboratorio)
        {
            if (id != laboratorio.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(laboratorio);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Laboratorios.Any(l => l.Id == laboratorio.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(laboratorio);
        }

        // GET: Laboratorios/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var laboratorio = await _context.Laboratorios.FirstOrDefaultAsync(l => l.Id == id);
            if (laboratorio == null) return NotFound();
            return View(laboratorio);
        }

        // POST: Laboratorios/Delete/5
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var laboratorio = await _context.Laboratorios
                .Include(l => l.Reservas)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (laboratorio == null)
                return NotFound();

            if (laboratorio.Reservas.Any())
            {
                TempData["Error"] = "No se puede eliminar el laboratorio porque tiene reservas asignadas.";
                return RedirectToAction(nameof(Index));
            }

            _context.Laboratorios.Remove(laboratorio);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
