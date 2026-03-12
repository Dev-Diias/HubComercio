namespace HubComercio.Models.ViewModels
{
    public class ItemCarrinhoViewModel
    {
        public int ProdutoId { get; set; }
        public string NomeProduto { get; set; } = string.Empty;
        public decimal PrecoUnitario { get; set; }
        public decimal Quantidade { get; set; }
        public string UnidadeMedida { get; set; } = string.Empty;
        public string? ImagemUrl { get; set; }
        public decimal Total => PrecoUnitario * Quantidade;
    }
}