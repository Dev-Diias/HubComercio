using HubComercio.Models;
using HubComercio.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HubComercio.Models.ViewModels;
namespace HubComercio.Controllers
{

    public class CatalogoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CatalogoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // O parâmetro 'id' aqui é o ID do Mercado (Tenant)
        public async Task<IActionResult> Index(int id)
        {
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Id == id && t.Ativo);

            if (tenant == null)
                return NotFound();

            var produtos = await _context.Produtos
                .Include(p => p.Categoria)
                .Where(p => p.TenantId == tenant.Id)
                .ToListAsync();

            var viewModel = new CatalogoViewModel
            {
                TenantId = tenant.Id,
                NomeEstabelecimento = tenant.NomeEstabelecimento,
                LogoUrl = tenant.LogoUrl,
                BannerUrl = tenant.BannerUrl,
                CorPrincipal = tenant.CorPrincipal,
                Produtos = produtos
            };

            return View(viewModel);
        }
    }
}