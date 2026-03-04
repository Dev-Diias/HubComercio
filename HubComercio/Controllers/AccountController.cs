using HubComercio.Data;
using HubComercio.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HubComercio.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Método para abrir a página de Login (GET)
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // 2. Método que processa o Login (POST)
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Busca assíncrona e sem rastreamento para evitar o erro 0xffffffff
                var usuario = await _context.Usuarios
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email == model.Email && u.Senha == model.Senha);

                if (usuario != null)
                {
                    var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, usuario.Nome),
                new Claim(ClaimTypes.Email, usuario.Email),
                // Converte o ID explicitamente para string para evitar erros de tipo
                new Claim("TenantId", usuario.TenantId.ToString()),
                new Claim("Cargo", usuario.Cargo ?? "dono")
            };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity));

                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError("", "E-mail ou senha inválidos.");
            }
            return View(model);
        }

        // 3. Método para Sair do Sistema
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Login", "Account");
        }
    }
}