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
                // Busca o usuário e traz os dados do Tenant (Mercado) 
                var usuario = _context.Usuarios
                    .Include(u => u.Tenant)
                    .FirstOrDefault(u => u.Email == model.Email && u.Senha == model.Senha);

                if (usuario != null)
                {
                    // Criamos os Claims (identidade do usuário no sistema) [cite: 10, 39]
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, usuario.Nome),
                        new Claim(ClaimTypes.Email, usuario.Email),
                        new Claim("TenantId", usuario.TenantId.ToString()), // Essencial para isolamento SaaS [cite: 37]
                        new Claim("Cargo", usuario.Cargo) // Diferencia Dono de Admin [cite: 14, 38]
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    // Cria o Cookie de autenticação no navegador
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity));

                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError("", "E-mail ou senha inválidos.");
            }
            return View(model);
        }

        // 3. Método para Sair do Sistema
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}