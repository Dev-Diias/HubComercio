using HubComercio.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HubComercio.Controllers
{
    [Authorize]
    public class EstoqueController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EstoqueController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? busca)
        {
            var tenantIdClaim = User.FindFirst("TenantId")?.Value;

            if (string.IsNullOrEmpty(tenantIdClaim))
            {
                return RedirectToAction("Login", "Account");
            }

            int tenantId = int.Parse(tenantIdClaim);

            var query = _context.Produtos
                .Where(p => p.TenantId == tenantId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(busca))
            {
                query = query.Where(p => p.Nome.Contains(busca));
            }

            var produtos = await query
                .OrderBy(p => p.Nome)
                .ToListAsync();

            ViewBag.Busca = busca;

            return View(produtos);
        }
    }
}