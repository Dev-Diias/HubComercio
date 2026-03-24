using HubComercio.Data;
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

        // =========================
        // Helpers
        // =========================
        private int GetTenantId()
        {
            var tenantClaim = User.FindFirst("TenantId")?.Value;
            return int.Parse(tenantClaim ?? "0");
        }

        // Aceita "12.90" e "12,90"
        private bool TryParsePrecoFromForm(out decimal preco)
        {
            preco = 0m;
            var precoForm = Request.Form["Preco"].ToString();

            if (string.IsNullOrWhiteSpace(precoForm))
                return true; // deixa validação de Required/Range cuidar, se existir

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

        // =========================
        // GET: Produtos
        // =========================
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



        // =========================
        // GET: Produtos/Create
        // =========================
        public IActionResult Create()
        {
            int tenantId = GetTenantId();
            CarregarCategoriasDoTenant(tenantId);
            return View();
        }

        // =========================
        // POST: Produtos/Create
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Produto produto, IFormFile? foto)
        {
            int tenantId = GetTenantId();
            produto.TenantId = tenantId;

            // Converte preço aceitando ponto ou vírgula
            if (!TryParsePrecoFromForm(out var precoOk))
                ModelState.AddModelError("Preco", "Preço inválido. Use 12.90 ou 12,90.");
            else
                produto.Preco = precoOk;

            // Valida categoria dentro do tenant
            bool categoriaValida = await _context.Categorias
                .AnyAsync(c => c.IdCategoria == produto.CategoriaId && c.TenantId == tenantId);

            if (!categoriaValida)
                ModelState.AddModelError("CategoriaId", "Categoria inválida.");

            // Upload da imagem
            if (foto != null && foto.Length > 0)
            {
                var pasta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "imagens");
                Directory.CreateDirectory(pasta);

                var ext = Path.GetExtension(foto.FileName);
                var nomeArquivo = $"{Guid.NewGuid()}{ext}";
                var caminho = Path.Combine(pasta, nomeArquivo);

                using (var stream = new FileStream(caminho, FileMode.Create))
                    await foto.CopyToAsync(stream);

                produto.ImagemUrl = "/imagens/" + nomeArquivo;
            }

            // Evita validação de navegação e tenant vindo do form
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

        // =========================
        // GET: Produtos/Edit/5
        // =========================
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

        // =========================
        // POST: Produtos/Edit/5
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Produto produto, IFormFile? foto)
        {
            if (id != produto.IdProduto) return NotFound();

            int tenantId = GetTenantId();

            // Busca o produto real do banco (para manter TenantId/Imagem atual com segurança)
            var produtoDb = await _context.Produtos
                .FirstOrDefaultAsync(p => p.IdProduto == id && p.TenantId == tenantId);

            if (produtoDb == null) return NotFound();

            // Converte preço aceitando ponto ou vírgula
            if (!TryParsePrecoFromForm(out var precoOk))
                ModelState.AddModelError("Preco", "Preço inválido. Use 12.90 ou 12,90.");

            // Valida categoria dentro do tenant
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

            // Atualiza campos permitidos
            produtoDb.Nome = produto.Nome;
            produtoDb.Preco = precoOk;
            produtoDb.UnidadeMedida = produto.UnidadeMedida;
            produtoDb.Qtde = produto.Qtde;
            produtoDb.CategoriaId = produto.CategoriaId;

            // Upload: se trocar imagem, salva nova e atualiza URL
            if (foto != null && foto.Length > 0)
            {
                var pasta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "imagens");
                Directory.CreateDirectory(pasta);

                var ext = Path.GetExtension(foto.FileName);
                var nomeArquivo = $"{Guid.NewGuid()}{ext}";
                var caminho = Path.Combine(pasta, nomeArquivo);

                using (var stream = new FileStream(caminho, FileMode.Create))
                    await foto.CopyToAsync(stream);

                // (Opcional) apagar imagem antiga do disco
                // if (!string.IsNullOrEmpty(produtoDb.ImagemUrl))
                // {
                //     var antiga = produtoDb.ImagemUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
                //     var caminhoAntigo = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", antiga);
                //     if (System.IO.File.Exists(caminhoAntigo)) System.IO.File.Delete(caminhoAntigo);
                // }

                produtoDb.ImagemUrl = "/imagens/" + nomeArquivo;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // GET: Produtos/Delete/5
        // =========================
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

        // =========================
        // POST: Produtos/Delete/5
        // =========================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            int tenantId = GetTenantId();

            var produto = await _context.Produtos
                .FirstOrDefaultAsync(p => p.IdProduto == id && p.TenantId == tenantId);

            if (produto != null)
            {
                // (Opcional) apagar imagem do disco ao deletar
                // if (!string.IsNullOrEmpty(produto.ImagemUrl))
                // {
                //     var rel = produto.ImagemUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
                //     var caminho = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", rel);
                //     if (System.IO.File.Exists(caminho)) System.IO.File.Delete(caminho);
                // }

                _context.Produtos.Remove(produto);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}