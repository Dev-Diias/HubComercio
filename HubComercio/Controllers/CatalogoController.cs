using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HubComercio.Data;
using HubComercio.Models;
using HubComercio.Models.ViewModels;

namespace HubComercio.Controllers
{ 

    public class CatalogoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CatalogoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // O parâmetro 'id' aqui é o ID do Mercado (Tenant)
        public async Task<IActionResult> Index(int id)
        {
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Id == id && t.Ativo);

            if (tenant == null)
                return NotFound();

            var categorias = await _context.Categorias
                .Where(c => c.TenantId == tenant.Id)
                .ToListAsync();

            var viewModel = new CatalogoViewModel
            {
                TenantId = tenant.Id,
                NomeEstabelecimento = tenant.NomeEstabelecimento,
                LogoUrl = tenant.LogoUrl,
                BannerUrl = tenant.BannerUrl,
                CorPrincipal = tenant.CorPrincipal,
                Categorias = categorias
            };

            return View(viewModel);
        }


        public async Task<IActionResult> Categoria(int id, int categoriaId)
        {
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Id == id && t.Ativo);

            if (tenant == null)
                return NotFound();

            var categoria = await _context.Categorias
                .FirstOrDefaultAsync(c => c.IdCategoria == categoriaId && c.TenantId == tenant.Id);

            if (categoria == null)
                return NotFound();

            var produtos = await _context.Produtos
                .Where(p => p.TenantId == tenant.Id && p.CategoriaId == categoriaId)
                .ToListAsync();

            var viewModel = new CatalogoViewModel
            {
                TenantId = tenant.Id,
                NomeEstabelecimento = tenant.NomeEstabelecimento,
                LogoUrl = tenant.LogoUrl,
                BannerUrl = tenant.BannerUrl,
                CorPrincipal = tenant.CorPrincipal,
                Produtos = produtos
            };

            ViewBag.NomeCategoria = categoria.Nome;

            var chaveCarrinho = $"Carrinho_{id}";
            var carrinhoJson = HttpContext.Session.GetString(chaveCarrinho);

            var carrinho = string.IsNullOrEmpty(carrinhoJson)
                ? new List<ItemCarrinhoViewModel>()
                : JsonSerializer.Deserialize<List<ItemCarrinhoViewModel>>(carrinhoJson) ?? new List<ItemCarrinhoViewModel>();

            ViewBag.QuantidadeItensCarrinho = carrinho.Count;

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> AdicionarAoCarrinho(int tenantId, int produtoId, decimal quantidade)
        {
            var produto = await _context.Produtos
                .FirstOrDefaultAsync(p => p.IdProduto == produtoId && p.TenantId == tenantId);

            if (produto == null)
                return NotFound();

            var chaveCarrinho = $"Carrinho_{tenantId}";
            var carrinhoJson = HttpContext.Session.GetString(chaveCarrinho);
            var carrinho = string.IsNullOrEmpty(carrinhoJson)
                ? new List<ItemCarrinhoViewModel>()
                : JsonSerializer.Deserialize<List<ItemCarrinhoViewModel>>(carrinhoJson) ?? new List<ItemCarrinhoViewModel>();

            var itemExistente = carrinho.FirstOrDefault(i => i.ProdutoId == produtoId);

            if (itemExistente != null)
            {
                itemExistente.Quantidade += quantidade;
            }
            else
            {
                carrinho.Add(new ItemCarrinhoViewModel
                {
                    ProdutoId = produto.IdProduto,
                    NomeProduto = produto.Nome,
                    PrecoUnitario = produto.Preco,
                    Quantidade = quantidade,
                    UnidadeMedida = produto.UnidadeMedida,
                    ImagemUrl = produto.ImagemUrl
                });
            }

            HttpContext.Session.SetString(chaveCarrinho, JsonSerializer.Serialize(carrinho));

            return Ok();
        }

        public IActionResult Carrinho(int id)
        {
            var chaveCarrinho = $"Carrinho_{id}";
            var carrinhoJson = HttpContext.Session.GetString(chaveCarrinho);

            var carrinho = string.IsNullOrEmpty(carrinhoJson)
                ? new List<ItemCarrinhoViewModel>()
                : JsonSerializer.Deserialize<List<ItemCarrinhoViewModel>>(carrinhoJson) ?? new List<ItemCarrinhoViewModel>();

            ViewBag.TenantId = id;

            return View(carrinho);
        }

        public async Task<IActionResult> Finalizar(int id)
        {
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Id == id && t.Ativo);

            if (tenant == null)
                return NotFound();

            var chaveCarrinho = $"Carrinho_{id}";
            var carrinhoJson = HttpContext.Session.GetString(chaveCarrinho);

            var carrinho = string.IsNullOrEmpty(carrinhoJson)
                ? new List<ItemCarrinhoViewModel>()
                : JsonSerializer.Deserialize<List<ItemCarrinhoViewModel>>(carrinhoJson) ?? new List<ItemCarrinhoViewModel>();

            ViewBag.TenantId = id;
            ViewBag.NomeLoja = tenant.NomeEstabelecimento;
            ViewBag.WhatsApp = tenant.WhatsApp;

            return View(carrinho);
        }

        [HttpPost]
        public async Task<IActionResult> EnviarPedidoWhatsApp(int tenantId, string endereco, string formaPagamento)
        {
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Id == tenantId && t.Ativo);

            if (tenant == null)
                return NotFound();

            var chaveCarrinho = $"Carrinho_{tenantId}";
            var carrinhoJson = HttpContext.Session.GetString(chaveCarrinho);

            var carrinho = string.IsNullOrEmpty(carrinhoJson)
                ? new List<ItemCarrinhoViewModel>()
                : JsonSerializer.Deserialize<List<ItemCarrinhoViewModel>>(carrinhoJson) ?? new List<ItemCarrinhoViewModel>();

            if (!carrinho.Any())
                return RedirectToAction("Carrinho", new { id = tenantId });

            var total = carrinho.Sum(i => i.Total);

            var mensagem = $"Olá, gostaria de fazer o seguinte pedido:\n\n";
            mensagem += $"{tenant.NomeEstabelecimento}\n\n";

            foreach (var item in carrinho)
            {
                mensagem += $"- {item.NomeProduto} - {item.Quantidade} {item.UnidadeMedida} - {item.Total.ToString("C", new System.Globalization.CultureInfo("pt-BR"))}\n";
            }

            mensagem += $"\nTotal: {total.ToString("C", new System.Globalization.CultureInfo("pt-BR"))}\n\n";
            mensagem += $"Endereço de entrega:\n{endereco}\n\n";
            mensagem += $"Forma de pagamento:\n{formaPagamento}";

            var numero = tenant.WhatsApp ?? "";

            numero = new string(numero.Where(char.IsDigit).ToArray());

            if (string.IsNullOrWhiteSpace(numero))
            {
                TempData["Erro"] = "A loja não possui um WhatsApp cadastrado.";
                return RedirectToAction("Finalizar", new { id = tenantId });
            }

            var mensagemCodificada = Uri.EscapeDataString(mensagem);
            var url = $"https://wa.me/{numero}?text={mensagemCodificada}";

            HttpContext.Session.Remove(chaveCarrinho);

            return Redirect(url);
        }

        [HttpPost]
        public IActionResult RemoverDoCarrinho(int tenantId, int produtoId)
        {
            var chaveCarrinho = $"Carrinho_{tenantId}";
            var carrinhoJson = HttpContext.Session.GetString(chaveCarrinho);

            var carrinho = string.IsNullOrEmpty(carrinhoJson)
                ? new List<ItemCarrinhoViewModel>()
                : JsonSerializer.Deserialize<List<ItemCarrinhoViewModel>>(carrinhoJson) ?? new List<ItemCarrinhoViewModel>();

            var item = carrinho.FirstOrDefault(i => i.ProdutoId == produtoId);

            if (item != null)
            {
                carrinho.Remove(item);
                HttpContext.Session.SetString(chaveCarrinho, JsonSerializer.Serialize(carrinho));
            }

            return RedirectToAction("Carrinho", new { id = tenantId });
        }

        [HttpPost]
        public IActionResult AtualizarQuantidadeCarrinho(int tenantId, int produtoId, string quantidade)
        {
            if (string.IsNullOrWhiteSpace(quantidade))
                return RedirectToAction("Carrinho", new { id = tenantId });

            quantidade = quantidade.Replace(".", ",");

            if (!decimal.TryParse(quantidade, out var quantidadeDecimal) || quantidadeDecimal <= 0)
                return RedirectToAction("Carrinho", new { id = tenantId });

            var chaveCarrinho = $"Carrinho_{tenantId}";
            var carrinhoJson = HttpContext.Session.GetString(chaveCarrinho);

            var carrinho = string.IsNullOrEmpty(carrinhoJson)
                ? new List<ItemCarrinhoViewModel>()
                : JsonSerializer.Deserialize<List<ItemCarrinhoViewModel>>(carrinhoJson) ?? new List<ItemCarrinhoViewModel>();

            var item = carrinho.FirstOrDefault(i => i.ProdutoId == produtoId);

            if (item != null)
            {
                item.Quantidade = quantidadeDecimal;
                HttpContext.Session.SetString(chaveCarrinho, JsonSerializer.Serialize(carrinho));
            }

            return RedirectToAction("Carrinho", new { id = tenantId });
        }
    }
}