using HubComercio.Data;
using HubComercio.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HubComercio.Controllers
{
    [Authorize]
    public class PedidosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PedidosController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var tenantIdClaim = User.FindFirst("TenantId")?.Value;

            if (string.IsNullOrEmpty(tenantIdClaim))
                return Unauthorized();

            int tenantId = int.Parse(tenantIdClaim);

            var pedidos = await _context.Pedidos
                .Where(p => p.TenantId == tenantId)
                .Include(p => p.Itens)
               .OrderBy(p => p.Status == StatusPedido.Pendente ? 0 : 1)
               .ThenByDescending(p => p.DataPedido)
                .ToListAsync();

            return View(pedidos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IniciarPreparacao(int id)
        {
            var tenantIdClaim = User.FindFirst("TenantId")?.Value;

            if (string.IsNullOrEmpty(tenantIdClaim))
                return Unauthorized();

            int tenantId = int.Parse(tenantIdClaim);

            var pedido = await _context.Pedidos
                .Include(p => p.Itens)
                .FirstOrDefaultAsync(p => p.IdPedido == id && p.TenantId == tenantId);

            if (pedido == null)
                return NotFound();

            if (pedido.Status != StatusPedido.Pendente)
            {
                TempData["Erro"] = "Apenas pedidos pendentes podem ir para preparação.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var item in pedido.Itens)
            {
                var produto = await _context.Produtos
                    .FirstOrDefaultAsync(p => p.IdProduto == item.ProdutoId && p.TenantId == tenantId);

                if (produto == null)
                {
                    TempData["Erro"] = $"Produto {item.NomeProduto} não encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                if (produto.Qtde < item.Quantidade)
                {
                    TempData["Erro"] = $"Estoque insuficiente para {produto.Nome}.";
                    return RedirectToAction(nameof(Index));
                }
            }

            foreach (var item in pedido.Itens)
            {
                var produto = await _context.Produtos
                    .FirstOrDefaultAsync(p => p.IdProduto == item.ProdutoId && p.TenantId == tenantId);

                if (produto != null)
                {
                    produto.Qtde -= item.Quantidade;
                }
            }

            pedido.Status = StatusPedido.EmPreparacao;

            await _context.SaveChangesAsync();

            var mensagem = $"Olá! Seu pedido #{pedido.IdPedido} já está em preparação!";

            var numero = new string(pedido.TelefoneCliente.Where(char.IsDigit).ToArray());

            if (!numero.StartsWith("55"))
            {
                numero = "55" + numero;
            }

            var url = $"https://wa.me/{numero}?text={Uri.EscapeDataString(mensagem)}";

            return Redirect(url);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Concluir(int id)
        {
            var tenantIdClaim = User.FindFirst("TenantId")?.Value;

            if (string.IsNullOrEmpty(tenantIdClaim))
                return Unauthorized();

            int tenantId = int.Parse(tenantIdClaim);

            var pedido = await _context.Pedidos
                .FirstOrDefaultAsync(p => p.IdPedido == id && p.TenantId == tenantId);

            if (pedido == null)
                return NotFound();

            if (pedido.Status != StatusPedido.EmPreparacao)
            {
                TempData["Erro"] = "Apenas pedidos em preparação podem ser concluídos.";
                return RedirectToAction(nameof(Index));
            }

            pedido.Status = StatusPedido.Concluido;

            await _context.SaveChangesAsync();

            if (string.IsNullOrWhiteSpace(pedido.TelefoneCliente))
            {
                TempData["Sucesso"] = $"Pedido #{pedido.IdPedido} concluído com sucesso.";
                return RedirectToAction(nameof(Index));
            }

            var mensagem = $"Olá! Seu pedido #{pedido.IdPedido} foi concluído com sucesso!";

            var numero = new string(pedido.TelefoneCliente.Where(char.IsDigit).ToArray());

            if (!numero.StartsWith("55"))
            {
                numero = "55" + numero;
            }

            var url = $"https://wa.me/{numero}?text={Uri.EscapeDataString(mensagem)}";

            return Redirect(url);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id)
        {
            var tenantIdClaim = User.FindFirst("TenantId")?.Value;

            if (string.IsNullOrEmpty(tenantIdClaim))
                return Unauthorized();

            int tenantId = int.Parse(tenantIdClaim);

            var pedido = await _context.Pedidos
                .Include(p => p.Itens)
                .FirstOrDefaultAsync(p => p.IdPedido == id && p.TenantId == tenantId);

            if (pedido == null)
                return NotFound();

            if (pedido.Status == StatusPedido.Cancelado)
            {
                TempData["Erro"] = "Esse pedido já está cancelado.";
                return RedirectToAction(nameof(Index));
            }

            if (pedido.Status == StatusPedido.EmPreparacao || pedido.Status == StatusPedido.Concluido)
            {
                foreach (var item in pedido.Itens)
                {
                    var produto = await _context.Produtos
                        .FirstOrDefaultAsync(p => p.IdProduto == item.ProdutoId && p.TenantId == tenantId);

                    if (produto != null)
                    {
                        produto.Qtde += item.Quantidade;
                    }
                }
            }

            pedido.Status = StatusPedido.Cancelado;

            await _context.SaveChangesAsync();

            if (string.IsNullOrWhiteSpace(pedido.TelefoneCliente))
            {
                TempData["Sucesso"] = $"Pedido #{pedido.IdPedido} cancelado com sucesso.";
                return RedirectToAction(nameof(Index));
            }

            var mensagem = $"Olá! Seu pedido #{pedido.IdPedido} foi cancelado.";

            var numero = new string(pedido.TelefoneCliente.Where(char.IsDigit).ToArray());

            if (!numero.StartsWith("55"))
            {
                numero = "55" + numero;
            }

            var url = $"https://wa.me/{numero}?text={Uri.EscapeDataString(mensagem)}";

            return Redirect(url);
        }
    }
}