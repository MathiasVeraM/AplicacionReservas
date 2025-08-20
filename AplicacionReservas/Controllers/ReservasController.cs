using AplicacionReservas.Data;
using AplicacionReservas.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DinkToPdf;
using DinkToPdf.Contracts;
using System.Text;
using AplicacionReservas.Interfaces;
using AplicacionReservas.ViewModels;
using Microsoft.Extensions.Logging;

namespace AplicacionReservas.Controllers
{
    public class ReservasController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConverter _converter;
        private readonly IEmailServices _emailService;
        private readonly ILogger<ReservasController> _logger;

        public ReservasController(AppDbContext context, IConverter converter, IEmailServices emailService, ILogger<ReservasController> logger)
        {
            _context = context;
            _converter = converter;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Crear()
        {
            ViewBag.Laboratorios = _context.Laboratorios.ToList();
            ViewBag.Modulos = _context.ModulosHorario.Select(m => new {m.Id, m.Nombre, m.DuracionHoras}).ToList();
            ViewBag.Reactivos = _context.Reactivos.ToList();
            ViewBag.Equipos = _context.Equipos.ToList();
            ViewBag.Docentes = _context.Docentes.ToList();
            ViewBag.Unidades = _context.Unidades.ToList();
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
            // Obtener módulo horario actual
            var moduloActual = await _context.ModulosHorario
                .Where(m => m.Id == reserva.ModuloHorarioId)
                .Select(m => new { m.HoraInicio, m.HoraFin, m.DuracionHoras })
                .FirstOrDefaultAsync();

            if (moduloActual == null)
            {
                ModelState.AddModelError("ModuloHorarioId", "El módulo horario seleccionado no existe.");
                CargarListasParaViewBag();
                return View(reserva);
            }

            // Validar si la duración elegida coincide con la del módulo
            if (reserva.DuracionHoras != moduloActual.DuracionHoras)
            {
                ModelState.AddModelError("DuracionHoras", "La duración seleccionada no coincide con la del módulo horario.");
                CargarListasParaViewBag();
                return View(reserva);
            }

            // Validar traslape de reservas
            var reservasTraslapadas = await _context.Reservas
                .Include(r => r.ModuloHorario)
                .Where(r =>
                    r.Fecha == reserva.Fecha &&
                    r.LaboratorioId == reserva.LaboratorioId &&
                    r.ModuloHorario != null &&
                    r.ModuloHorario.HoraInicio < moduloActual.HoraFin &&
                    r.ModuloHorario.HoraFin > moduloActual.HoraInicio
                )
                .CountAsync();

            if (reservasTraslapadas >= 3)
            {
                ModelState.AddModelError("", "Ya existen 3 reservas traslapadas para ese laboratorio y horario.");
                CargarListasParaViewBag();
                return View(reserva);
            }

            // Guardar evidencia
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
                CargarListasParaViewBag();
                return View(reserva);
            }

            // Asignar usuario
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                ModelState.AddModelError("", "No se pudo identificar al usuario.");
                CargarListasParaViewBag();
                return View(reserva);
            }

            reserva.UsuarioId = userId;
            // Forzar que la fecha siempre se guarde sin hora
            reserva.Fecha = reserva.Fecha.Date;

            // Validaciones generales
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
                    CargarListasParaViewBag();
                    return View(reserva);
                }
            }

            // Validar traslape con mantenimiento
            var hayMantenimiento = await _context.Reservas.AnyAsync(r =>
                r.EsMantenimiento &&
                r.Fecha == reserva.Fecha &&
                r.LaboratorioId == reserva.LaboratorioId &&
                r.HoraInicioMantenimiento < moduloActual.HoraFin &&
                r.HoraFinMantenimiento > moduloActual.HoraInicio);

            if (hayMantenimiento)
            {
                ModelState.AddModelError("", "Ya hay un mantenimiento programado para ese horario.");
                CargarListasParaViewBag();
                return View(reserva);
            }

            // Cargar relaciones
            reserva.MiembrosEquipo = miembros;
            reserva.Insumos = insumos;

            reserva.Equipos = _context.Equipos
                .Where(e => equipoIds.Contains(e.Id))
                .ToList();

            reserva.ReservaReactivos = new List<ReservaReactivo>();
            foreach (var reactivoId in reactivosSeleccionados)
            {
                if (cantidades.TryGetValue(reactivoId, out int cantidad) &&
                    unidades.TryGetValue(reactivoId, out string unidad) &&
                    !string.IsNullOrWhiteSpace(unidad) && cantidad > 0)
                {
                    reserva.ReservaReactivos.Add(new ReservaReactivo
                    {
                        ReactivoId = reactivoId,
                        Cantidad = cantidad,
                        Unidad = unidad
                    });
                }
            }

            _context.Reservas.Add(reserva);
            await _context.SaveChangesAsync();

            // Envio de correos a administradores

            string subject = "Nueva reserva creada";
            string body = $@"
            Hola, <br/><br/>
            Se ha creado una nueva reserva pendiente de aprobación:<br/>
            <strong>Fecha:</strong> {reserva.Fecha}<br/>
            <strong>Laboratorio:</strong> {reserva.Laboratorio.Nombre}<br/>
            <strong>Creado por:</strong> {reserva.Usuario.Email}<br/><br/>
            Por favor revisa el panel de administración.";

            var adminEmails = _context.Usuario
                          .Where(u => u.Rol == "Admin")
                          .Select(u => u.Email)
                          .ToList();

            foreach (var email in adminEmails)
            {
                try
                {
                    await _emailService.EnviarCorreoAsync(email, subject, body);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "No se pudo enviar el correo a {Email}", email);
                }
            }

            return RedirectToAction("Listado");
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult CrearEspecial()
        {
            ViewBag.Laboratorios = _context.Laboratorios.ToList();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CrearEspecial(ReservaViewModel modelo)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                ModelState.AddModelError("", "No se pudo identificar al usuario.");
                ViewBag.Laboratorios = _context.Laboratorios.ToList();
                return View(modelo);
            }

            var reserva = new Reserva
            {
                Fecha = modelo.Fecha,
                LaboratorioId = modelo.LaboratorioId,
                HoraInicioMantenimiento = modelo.HoraInicio,
                HoraFinMantenimiento = modelo.HoraFin,
                EsMantenimiento = modelo.EsMantenimiento,
                UsuarioId = userId
            };

            _context.Reservas.Add(reserva);
            await _context.SaveChangesAsync();

            return RedirectToAction("Listado");
        }

        // Metodo auxiliar
        private void CargarListasParaViewBag()
        {
            ViewBag.Laboratorios = _context.Laboratorios.ToList();
            ViewBag.Modulos = _context.ModulosHorario.Select(m => new { m.Id, m.Nombre, m.DuracionHoras }).ToList();
            ViewBag.Reactivos = _context.Reactivos.ToList();
            ViewBag.Equipos = _context.Equipos.ToList();
            ViewBag.Docentes = _context.Docentes.ToList();
            ViewBag.Unidades = _context.Unidades.ToList();
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
        public async Task<IActionResult> Aprobar(int id, string? observaciones)
        {
            var reserva = _context.Reservas
                                  .Include(r => r.Usuario)
                                  .FirstOrDefault(r => r.Id == id);

            if (reserva != null)
            {
                reserva.Aprobado = EstadoAprobacion.Aprobado;
                _context.SaveChanges();

                string subject = "Reserva Aprobada";
                string body = $"Hola, <br/>Tu reserva {reserva.Fecha} - {reserva.Laboratorio} ha sido aprobada.";

                if (!string.IsNullOrEmpty(reserva.Usuario?.Email))
                {
                    if (!string.IsNullOrEmpty(observaciones))
                    {
                        body = $"Hola, <br/>Tu reserva {reserva.Fecha} - {reserva.Laboratorio} ha sido aprobada.<br/><br/><strong>Observaciones del administrador:</strong><br/>{observaciones}";
                    }

                    try
                    {
                        await _emailService.EnviarCorreoAsync(reserva.Usuario.Email, subject, body);
                    }
                    catch (Exception ex)
                    {
                        // Aquí puedes loguear el error
                        // Por ejemplo, usando ILogger
                        _logger.LogError(ex, "Error enviando correo de aprobación a {Email}", reserva.Usuario.Email);

                        // Opcional: mostrar alerta en la vista, pero no detener la acción
                        TempData["MensajeErrorCorreo"] = "No se pudo enviar el correo, pero la reserva fue aprobada.";
                    }
                }
            }

            return RedirectToAction("Listado");
        }

        [HttpPost]
        public async Task<IActionResult> Rechazar(int id, string? observaciones)
        {
            var reserva = _context.Reservas
                                  .Include(r => r.Usuario)
                                  .FirstOrDefault(r => r.Id == id);

            if (reserva != null)
            {
                reserva.Aprobado = EstadoAprobacion.NoAprobado;
                _context.SaveChanges();

                string subject = "Reserva No Aprobada";
                string body = $"Hola, <br/>Tu reserva {reserva.Fecha} - {reserva.Laboratorio} ha sido rechazada.";

                if (!string.IsNullOrEmpty(reserva.Usuario?.Email))
                {
                    if (!string.IsNullOrEmpty(observaciones))
                    {
                        body = $"Hola, <br/>Tu reserva {reserva.Fecha} - {reserva.Laboratorio} ha sido rechazada por estos motivos:<br/><br/><strong>Observaciones del administrador:</strong><br/>{observaciones}";
                    }

                    try
                    {
                        await _emailService.EnviarCorreoAsync(reserva.Usuario.Email, subject, body);
                    }
                    catch (Exception ex)
                    {
                        // Aquí puedes loguear el error
                        // Por ejemplo, usando ILogger
                        _logger.LogError(ex, "Error enviando correo de rechazo a {Email}", reserva.Usuario.Email);

                        // Opcional: mostrar alerta en la vista, pero no detener la acción
                        TempData["MensajeErrorCorreo"] = "No se pudo enviar el correo, pero la reserva fue rechazada.";
                    }
                }
            }

            return RedirectToAction("Listado");
        }

        [HttpPost]
        public async Task<IActionResult> EnRevision(int id, string? observaciones) {

            var reserva = _context.Reservas
                                  .Include(r => r.Usuario)
                                  .FirstOrDefault(r => r.Id == id);

            if (reserva != null)
            {
                reserva.Aprobado = EstadoAprobacion.Revision;
                _context.SaveChanges();

                string subject = "Reserva Aprobada Parcialmente";
                string body = $"Hola, <br/>Tu reserva {reserva.Fecha} - {reserva.Laboratorio} necesita modificaciones en algunos campos.";

                if (!string.IsNullOrEmpty(reserva.Usuario?.Email))
                {
                    if (!string.IsNullOrEmpty(observaciones))
                    {
                        body = $"Hola, <br/>Tu reserva {reserva.Fecha} - {reserva.Laboratorio} necesita modificaciones en algunos campos: <br/><br/><strong>Observaciones del administrador:</strong><br/>{observaciones}";
                    }

                    try
                    {
                        await _emailService.EnviarCorreoAsync(reserva.Usuario.Email, subject, body);
                    }
                    catch (Exception ex)
                    {
                        // Aquí puedes loguear el error
                        // Por ejemplo, usando ILogger
                        _logger.LogError(ex, "Error enviando correo de pendiente revisión a {Email}", reserva.Usuario.Email);

                        // Opcional: mostrar alerta en la vista, pero no detener la acción
                        TempData["MensajeErrorCorreo"] = "No se pudo enviar el correo, pero la reserva fue asignada como pendiente revisión.";
                    }
                }
            }

            return RedirectToAction("Listado");
        }

        [HttpGet]
        public IActionResult Calendario()
        {
            var laboratorios = _context.Laboratorios.ToList(); // ← Carga los laboratorios desde la BD
            ViewBag.Laboratorios = laboratorios;

            return View();
        }

        [HttpGet]
        public JsonResult ObtenerReservas(int? laboratorioId)
        {
            var reservas = _context.Reservas
            .Include(r => r.Laboratorio)
            .Include(r => r.ModuloHorario)
            .Include(r => r.Usuario)
            .Where(r => (!laboratorioId.HasValue || r.LaboratorioId == laboratorioId)) // Para solo mostrar las aprobadas = && (r.Aprobado == EstadoAprobacion.Aprobado)
            .Select(r => new
            {
                title = r.EsMantenimiento
                    ? $"Mantenimiento - {r.Laboratorio.Nombre}"
                    : $"Reserva - {r.Laboratorio.Nombre}",

                start = r.EsMantenimiento
                    ? r.Fecha.Date.Add(r.HoraInicioMantenimiento.Value).ToString("s")
                    : r.Fecha.Date.Add(r.ModuloHorario.HoraInicio).ToString("s"),

                end = r.EsMantenimiento
                    ? r.Fecha.Date.Add(r.HoraFinMantenimiento.Value).ToString("s")
                    : r.Fecha.Date.Add(r.ModuloHorario.HoraFin).ToString("s"),

                laboratorio = r.Laboratorio.Nombre,
                email = r.Usuario.Email,
                color = r.EsMantenimiento ? "#dc3545" : "#198754"
            })
            .ToList();

            return Json(reservas);
        }

        [HttpGet]
        public IActionResult Editar(int id)
        {
            var reserva = _context.Reservas
                .Include(r => r.MiembrosEquipo)
                .Include(r => r.ReservaReactivos)
                .Include(r => r.Equipos)
                .Include(r => r.Insumos)
                .FirstOrDefault(r => r.Id == id);

            if (reserva == null) return NotFound();

            var userIdClaim = User.FindFirst("UserId")?.Value;
            bool esAdmin = User.IsInRole("Admin");

            if (!esAdmin && (!int.TryParse(userIdClaim, out int userId) || reserva.UsuarioId != userId))
            {
                return Forbid();
            }

            // Solo permitir editar si está en revisión
            if (!esAdmin && reserva.Aprobado != EstadoAprobacion.Revision)
            {
                return Forbid();
            }

            ViewBag.Laboratorios = _context.Laboratorios.ToList();
            ViewBag.Modulos = _context.ModulosHorario.Select(m => new { m.Id, m.Nombre, m.DuracionHoras }).ToList();
            ViewBag.Reactivos = _context.Reactivos.ToList();
            ViewBag.Equipos = _context.Equipos.ToList();
            ViewBag.Docentes = _context.Docentes.ToList();
            ViewBag.Unidades = _context.Unidades.ToList();
            return View(reserva);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(
            int id,
            Reserva reservaActualizada,
            IFormFile EvidenciaCorreoRuta,
            List<MiembroEquipo> miembros,
            List<Insumo> insumos,
            List<int> equipoIds,
            List<int> reactivosSeleccionados,
            Dictionary<int, int> cantidades,
            Dictionary<int, string> unidades)
        {
            var reserva = _context.Reservas
                .Include(r => r.MiembrosEquipo)
                .Include(r => r.ReservaReactivos)
                .Include(r => r.Equipos)
                .Include(r => r.Insumos)
                .FirstOrDefault(r => r.Id == id);

            if (reserva == null) return NotFound();

            var userIdClaim = User.FindFirst("UserId")?.Value;
            bool esAdmin = User.IsInRole("Admin");

            if (!esAdmin && (!int.TryParse(userIdClaim, out int userId) || reserva.UsuarioId != userId))
            {
                return Forbid();
            }

            if (!esAdmin && reserva.Aprobado != EstadoAprobacion.Revision)
            {
                return Forbid();
            }

            // Validar traslape de horarios antes de guardar
            var moduloActual = await _context.ModulosHorario
                .Where(m => m.Id == reservaActualizada.ModuloHorarioId)
                .Select(m => new { m.HoraInicio, m.HoraFin })
                .FirstOrDefaultAsync();

            if (moduloActual == null)
            {
                ModelState.AddModelError("ModuloHorarioId", "El módulo horario seleccionado no existe.");
                CargarListasParaViewBag();
                return View(reservaActualizada);
            }

            var reservasTraslapadas = await _context.Reservas
                .Include(r => r.ModuloHorario)
                .Where(r =>
                    r.Id != id &&
                    r.Fecha == reservaActualizada.Fecha &&
                    r.LaboratorioId == reservaActualizada.LaboratorioId &&
                    r.ModuloHorario != null &&
                    r.ModuloHorario.HoraInicio < moduloActual.HoraFin &&
                    r.ModuloHorario.HoraFin > moduloActual.HoraInicio
                )
                .CountAsync();

            if (reservasTraslapadas >= 3)
            {
                ModelState.AddModelError("", "Ya existen 3 reservas traslapadas para ese laboratorio y horario.");
                CargarListasParaViewBag();
                return View(reservaActualizada);
            }

            // Procesar evidencia (si se actualiza)
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

            // Actualizar campos base
            reserva.Fecha = reservaActualizada.Fecha;
            reserva.Materia = reservaActualizada.Materia;
            reserva.NombreProyecto = reservaActualizada.NombreProyecto;
            reserva.Actividad = reservaActualizada.Actividad;
            reserva.ConsideracionesEspeciales = reservaActualizada.ConsideracionesEspeciales;
            reserva.ModuloHorarioId = reservaActualizada.ModuloHorarioId;
            reserva.LaboratorioId = reservaActualizada.LaboratorioId;
            reserva.DocenteId = reservaActualizada.DocenteId;
            reserva.DuracionHoras = reservaActualizada.DuracionHoras;

            // Actualizar miembros
            reserva.MiembrosEquipo = miembros;

            // Actualizar insumos
            reserva.Insumos = insumos;

            // Actualizar equipos
            reserva.Equipos = _context.Equipos
                .Where(e => equipoIds.Contains(e.Id))
                .ToList();

            // Actualizar reactivos
            reserva.ReservaReactivos = new List<ReservaReactivo>();
            foreach (var reactivoId in reactivosSeleccionados)
            {
                if (cantidades.TryGetValue(reactivoId, out int cantidad) &&
                    unidades.TryGetValue(reactivoId, out string unidad))
                {
                    reserva.ReservaReactivos.Add(new ReservaReactivo
                    {
                        ReactivoId = reactivoId,
                        Cantidad = cantidad,
                        Unidad = unidad
                    });
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Listado");
        }


        // Metodo para descargar PDF
        public IActionResult DescargarPDF(int id)
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

            if (reserva == null)
            {
                return NotFound();
            }

            // Construir HTML manualmente
            var html = new StringBuilder();

            html.Append(@"
            <html><head>
                <style>
                    body { font-family: Arial; font-size: 14px; }
                    h1, h2 { text-align: center; }
                    table { width: 100%; border-collapse: collapse; margin-top: 20px; }
                    th, td { border: 1px solid #ccc; padding: 6px; text-align: left; }
                    ul { margin: 0; padding-left: 20px; }
                </style>
            </head><body>");

            html.Append("<h1>Detalle de la Reserva</h1>");

            // Información general
            html.Append("<h2>Informacion General</h2>");
            html.Append("<table>");
            html.Append($"<tr><th>Fecha</th><td>{reserva.Fecha:yyyy-MM-dd}</td></tr>");
            html.Append($"<tr><th>Laboratorio</th><td>{reserva.Laboratorio?.Nombre}</td></tr>");
            html.Append($"<tr><th>Modulo</th><td>{reserva.ModuloHorario?.Nombre}</td></tr>");
            html.Append($"<tr><th>Docente</th><td>{reserva.Docente?.Nombre}</td></tr>");
            html.Append($"<tr><th>Materia</th><td>{reserva.Materia}</td></tr>");
            html.Append($"<tr><th>Proyecto</th><td>{reserva.NombreProyecto}</td></tr>");
            html.Append($"<tr><th>Actividad</th><td>{reserva.Actividad}</td></tr>");
            html.Append($"<tr><th>Duracion</th><td>{reserva.DuracionHoras} horas</td></tr>");
            html.Append($"<tr><th>Consideraciones Especiales</th><td>{reserva.ConsideracionesEspeciales}</td></tr>");
            html.Append($"<tr><th>Estado</th><td>{reserva.Aprobado}</td></tr>");
            html.Append("</table>");

            // Miembros del equipo
            html.Append("<h2>Miembros del Equipo</h2><ul>");
            if (reserva.MiembrosEquipo != null && reserva.MiembrosEquipo.Any())
            {
                foreach (var m in reserva.MiembrosEquipo)
                    html.Append($"<li>{m.Nombre}</li>");
            }
            else
            {
                html.Append("<li>—</li>");
            }
            html.Append("</ul>");

            // Equipos
            html.Append("<h2>Equipos</h2><table><tr><th>Nombre</th><th>Laboratorio</th></tr>");
            if (reserva.Equipos != null && reserva.Equipos.Any())
            {
                foreach (var e in reserva.Equipos)
                    html.Append($"<tr><td>{e.Nombre}</td><td>{e.NombreLaboratorio}</td></tr>");
            }
            else
            {
                html.Append("<tr><td colspan='2'>—</td></tr>");
            }
            html.Append("</table>");

            // Insumos
            html.Append("<h2>Insumos</h2><table><tr><th>Nombre</th><th>Cantidad</th></tr>");
            if (reserva.Insumos != null && reserva.Insumos.Any())
            {
                foreach (var i in reserva.Insumos)
                    html.Append($"<tr><td>{i.Nombre}</td><td>{i.Cantidad}</td></tr>");
            }
            else
            {
                html.Append("<tr><td colspan='2'>—</td></tr>");
            }
            html.Append("</table>");

            // Reactivos
            html.Append("<h2>Reactivos</h2><table><tr><th>Nombre</th><th>Cantidad</th><th>Unidad</th></tr>");
            if (reserva.ReservaReactivos != null && reserva.ReservaReactivos.Any())
            {
                foreach (var rr in reserva.ReservaReactivos)
                    html.Append($"<tr><td>{rr.Reactivo?.Nombre}</td><td>{rr.Cantidad}</td><td>{rr.Unidad}</td></tr>");
            }
            else
            {
                html.Append("<tr><td colspan='3'>—</td></tr>");
            }
            html.Append("</table>");

            // Obtener ruta absoluta al archivo
            var imagenPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", reserva.EvidenciaCorreoRuta.TrimStart('/'));

            if (System.IO.File.Exists(imagenPath))
            {
                html.Append("<h2>Evidencia del Correo</h2>");
                html.Append($"<img src='file:///{imagenPath.Replace("\\", "/")}' style='max-width:100%; max-height:500px;' />");
            }
            else
            {
                html.Append("<h2>Evidencia del Correo</h2><p>No disponible.</p>");
            }

            html.Append("</body></html>");

            // Convertir a PDF
            var doc = new HtmlToPdfDocument()
            {
                GlobalSettings = {
                ColorMode = ColorMode.Color,
                Orientation = Orientation.Portrait,
                PaperSize = PaperKind.A4
            },
                Objects = {
                    new ObjectSettings() {
                        HtmlContent = html.ToString()
                    }
                }
            };

            var pdf = _converter.Convert(doc);

            return File(pdf, "application/pdf", $"Reserva {reserva.Laboratorio.Nombre} {reserva.ModuloHorario.Nombre}.pdf");
        }
    }
}

