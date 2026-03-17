using HubComercio.Data;
using HubComercio.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HubComercio.Controllers
{
    [Authorize]
    public class FinanceiroController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FinanceiroController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var tenantIdStr = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantIdStr))
                return Unauthorized();

            int tenantId = int.Parse(tenantIdStr);

            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);

            if (tenant == null)
                return NotFound();

            var pedidosQuery = _context.Pedidos.Where(p => p.TenantId == tenantId);

            if (tenant.DataResetFinanceiro.HasValue)
            {
                pedidosQuery = pedidosQuery.Where(p => p.DataPedido >= tenant.DataResetFinanceiro.Value);
            }

            var pedidos = await pedidosQuery.ToListAsync();

            int totalVendas = pedidos.Count;
            decimal valorBruto = pedidos.Sum(p => p.ValorTotal);
            decimal ticketMedio = totalVendas > 0 ? valorBruto / totalVendas : 0;

            int totalPix = pedidos.Count(p => p.FormaPagamento == "PIX");
            int totalCartao = pedidos.Count(p => p.FormaPagamento == "Cartão");
            int totalDinheiro = pedidos.Count(p => p.FormaPagamento == "Dinheiro");

            decimal percentualPix = totalVendas > 0 ? (decimal)totalPix * 100 / totalVendas : 0;
            decimal percentualCartao = totalVendas > 0 ? (decimal)totalCartao * 100 / totalVendas : 0;
            decimal percentualDinheiro = totalVendas > 0 ? (decimal)totalDinheiro * 100 / totalVendas : 0;

            var vm = new HubComercio.Models.ViewModels.FinanceiroViewModel
            {
                TotalVendas = totalVendas,
                TicketMedio = ticketMedio,
                ValorBruto = valorBruto,
                PercentualPix = percentualPix,
                PercentualCartao = percentualCartao,
                PercentualDinheiro = percentualDinheiro
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resetar()
        {
            var tenantIdStr = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantIdStr))
                return Unauthorized();

            int tenantId = int.Parse(tenantIdStr);

            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);

            if (tenant == null)
                return NotFound();

            tenant.DataResetFinanceiro = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Painel financeiro resetado com sucesso.";
            return RedirectToAction("Index");
        }
    }
}