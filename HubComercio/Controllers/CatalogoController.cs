using System.Text;
using System.Text.Json;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HubComercio.Data;
using HubComercio.Models;
using HubComercio.Models.ViewModels;
using HubComercio.Helpers;

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

            var chaveCarrinho = $"Carrinho{id}";
            var carrinhoJson = HttpContext.Session.GetString(chaveCarrinho);

            var carrinho = string.IsNullOrEmpty(carrinhoJson)
                ? new List<ItemCarrinhoViewModel>()
                : JsonSerializer.Deserialize<List<ItemCarrinhoViewModel>>(carrinhoJson) ?? new List<ItemCarrinhoViewModel>();

            ViewBag.QuantidadeItensCarrinho = carrinho.Count;

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

            if (quantidade <= 0)
                return BadRequest("Quantidade inválida.");

            // produto por unidade não pode ser fracionado
            if (produto.UnidadeMedida?.ToLower() == "un" && quantidade % 1 != 0)
                return BadRequest("Produtos por unidade não podem ter quantidade fracionada.");

            var chaveCarrinho = $"Carrinho_{tenantId}";
            var carrinhoJson = HttpContext.Session.GetString(chaveCarrinho);
            var carrinho = string.IsNullOrEmpty(carrinhoJson)
                ? new List<ItemCarrinhoViewModel>()
                : JsonSerializer.Deserialize<List<ItemCarrinhoViewModel>>(carrinhoJson) ?? new List<ItemCarrinhoViewModel>();

            var itemExistente = carrinho.FirstOrDefault(i => i.ProdutoId == produtoId);

            decimal quantidadeFinal = quantidade;

            if (itemExistente != null)
                quantidadeFinal += itemExistente.Quantidade;

            // valida estoque disponível
            if (quantidadeFinal > produto.Qtde)
                return BadRequest($"Estoque insuficiente. Disponível: {produto.Qtde}");

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
        public async Task<IActionResult> EnviarPedidoWhatsApp(
     int tenantId,
     string enderecoEntrega,
     string formaPagamento,
     string telefoneCliente,
     string? precisaTroco,
     string? trocoPara)
        {
            string chaveCarrinho = $"Carrinho_{tenantId}";
            var carrinho = HttpContext.Session.GetObjectFromJson<List<ItemCarrinhoViewModel>>(chaveCarrinho);

            if (carrinho == null || !carrinho.Any())
            {
                TempData["Erro"] = "Seu carrinho está vazio.";
                return RedirectToAction("Carrinho", new { id = tenantId });
            }

            decimal totalPedido = carrinho.Sum(i => i.Total);

            bool clientePrecisaTroco = formaPagamento == "Dinheiro" && precisaTroco == "true";

            if (clientePrecisaTroco && string.IsNullOrWhiteSpace(trocoPara))
            {
                TempData["Erro"] = "Informe o valor para troco.";
                return RedirectToAction("Finalizar", new { id = tenantId });
            }

            decimal? valorTroco = null;

            if (clientePrecisaTroco && !string.IsNullOrWhiteSpace(trocoPara))
            {
                valorTroco = decimal.Parse(
                    trocoPara.Replace(".", ","),
                    new System.Globalization.CultureInfo("pt-BR"));
            }

            // VALIDA ESTOQUE ANTES DE SALVAR O PEDIDO
            foreach (var item in carrinho)
            {
                var produto = await _context.Produtos
                    .FirstOrDefaultAsync(p => p.IdProduto == item.ProdutoId && p.TenantId == tenantId);

                if (produto == null)
                {
                    TempData["Erro"] = $"O produto {item.NomeProduto} não foi encontrado.";
                    return RedirectToAction("Carrinho", new { id = tenantId });
                }

                if ((produto.UnidadeMedida?.ToLower() == "un" || produto.UnidadeMedida?.ToLower() == "unidade")
                    && item.Quantidade % 1 != 0)
                {
                    TempData["Erro"] = $"O produto {produto.Nome} só permite quantidades inteiras.";
                    return RedirectToAction("Carrinho", new { id = tenantId });
                }

                if (item.Quantidade > produto.Qtde)
                {
                    TempData["Erro"] = $"Estoque insuficiente para o produto {produto.Nome}. Disponível: {produto.Qtde}";
                    return RedirectToAction("Carrinho", new { id = tenantId });
                }
            }

            // CRIA O PEDIDO COMO PENDENTE
            var pedido = new Pedido
            {
                TenantId = tenantId,
                DataPedido = DateTime.Now,
                EnderecoEntrega = enderecoEntrega,
                FormaPagamento = formaPagamento,
                TelefoneCliente = telefoneCliente,
                ValorTotal = totalPedido,
                Status = StatusPedido.Pendente,
                PrecisaTroco = clientePrecisaTroco,
                TrocoPara = valorTroco,
                Itens = carrinho.Select(item => new ItemPedido
                {
                    ProdutoId = item.ProdutoId,
                    NomeProduto = item.NomeProduto,
                    Quantidade = item.Quantidade,
                    PrecoUnitario = item.PrecoUnitario,
                    UnidadeMedida = item.UnidadeMedida,
                    TotalItem = item.Total
                }).ToList()
            };

            _context.Pedidos.Add(pedido);

            // NÃO BAIXA ESTOQUE AQUI
            // A baixa será feita quando o lojista iniciar a preparação

            await _context.SaveChangesAsync();

            var tenant = await _context.Tenants.FindAsync(tenantId);

            if (tenant == null || string.IsNullOrEmpty(tenant.WhatsApp))
                return NotFound("WhatsApp da loja não encontrado.");

            var sb = new StringBuilder();
            var cultura = new CultureInfo("pt-BR");

            sb.AppendLine("*Novo Pedido*");
            sb.AppendLine();

            foreach (var item in carrinho)
            {
                sb.AppendLine($"- {item.NomeProduto} | Qtd: {item.Quantidade.ToString("0.###", cultura)} {item.UnidadeMedida} | Total: {item.Total.ToString("C", cultura)}");
            }

            sb.AppendLine();
            sb.AppendLine($"*Endereço:* {enderecoEntrega}");
            sb.AppendLine($"*Pagamento:* {formaPagamento}");

            if (formaPagamento == "Dinheiro")
            {
                if (clientePrecisaTroco)
                    sb.AppendLine($"*Troco para:* R$ {trocoPara}");
                else
                    sb.AppendLine("*Troco:* Não precisa");
            }

            sb.AppendLine($"*Total do Pedido:* {totalPedido.ToString("C", cultura)}");

            string mensagem = sb.ToString();

            string numero = tenant.WhatsApp
                .Replace("(", "")
                .Replace(")", "")
                .Replace("-", "")
                .Replace(" ", "");

            string mensagemCodificada = Uri.EscapeDataString(mensagem);
            string url = $"https://wa.me/{numero}?text={mensagemCodificada}";

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
        public async Task<IActionResult> AtualizarQuantidadeCarrinho(int tenantId, int produtoId, string quantidade)
        {
            if (string.IsNullOrWhiteSpace(quantidade))
                return RedirectToAction("Carrinho", new { id = tenantId });

            quantidade = quantidade.Replace(".", ",");

            if (!decimal.TryParse(quantidade, out var quantidadeDecimal) || quantidadeDecimal <= 0)
                return RedirectToAction("Carrinho", new { id = tenantId });

            var produto = await _context.Produtos
                .FirstOrDefaultAsync(p => p.IdProduto == produtoId && p.TenantId == tenantId);

            if (produto == null)
                return RedirectToAction("Carrinho", new { id = tenantId });

            if (produto.UnidadeMedida?.ToLower() == "un" && quantidadeDecimal % 1 != 0)
            {
                TempData["Erro"] = "Produtos por unidade não podem ter quantidade fracionada.";
                return RedirectToAction("Carrinho", new { id = tenantId });
            }

            if (quantidadeDecimal > produto.Qtde)
            {
                TempData["Erro"] = $"Estoque insuficiente. Disponível: {produto.Qtde}";
                return RedirectToAction("Carrinho", new { id = tenantId });
            }

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