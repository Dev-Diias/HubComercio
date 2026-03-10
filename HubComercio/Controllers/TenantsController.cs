using HubComercio.Data;
using HubComercio.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace HubComercio.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TenantsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TenantsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetTenantId()
        {
            var tenantClaim = User.FindFirst("TenantId")?.Value;
            return int.TryParse(tenantClaim, out var tenantId) ? tenantId : 0;
        }

        // GET: Tenants
        public async Task<IActionResult> Index()
        {
            var tenants = await _context.Tenants
                .AsNoTracking()
                .ToListAsync();

            return View(tenants);
        }

        // GET: Tenants/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Tenants/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Tenant tenant, IFormFile? logo, IFormFile? banner)
        {
            tenant.DataCadastro = DateTime.Now;

            if (logo != null && logo.Length > 0)
                tenant.LogoUrl = await SalvarImagemTenantAsync(logo);

            if (banner != null && banner.Length > 0)
                tenant.BannerUrl = await SalvarImagemTenantAsync(banner);

            ModelState.Remove("DataCadastro");

            if (!ModelState.IsValid)
                return View(tenant);

            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Tenants/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var tenant = await _context.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tenant == null) return NotFound();

            return View(tenant);
        }

        // POST: Tenants/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Tenant tenant, IFormFile? logo, IFormFile? banner)
        {
            if (id != tenant.Id) return NotFound();

            var tenantDb = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tenantDb == null) return NotFound();

            tenantDb.NomeEstabelecimento = tenant.NomeEstabelecimento;
            tenantDb.CNPJ = tenant.CNPJ;
            tenantDb.Ativo = tenant.Ativo;
            tenantDb.CorPrincipal = tenant.CorPrincipal;

            if (logo != null && logo.Length > 0)
                tenantDb.LogoUrl = await SalvarImagemTenantAsync(logo);

            if (banner != null && banner.Length > 0)
                tenantDb.BannerUrl = await SalvarImagemTenantAsync(banner);

            ModelState.Remove("DataCadastro");
            ModelState.Remove("LogoUrl");
            ModelState.Remove("BannerUrl");

            if (!ModelState.IsValid)
                return View(tenantDb);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        // GET: Tenants/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var tenant = await _context.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tenant == null) return NotFound();

            return View(tenant);
        }

        // POST: Tenants/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tenant == null) return NotFound();

            _context.Tenants.Remove(tenant);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private async Task<string> SalvarImagemTenantAsync(IFormFile arquivo)
        {
            var pasta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "imagens", "tenants");
            Directory.CreateDirectory(pasta);

            var ext = Path.GetExtension(arquivo.FileName);
            var nomeArquivo = $"{Guid.NewGuid()}{ext}";
            var caminho = Path.Combine(pasta, nomeArquivo);

            using (var stream = new FileStream(caminho, FileMode.Create))
                await arquivo.CopyToAsync(stream);

            return "/imagens/tenants/" + nomeArquivo;
        }
    }
}