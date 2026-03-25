using HubComercio.Data;
using HubComercio.Helpers;
using HubComercio.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace HubComercio.Controllers
{
    [Authorize]
    public class ProdutosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProdutosController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetTenantId()
        {
            var tenantClaim = User.FindFirst("TenantId")?.Value;
            return int.Parse(tenantClaim ?? "0");
        }

        private bool TryParsePrecoFromForm(out decimal preco)
        {
            preco = 0m;
            var precoForm = Request.Form["Preco"].ToString();

            if (string.IsNullOrWhiteSpace(precoForm))
                return true;

            precoForm = precoForm.Trim().Replace(",", ".");

            return decimal.TryParse(
                precoForm,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out preco
            );
        }

        private void CarregarCategoriasDoTenant(int tenantId, int? categoriaSelecionada = null)
        {
            ViewBag.Categorias = _context.Categorias
                .Where(c => c.TenantId == tenantId)
                .Select(c => new { c.IdCategoria, c.Nome })
                .ToList();

            ViewBag.CategoriaSelecionada = categoriaSelecionada;
        }

        public async Task<IActionResult> Index(string busca, int? categoriaId)
        {
            var tenantIdClaim = User.FindFirst("TenantId")?.Value;

            if (string.IsNullOrEmpty(tenantIdClaim))
                return Unauthorized();

            int tenantId = int.Parse(tenantIdClaim);

            var query = _context.Produtos
                .Include(p => p.Categoria)
                .Include(p => p.Tenant)
                .Where(p => p.TenantId == tenantId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(busca))
            {
                busca = busca.Trim();
                query = query.Where(p => p.Nome.Contains(busca));
            }

            if (categoriaId.HasValue)
            {
                query = query.Where(p => p.CategoriaId == categoriaId.Value);
            }

            var produtos = await query.ToListAsync();

            var categorias = await _context.Categorias
                .Where(c => c.TenantId == tenantId)
                .OrderBy(c => c.Nome)
                .ToListAsync();

            ViewBag.Categorias = categorias;
            ViewBag.CategoriaSelecionada = categoriaId;

            return View(produtos);
        }

        public IActionResult Create()
        {
            int tenantId = GetTenantId();
            CarregarCategoriasDoTenant(tenantId);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Produto produto, IFormFile? foto)
        {
            int tenantId = GetTenantId();
            produto.TenantId = tenantId;

            if (!TryParsePrecoFromForm(out var precoOk))
                ModelState.AddModelError("Preco", "Preço inválido. Use 12.90 ou 12,90.");
            else
                produto.Preco = precoOk;

            bool categoriaValida = await _context.Categorias
                .AnyAsync(c => c.IdCategoria == produto.CategoriaId && c.TenantId == tenantId);

            if (!categoriaValida)
                ModelState.AddModelError("CategoriaId", "Categoria inválida.");

            if (foto != null && foto.Length > 0)
            {
                produto.ImagemUrl = await UploadHelper.SalvarImagem(foto, "produtos", tenantId);
            }

            ModelState.Remove("Categoria");
            ModelState.Remove("Tenant");
            ModelState.Remove("TenantId");

            if (!ModelState.IsValid)
            {
                CarregarCategoriasDoTenant(tenantId, produto.CategoriaId);
                return View(produto);
            }

            _context.Produtos.Add(produto);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            int tenantId = GetTenantId();

            var produto = await _context.Produtos
                .FirstOrDefaultAsync(p => p.IdProduto == id && p.TenantId == tenantId);

            if (produto == null) return NotFound();

            CarregarCategoriasDoTenant(tenantId, produto.CategoriaId);
            return View(produto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Produto produto, IFormFile? foto)
        {
            if (id != produto.IdProduto) return NotFound();

            int tenantId = GetTenantId();

            var produtoDb = await _context.Produtos
                .FirstOrDefaultAsync(p => p.IdProduto == id && p.TenantId == tenantId);

            if (produtoDb == null) return NotFound();

            if (!TryParsePrecoFromForm(out var precoOk))
                ModelState.AddModelError("Preco", "Preço inválido. Use 12.90 ou 12,90.");

            bool categoriaValida = await _context.Categorias
                .AnyAsync(c => c.IdCategoria == produto.CategoriaId && c.TenantId == tenantId);

            if (!categoriaValida)
                ModelState.AddModelError("CategoriaId", "Categoria inválida.");

            ModelState.Remove("Categoria");
            ModelState.Remove("Tenant");
            ModelState.Remove("TenantId");

            if (!ModelState.IsValid)
            {
                CarregarCategoriasDoTenant(tenantId, produto.CategoriaId);
                return View(produto);
            }

            produtoDb.Nome = produto.Nome;
            produtoDb.Preco = precoOk;
            produtoDb.UnidadeMedida = produto.UnidadeMedida;
            produtoDb.Qtde = produto.Qtde;
            produtoDb.CategoriaId = produto.CategoriaId;

            if (foto != null && foto.Length > 0)
            {
                UploadHelper.ExcluirImagem(produtoDb.ImagemUrl);
                produtoDb.ImagemUrl = await UploadHelper.SalvarImagem(foto, "produtos", tenantId);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            int tenantId = GetTenantId();

            var produto = await _context.Produtos
                .Include(p => p.Categoria)
                .Include(p => p.Tenant)
                .FirstOrDefaultAsync(p => p.IdProduto == id && p.TenantId == tenantId);

            if (produto == null) return NotFound();

            return View(produto);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            int tenantId = GetTenantId();

            var produto = await _context.Produtos
                .FirstOrDefaultAsync(p => p.IdProduto == id && p.TenantId == tenantId);

            if (produto != null)
            {
                UploadHelper.ExcluirImagem(produto.ImagemUrl);
                _context.Produtos.Remove(produto);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}