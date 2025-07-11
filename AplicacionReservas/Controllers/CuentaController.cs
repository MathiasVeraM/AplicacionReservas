using AplicacionReservas.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AplicacionReservas.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace AplicacionReservas.Controllers
{
    public class CuentaController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PasswordHasher<Usuario> _passwordHasher;

        public CuentaController(AppDbContext appDbContext)
        {
            _context = appDbContext;
            _passwordHasher = new PasswordHasher<Usuario>();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult LoginUsuario()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> LoginUsuario(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Correo y contraseña son requeridos.");
                return View();
            }

            var usuario = _context.Usuario.FirstOrDefault(u => u.Email == email);

            if (usuario == null) {
                ModelState.AddModelError("", "No se encontro al usuario");
                return View();
            }

            // Verifica la contraseña con el hasher
            var resultado = _passwordHasher.VerifyHashedPassword(usuario, usuario.Password, password);

            if (resultado != PasswordVerificationResult.Success)
            {
                ModelState.AddModelError("", "Contraseña incorrecta.");
                return View();
            }

            // Claims pa mantener la sesion
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Role, usuario.Rol),
                new Claim("UserId", usuario.Id.ToString()),
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true, // Recuerda la sesión aunque se cierre el navegador (opcional)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return RedirectToAction("Index", "Home"); // Redirige a la página principal
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("LoginUsuario", "Cuenta");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult RegistrarUsuario()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult RegistrarUsuario(string email, string password, string idbanner)
        {
            // Verifica que se escriban email y contraseña
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(idbanner))
            {
                ModelState.AddModelError("", "Todos los campos son requeridos.");
                return View();
            }

            // Verifica que no exista ya ese usuario
            if (_context.Usuario.Any(u => u.Email == email))
            {
                ModelState.AddModelError("", "Ya existe un usuario con ese correo.");
                return View();
            }

            var nuevoUsuario = new Usuario
            {
                Email = email,
                Rol = "Usuario",
                IDBanner = idbanner
            };

            // Hashea la contraseña
            nuevoUsuario.Password = _passwordHasher.HashPassword(nuevoUsuario, password);

            _context.Usuario.Add(nuevoUsuario);
            _context.SaveChanges();

            return RedirectToAction("LoginUsuario");
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult RegistrarAdmin()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult RegistrarAdmin(string email, string password)
        {
            // Verifica que se escriba email y contraseña
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Correo y contraseña son requeridos.");
                return View();
            }

            // Verifica que aun no exista ese usuario
            if (_context.Usuario.Any(u => u.Email == email))
            {
                ModelState.AddModelError("", "Ya existe un usuario admin con ese correo.");
                return View();
            }

            var nuevoAdmin = new Usuario
            {
                Email = email,
                Rol = "Admin",
                IDBanner = "ADMIN"
            };

            nuevoAdmin.Password = _passwordHasher.HashPassword(nuevoAdmin, password);

            _context.Usuario.Add(nuevoAdmin);
            _context.SaveChanges();

            return RedirectToAction("Index","Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccesoDenegado()
        {
            return View();
        }

    }
}
