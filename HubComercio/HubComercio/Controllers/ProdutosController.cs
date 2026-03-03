using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HubComercio.Data;
using HubComercio.Models;

namespace HubComercio.Controllers
{
    public class ProdutosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProdutosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Produtos
        public async Task<IActionResult> Index()
        {
            int tenantId = GetTenantId();

            // Filtra os produtos onde o TenantId coincide com o do usuário logado
            var produtosDoMeuMercado = _context.Produtos
                .Include(p => p.Categoria)
                .Include(p => p.Tenant)
                .Where(p => p.TenantId == tenantId);

            return View(await produtosDoMeuMercado.ToListAsync());
        }

        // GET: Produtos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var produto = await _context.Produtos
                .Include(p => p.Categoria)
                .Include(p => p.Tenant)
                .FirstOrDefaultAsync(m => m.IdProduto == id);
            if (produto == null)
            {
                return NotFound();
            }

            return View(produto);
        }

        // GET: Produtos/Create
        public IActionResult Create()
        {
            int tenantId = GetTenantId();
            // Filtra as categorias para mostrar apenas as do mercado logado
            ViewData["CategoriaId"] = new SelectList(_context.Categorias.Where(c => c.TenantId == tenantId), "IdCategoria", "Nome");

            // Removi a linha do ViewData["TenantId"] pois o sistema já sabe quem é o mercado
            return View();
        }

        // POST: Produtos/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdProduto,Nome,Preco,UnidadeMedida,Qtde,ImagemUrl,CategoriaId,TenantId")] Produto produto)
        {
            // Força o vínculo com o mercado do usuário logado
            produto.TenantId = GetTenantId();

            if (ModelState.IsValid)
            {
                _context.Add(produto);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(produto);
        }

        // GET: Produtos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var produto = await _context.Produtos.FindAsync(id);
            if (produto == null)
            {
                return NotFound();
            }
            ViewData["CategoriaId"] = new SelectList(_context.Categorias, "IdCategoria", "Nome", produto.CategoriaId);
            ViewData["TenantId"] = new SelectList(_context.Tenants, "Id", "NomeEstabelecimento", produto.TenantId);
            return View(produto);
        }

        // POST: Produtos/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdProduto,Nome,Preco,UnidadeMedida,Qtde,ImagemUrl,CategoriaId,TenantId")] Produto produto)
        {
            if (id != produto.IdProduto)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(produto);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProdutoExists(produto.IdProduto))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoriaId"] = new SelectList(_context.Categorias, "IdCategoria", "Nome", produto.CategoriaId);
            ViewData["TenantId"] = new SelectList(_context.Tenants, "Id", "NomeEstabelecimento", produto.TenantId);
            return View(produto);
        }

        // GET: Produtos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var produto = await _context.Produtos
                .Include(p => p.Categoria)
                .Include(p => p.Tenant)
                .FirstOrDefaultAsync(m => m.IdProduto == id);
            if (produto == null)
            {
                return NotFound();
            }

            return View(produto);
        }

        // POST: Produtos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var produto = await _context.Produtos.FindAsync(id);
            if (produto != null)
            {
                _context.Produtos.Remove(produto);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProdutoExists(int id)
        {
            return _context.Produtos.Any(e => e.IdProduto == id);
        }

        private int GetTenantId()
        {
            // Lê o "crachá" (Claim) que guardamos no momento do login
            var tenantClaim = User.FindFirst("TenantId")?.Value;
            return int.Parse(tenantClaim ?? "0");
        }
    }
}
