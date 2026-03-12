using HubComercio.Data;
using HubComercio.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HubComercio.Controllers
{
    [Authorize]
    public class TenantsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TenantsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Tenants
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var tenants = await _context.Tenants
                .AsNoTracking()
                .ToListAsync();

            return View(tenants);
        }

        // GET: Tenants/Create
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Tenants/Create
        [Authorize(Roles = "Admin")]
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

        private bool PodeEditarTenant(int tenantIdDaRota)
        {
            if (User.IsInRole("Admin"))
                return true;

            var tenantIdClaim = User.FindFirst("TenantId")?.Value;

            if (string.IsNullOrEmpty(tenantIdClaim))
                return false;

            return tenantIdClaim == tenantIdDaRota.ToString();
        }

        [HttpGet]
        public IActionResult MinhaLoja()
        {
            var tenantIdClaim = User.FindFirst("TenantId")?.Value;

            if (string.IsNullOrEmpty(tenantIdClaim))
                return Forbid();

            if (!int.TryParse(tenantIdClaim, out var tenantId))
                return Forbid();

            return RedirectToAction(nameof(Edit), new { id = tenantId });
        }

        // GET: Tenants/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var tenant = await _context.Tenants.FindAsync(id);
            if (tenant == null)
                return NotFound();

            if (!PodeEditarTenant(tenant.Id))
                return Forbid();

            return View(tenant);
        }

        // POST: Tenants/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Tenant tenant, IFormFile? logo, IFormFile? banner)
        {
            if (id != tenant.Id)
                return NotFound();

            var tenantBanco = await _context.Tenants.FindAsync(id);
            if (tenantBanco == null)
                return NotFound();

            if (!PodeEditarTenant(tenantBanco.Id))
                return Forbid();

            ModelState.Remove("LogoUrl");
            ModelState.Remove("BannerUrl");
            ModelState.Remove("DataCadastro");

            if (!User.IsInRole("Admin"))
            {
                ModelState.Remove("NomeEstabelecimento");
                ModelState.Remove("CNPJ");
                ModelState.Remove("Ativo");
            }

            if (!ModelState.IsValid)
                return View(tenantBanco);

            if (User.IsInRole("Admin"))
            {
                tenantBanco.NomeEstabelecimento = tenant.NomeEstabelecimento;
                tenantBanco.CNPJ = tenant.CNPJ;
                tenantBanco.Ativo = tenant.Ativo;
            }

            tenantBanco.CorPrincipal = tenant.CorPrincipal;
            tenantBanco.WhatsApp = tenant.WhatsApp;

            if (logo != null && logo.Length > 0)
            {
                tenantBanco.LogoUrl = await SalvarImagemTenantAsync(logo);
            }

            if (banner != null && banner.Length > 0)
            {
                tenantBanco.BannerUrl = await SalvarImagemTenantAsync(banner);
            }

            _context.Update(tenantBanco);
            await _context.SaveChangesAsync();

            if (User.IsInRole("Admin"))
                return RedirectToAction(nameof(Index));

            return RedirectToAction("Index", "Home");
        }

        // GET: Tenants/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var tenant = await _context.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tenant == null)
                return NotFound();

            return View(tenant);
        }

        // POST: Tenants/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tenant == null)
                return NotFound();

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
            {
                await arquivo.CopyToAsync(stream);
            }

            return "/imagens/tenants/" + nomeArquivo;
        }
    }
}