namespace HubComercio.Models.ViewModels
{
    public class FinanceiroViewModel
    {
        public int TotalVendas { get; set; }
        public decimal TicketMedio { get; set; }
        public decimal ValorBruto { get; set; }

        public decimal PercentualPix { get; set; }
        public decimal PercentualCartao { get; set; }
        public decimal PercentualDinheiro { get; set; }
    }
}