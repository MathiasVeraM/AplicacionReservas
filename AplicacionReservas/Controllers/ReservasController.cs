using AplicacionReservas.Data;
using AplicacionReservas.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AplicacionReservas.Controllers
{
    public class ReservasController : Controller
    {
        private readonly AppDbContext _context;
        public ReservasController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Crear()
        {
            ViewBag.Laboratorios = _context.Laboratorios.ToList();
            ViewBag.Modulos = _context.ModulosHorario.ToList();
            ViewBag.Reactivos = _context.Reactivos.ToList();
            ViewBag.Equipos = _context.Equipos.ToList();
            ViewBag.Docentes = _context.Docentes.ToList();
            ViewBag.Unidades = _context.Unidades.ToList();
            ViewBag.TiposReserva = Enum.GetValues(typeof(TipoReserva)).Cast<TipoReserva>();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(
            Reserva reserva,
            IFormFile EvidenciaCorreoRuta,
            List<MiembroEquipo> miembros,
            List<Insumo> insumos,
            List<int> equipoIds,
            List<int> reactivosSeleccionados,
            Dictionary<int, int> cantidades,
            Dictionary<int, string> unidades)
        {
            var reservasExistentes = _context.Reservas.Count(r =>
                r.Fecha == reserva.Fecha &&
                r.LaboratorioId == reserva.LaboratorioId &&
                r.ModuloHorarioId == reserva.ModuloHorarioId);

            if (reservasExistentes >= 3)
            {
                ModelState.AddModelError("", "Ya existen 3 reservas para ese laboratorio, módulo y fecha.");
                return View(reserva);
            }

            if (EvidenciaCorreoRuta != null && EvidenciaCorreoRuta.Length > 0)
            {
                var nombreArchivo = Path.GetFileName(EvidenciaCorreoRuta.FileName);
                var rutaCarpeta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "evidencia");
                Directory.CreateDirectory(rutaCarpeta);
                var rutaCompleta = Path.Combine(rutaCarpeta, nombreArchivo);

                using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                {
                    await EvidenciaCorreoRuta.CopyToAsync(stream);
                }

                reserva.EvidenciaCorreoRuta = "/evidencia/" + nombreArchivo;
            }
            else
            {
                ModelState.AddModelError("EvidenciaCorreoRuta", "Debe subir una imagen como evidencia.");
                return View(reserva);
            }

            reserva.MiembrosEquipo = miembros;
            reserva.Insumos = insumos;

            reserva.Equipos = _context.Equipos
                .Where(e => equipoIds.Contains(e.Id))
                .ToList();

            reserva.ReservaReactivos = new List<ReservaReactivo>();
            foreach (var reactivoId in reactivosSeleccionados)
            {
                if (cantidades.TryGetValue(reactivoId, out int cantidad) && cantidad > 0 &&
                    unidades.TryGetValue(reactivoId, out string unidad) && !string.IsNullOrWhiteSpace(unidad))
                {
                    reserva.ReservaReactivos.Add(new ReservaReactivo
                    {
                        ReactivoId = reactivoId,
                        Cantidad = cantidad,
                        Unidad = unidad
                    });
                }
            }

            var userIdClaim = User.FindFirst("UserId")?.Value;

            if (!int.TryParse(userIdClaim, out int userId))
            {
                ModelState.AddModelError("", "No se pudo identificar al usuario.");
                return View(reserva);
            }

            reserva.UsuarioId = userId;

            if (!reserva.EsMantenimiento)
            {
                if (string.IsNullOrWhiteSpace(reserva.Materia) ||
                    string.IsNullOrWhiteSpace(reserva.NombreProyecto) ||
                    string.IsNullOrWhiteSpace(reserva.Actividad) ||
                    reserva.ModuloHorarioId == null ||
                    reserva.DocenteId == null ||
                    string.IsNullOrWhiteSpace(reserva.EvidenciaCorreoRuta))
                {
                    ModelState.AddModelError("", "Todos los campos del formulario son obligatorios.");
                    return View(reserva);
                }
            }

            var hayMantenimiento = await _context.Reservas.AnyAsync(r =>
                r.EsMantenimiento &&
                r.Fecha == reserva.Fecha &&
                r.LaboratorioId == reserva.LaboratorioId &&
                reserva.ModuloHorarioId != null &&
                r.HoraInicioMantenimiento < _context.ModulosHorario
                    .Where(m => m.Id == reserva.ModuloHorarioId)
                    .Select(m => m.HoraFin)
                    .FirstOrDefault() &&
                r.HoraFinMantenimiento > _context.ModulosHorario
                    .Where(m => m.Id == reserva.ModuloHorarioId)
                    .Select(m => m.HoraInicio)
                    .FirstOrDefault());

            if (hayMantenimiento)
            {
                ModelState.AddModelError("", "Ya hay un mantenimiento programado para ese horario.");
                return View(reserva);
            }

            _context.Reservas.Add(reserva);
            await _context.SaveChangesAsync();

            return RedirectToAction("Calendario");
        }

        // Ver las reservas realizadas
        public IActionResult Listado()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;

            if (!int.TryParse(userIdClaim, out int usuarioId))
            {
                return Unauthorized(); // o RedirectToAction("Login")
            }

            IQueryable<Reserva> query = _context.Reservas
                .Include(r => r.Laboratorio)
                .Include(r => r.ModuloHorario)
                .Include(r => r.Docente)
                .Include(r => r.MiembrosEquipo)
                .Include(r => r.Equipos)
                .Include(r => r.ReservaReactivos).ThenInclude(rr => rr.Reactivo);

            if (!User.IsInRole("Admin"))
            {
                // Usuario común: solo sus reservas
                query = query.Where(r => r.UsuarioId == usuarioId);
            }

            var reservas = query.OrderByDescending(r => r.Fecha).ToList();

            return View(reservas);
        }

        public IActionResult Detalle(int id)
        {
            var reserva = _context.Reservas
                .Include(r => r.Laboratorio)
                .Include(r => r.ModuloHorario)
                .Include(r => r.Docente)
                .Include(r => r.MiembrosEquipo)
                .Include(r => r.Equipos)
                .Include(r => r.ReservaReactivos).ThenInclude(rr => rr.Reactivo)
                .Include(r => r.Insumos)
                .FirstOrDefault(r => r.Id == id);

            if (reserva == null) return NotFound();
            return View(reserva);
        }

        [HttpPost]
        public async Task<IActionResult> Eliminar(int id)
        {
            var reserva = await _context.Reservas.FindAsync(id);
            if (reserva == null) return NotFound();

            _context.Reservas.Remove(reserva);
            await _context.SaveChangesAsync();
            return RedirectToAction("Listado");
        }

        [HttpPost]
        public IActionResult Aprobar(int id)
        {
            var reserva = _context.Reservas.Find(id);
            if (reserva != null)
            {
                reserva.Aprobado = true;
                _context.SaveChanges();
            }
            return RedirectToAction("Listado");
        }

        [HttpPost]
        public IActionResult Rechazar(int id)
        {
            var reserva = _context.Reservas.Find(id);
            if (reserva != null)
            {
                reserva.Aprobado = false;
                _context.SaveChanges();
            }
            return RedirectToAction("Listado");
        }
        public IActionResult Calendario()
        {
            return View();
        }

        [HttpGet]
        public JsonResult ObtenerReservas()
        {
            var reservas = _context.Reservas
                .Include(r => r.Laboratorio)
                .Include(r => r.ModuloHorario)
                .Include(r => r.Usuario) // Asegúrate de incluir esto si tu modelo lo permite
                .Select(r => new
                {
                    title = r.EsMantenimiento
                        ? $"Mantenimiento - {r.Laboratorio.Nombre}"
                        : $"Reserva - {r.Laboratorio.Nombre}",
                    start = r.EsMantenimiento
                        ? r.Fecha.Date.Add(r.HoraInicioMantenimiento.Value)
                        : r.Fecha.Date.Add(r.ModuloHorario.HoraInicio),
                    end = r.EsMantenimiento
                        ? r.Fecha.Date.Add(r.HoraFinMantenimiento.Value)
                        : r.Fecha.Date.Add(r.ModuloHorario.HoraFin),
                    laboratorio = r.Laboratorio.Nombre,
                    email = r.Usuario.Email, // ← Esto es clave
                    color = r.EsMantenimiento ? "#dc3545" : "#198754"
                }).ToList();

            return Json(reservas);
        }

        // Metodo para descargar PDF
        public IActionResult DescargarPDF()
        {
            return View();
        }
    }
}

