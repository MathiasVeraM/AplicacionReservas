using AplicacionReservas.Data;
using DinkToPdf.Contracts;
using DinkToPdf;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using AplicacionReservas.Extensions;
using AplicacionReservas.Services;
using AplicacionReservas.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Conexion default 
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Autenticacion por cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Cuenta/LoginUsuario";   // Página para iniciar sesión
        options.LogoutPath = "/Cuenta/Logout"; // Página para cerrar sesión
        options.AccessDeniedPath = "/Cuenta/AccesoDenegado";
    });

builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});

var context = new CustomAssemblyLoadContext();
var path = Path.Combine(Directory.GetCurrentDirectory(), "LibreriaPDF", "libwkhtmltox.dll");
context.LoadUnmanagedLibrary(path);

builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));

builder.Services.AddSingleton<IEmailServices, EmailService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Ruta raíz que redirige a /Reservas/Calendario
app.MapGet("/", context =>
{
    context.Response.Redirect("/Reservas/Calendario");
    return Task.CompletedTask;
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Reservas}/{action=Calendario}/{id?}");

app.Run();
