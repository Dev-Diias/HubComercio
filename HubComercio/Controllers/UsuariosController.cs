using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HubComercio.Data;
using Microsoft.AspNetCore.Authorization;
using HubComercio.Models;

namespace HubComercio.Controllers
{
    [Authorize(Roles = "Administrador,Admin")]
    public class UsuariosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsuariosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Usuarios
        [Authorize(Roles = "Administrador,Admin")]
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Usuarios.Include(u => u.Tenant);
            return View(await applicationDbContext.ToListAsync());
        }
        // GET: Usuarios/Create
        [Authorize(Roles = "Administrador,Admin")]
        public IActionResult Create()
        {
            ViewData["TenantId"] = new SelectList(_context.Tenants, "Id", "NomeEstabelecimento");
            return View();
        }

        // POST: Usuarios/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Admin")]
        public async Task<IActionResult> Create([Bind("IdUsuario,Nome,Telefone,Email,Senha,Cargo,TenantId")] Usuario usuario)
        {
            if (ModelState.IsValid)
            {
                _context.Add(usuario);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["TenantId"] = new SelectList(_context.Tenants, "Id", "NomeEstabelecimento", usuario.TenantId);
            return View(usuario);
        }

        // GET: Usuarios/Edit/5
        [Authorize(Roles = "Administrador,Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }
            ViewData["TenantId"] = new SelectList(_context.Tenants, "Id", "NomeEstabelecimento", usuario.TenantId);
            return View(usuario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("IdUsuario,Nome,Telefone,Email,Senha,Cargo,TenantId")] Usuario usuario)
        {
            if (id != usuario.IdUsuario)
            {
                return NotFound();
            }

            ModelState.Remove("Tenant");

            if (!ModelState.IsValid)
            {
                ViewData["TenantId"] = new SelectList(_context.Tenants, "Id", "NomeEstabelecimento", usuario.TenantId);
                return View(usuario);
            }

            try
            {
                _context.Update(usuario);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UsuarioExists(usuario.IdUsuario))
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

        // POST: Usuarios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario != null)
            {
                _context.Usuarios.Remove(usuario);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Administrador,Admin")]
        private bool UsuarioExists(int id)
        {
            return _context.Usuarios.Any(e => e.IdUsuario == id);
        }
    }
}
