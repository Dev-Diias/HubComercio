using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; 
using HubComercio.Data;
using HubComercio.Models;

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
        // 1. Busca os dados do mercado para personalizar a tela (cores/nome)
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == id);

        if (tenant == null) return NotFound();

        // 2. Busca os produtos desse mercado específico que estão ativos
        var produtos = await _context.Produtos
            .Include(p => p.Categoria)
            .Where(p => p.TenantId == id)
            .ToListAsync();

        // 3. Passamos o Tenant no ViewData para usar as cores no Layout do catálogo
        ViewData["Tenant"] = tenant;

        return View(produtos);
    }
}