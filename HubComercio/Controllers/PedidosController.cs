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

        public async Task<IActionResult> Index(string busca)
        {
            var tenantIdClaim = User.FindFirst("TenantId")?.Value;

            if (string.IsNullOrEmpty(tenantIdClaim))
                return Unauthorized();

            int tenantId = int.Parse(tenantIdClaim);

            var query = _context.Pedidos
                .Where(p => p.TenantId == tenantId &&
                       (p.Status == StatusPedido.Pendente || p.Status == StatusPedido.EmPreparacao))
                .Include(p => p.Itens)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(busca))
            {
                if (int.TryParse(busca, out int numeroPedido))
                {
                    query = query.Where(p => p.IdPedido == numeroPedido);
                }
                else
                {
                    query = query.Where(p => false);
                }
            }

            var pedidos = await query
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

        public async Task<JsonResult> VerificarNovosPedidos()
        {
            var tenantIdClaim = User.FindFirst("TenantId")?.Value;

            if (string.IsNullOrEmpty(tenantIdClaim))
                return Json(new { sucesso = false });

            int tenantId = int.Parse(tenantIdClaim);

            var ultimoPedidoId = await _context.Pedidos
                .Where(p => p.TenantId == tenantId)
                .OrderByDescending(p => p.IdPedido)
                .Select(p => p.IdPedido)
                .FirstOrDefaultAsync();

            return Json(new
            {
                sucesso = true,
                ultimoPedidoId
            });
        }

        public async Task<IActionResult> Historico(string busca)
        {
            var tenantIdClaim = User.FindFirst("TenantId")?.Value;

            if (string.IsNullOrEmpty(tenantIdClaim))
                return Unauthorized();

            int tenantId = int.Parse(tenantIdClaim);

            var query = _context.Pedidos
                .Where(p => p.TenantId == tenantId &&
                       (p.Status == StatusPedido.Concluido || p.Status == StatusPedido.Cancelado))
                .Include(p => p.Itens)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(busca))
            {
                if (int.TryParse(busca, out int numeroPedido))
                {
                    query = query.Where(p => p.IdPedido == numeroPedido);
                }
                else
                {
                    query = query.Where(p => false);
                }
            }

            var pedidos = await query
                .OrderByDescending(p => p.DataPedido)
                .ToListAsync();

            return View(pedidos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LimparHistorico()
        {
            var tenantIdClaim = User.FindFirst("TenantId")?.Value;

            if (string.IsNullOrEmpty(tenantIdClaim))
                return Unauthorized();

            int tenantId = int.Parse(tenantIdClaim);

            var pedidosHistorico = await _context.Pedidos
                .Where(p => p.TenantId == tenantId &&
                       (p.Status == StatusPedido.Concluido || p.Status == StatusPedido.Cancelado))
                .Include(p => p.Itens)
                .ToListAsync();

            if (!pedidosHistorico.Any())
            {
                TempData["Erro"] = "Não há pedidos no histórico para remover.";
                return RedirectToAction("Historico");
            }

            foreach (var pedido in pedidosHistorico)
            {
                if (pedido.Itens != null && pedido.Itens.Any())
                {
                    _context.ItensPedido.RemoveRange(pedido.Itens);
                }
            }

            _context.Pedidos.RemoveRange(pedidosHistorico);

            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Histórico de pedidos limpo com sucesso.";
            return RedirectToAction("Historico");
        }
    }
}