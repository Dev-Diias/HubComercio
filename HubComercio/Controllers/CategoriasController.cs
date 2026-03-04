using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HubComercio.Data;
using HubComercio.Models;
using Microsoft.AspNetCore.Authorization;
using System.IO;

namespace HubComercio.Controllers
{
    [Authorize] // Garante que apenas usuários logados acessem a gestão
    public class CategoriasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Método auxiliar para pegar o ID do mercado do usuário logado
        private int GetTenantId()
        {
            var tenantIdClaim = User.FindFirst("TenantId")?.Value;
            return int.Parse(tenantIdClaim ?? "0");
        }

        // GET: Categorias
        // FILTRADO: Mostra apenas as categorias do mercado logado
        public async Task<IActionResult> Index()
        {
            int tenantId = GetTenantId();
            var categorias = _context.Categorias
                .Where(c => c.TenantId == tenantId);

            return View(await categorias.ToListAsync());
        }

        // GET: Categorias/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            int tenantId = GetTenantId();
            var categoria = await _context.Categorias
                .FirstOrDefaultAsync(m => m.IdCategoria == id && m.TenantId == tenantId);

            if (categoria == null) return NotFound();

            return View(categoria);
        }

        // GET: Categorias/Create
        public IActionResult Create()
        {
            return View(new Categoria());
        }

        // POST: Categorias/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] Categoria categoria)
        {
            categoria.TenantId = GetTenantId();

            var foto = Request.Form.Files.FirstOrDefault(); // pega o 1º arquivo enviado

            if (foto == null || foto.Length == 0)
            {
                ModelState.AddModelError("", "Nenhum arquivo foi recebido.");
                return View(categoria);
            }

            var pasta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "imagens");
            Directory.CreateDirectory(pasta);

            var ext = Path.GetExtension(foto.FileName);
            var nomeArquivo = $"{Guid.NewGuid()}{ext}";
            var caminho = Path.Combine(pasta, nomeArquivo);

            using (var stream = new FileStream(caminho, FileMode.Create))
                await foto.CopyToAsync(stream);

            categoria.ImagemUrl = "/imagens/" + nomeArquivo;

            _context.Categorias.Add(categoria);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Categorias/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            int tenantId = GetTenantId();
            var categoria = await _context.Categorias
                .FirstOrDefaultAsync(c => c.IdCategoria == id && c.TenantId == tenantId);

            if (categoria == null) return NotFound();

            return View(categoria);
        }

        // POST: Categorias/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [FromForm] Categoria categoria, IFormFile? foto)
        {
            if (id != categoria.IdCategoria) return NotFound();

            // garante tenant do usuário
            categoria.TenantId = GetTenantId();
            ModelState.Remove("Tenant");
            ModelState.Remove("TenantId");

            // Pega do banco para manter dados (principalmente ImagemUrl) e para não perder TenantId
            var categoriaDb = await _context.Categorias
                .FirstOrDefaultAsync(c => c.IdCategoria == id && c.TenantId == categoria.TenantId);

            if (categoriaDb == null) return NotFound();

            // atualiza campos editáveis
            categoriaDb.Nome = categoria.Nome;

            // se veio nova foto, salva e atualiza ImagemUrl
            if (foto != null && foto.Length > 0)
            {
                var pasta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "imagens");
                Directory.CreateDirectory(pasta);

                var ext = Path.GetExtension(foto.FileName);
                var nomeArquivo = $"{Guid.NewGuid()}{ext}";
                var caminho = Path.Combine(pasta, nomeArquivo);

                using (var stream = new FileStream(caminho, FileMode.Create))
                    await foto.CopyToAsync(stream);

                // (opcional) apagar imagem antiga do disco
                if (!string.IsNullOrEmpty(categoriaDb.ImagemUrl))
                {
                    var antiga = categoriaDb.ImagemUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
                    var caminhoAntigo = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", antiga);

                    if (System.IO.File.Exists(caminhoAntigo))
                        System.IO.File.Delete(caminhoAntigo);
                }

                categoriaDb.ImagemUrl = "/imagens/" + nomeArquivo;
            }

            // salva
            _context.Update(categoriaDb);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Categorias/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            int tenantId = GetTenantId();
            var categoria = await _context.Categorias
                .FirstOrDefaultAsync(m => m.IdCategoria == id && m.TenantId == tenantId);

            if (categoria == null) return NotFound();

            return View(categoria);
        }

        // POST: Categorias/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            int tenantId = GetTenantId();
            var categoria = await _context.Categorias
                .FirstOrDefaultAsync(c => c.IdCategoria == id && c.TenantId == tenantId);

            if (categoria != null)
            {
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