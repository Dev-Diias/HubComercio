using HubComercio.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HubComercio.Data;
using HubComercio.Models;
using Microsoft.AspNetCore.Authorization;

namespace HubComercio.Controllers
{
    [Authorize]
    public class CategoriasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriasController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetTenantId()
        {
            var tenantIdClaim = User.FindFirst("TenantId")?.Value;
            return int.Parse(tenantIdClaim ?? "0");
        }

        public async Task<IActionResult> Index(string busca)
        {
            var tenantIdClaim = User.FindFirst("TenantId")?.Value;

            if (string.IsNullOrEmpty(tenantIdClaim))
                return Unauthorized();

            int tenantId = int.Parse(tenantIdClaim);

            var query = _context.Categorias
                .Include(c => c.Tenant)
                .Where(c => c.TenantId == tenantId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(busca))
            {
                busca = busca.Trim();
                query = query.Where(c => c.Nome.StartsWith(busca));
            }

            var categorias = await query.ToListAsync();
            return View(categorias);
        }

        

        public IActionResult Create()
        {
            return View(new Categoria());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] Categoria categoria, IFormFile? foto)
        {
            categoria.TenantId = GetTenantId();

            ModelState.Remove("Tenant");
            ModelState.Remove("TenantId");

            if (!ModelState.IsValid)
                return View(categoria);

            if (foto != null && foto.Length > 0)
            {
                categoria.ImagemUrl = await UploadHelper.SalvarImagem(foto, "categorias", categoria.TenantId);
            }
            else
            {
                categoria.ImagemUrl = null;
            }

            _context.Categorias.Add(categoria);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            int tenantId = GetTenantId();

            var categoria = await _context.Categorias
                .FirstOrDefaultAsync(c => c.IdCategoria == id && c.TenantId == tenantId);

            if (categoria == null) return NotFound();

            return View(categoria);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [FromForm] Categoria categoria, IFormFile? foto)
        {
            if (id != categoria.IdCategoria) return NotFound();

            categoria.TenantId = GetTenantId();

            ModelState.Remove("Tenant");
            ModelState.Remove("TenantId");

            var categoriaDb = await _context.Categorias
                .FirstOrDefaultAsync(c => c.IdCategoria == id && c.TenantId == categoria.TenantId);

            if (categoriaDb == null) return NotFound();

            if (!ModelState.IsValid)
                return View(categoria);

            categoriaDb.Nome = categoria.Nome;

            if (foto != null && foto.Length > 0)
            {
                UploadHelper.ExcluirImagem(categoriaDb.ImagemUrl);
                categoriaDb.ImagemUrl = await UploadHelper.SalvarImagem(foto, "categorias", categoriaDb.TenantId);
            }

            _context.Update(categoriaDb);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            int tenantId = GetTenantId();

            var categoria = await _context.Categorias
                .FirstOrDefaultAsync(m => m.IdCategoria == id && m.TenantId == tenantId);

            if (categoria == null) return NotFound();

            return View(categoria);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            int tenantId = GetTenantId();

            var categoria = await _context.Categorias
                .FirstOrDefaultAsync(c => c.IdCategoria == id && c.TenantId == tenantId);

            if (categoria != null)
            {
                UploadHelper.ExcluirImagem(categoria.ImagemUrl);
                _context.Categorias.Remove(categoria);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CategoriaExists(int id)
        {
            return _context.Categorias.Any(e => e.IdCategoria == id && e.TenantId == GetTenantId());
        }
    }
}