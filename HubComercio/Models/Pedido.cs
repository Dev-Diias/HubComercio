using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HubComercio.Models
{
    public class Pedido
    {
        [Key]
        public int IdPedido { get; set; }

        [Required]
        public int TenantId { get; set; }

        [Required]
        public DateTime DataPedido { get; set; } = DateTime.Now;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ValorTotal { get; set; }

        [Required]
        [StringLength(30)]
        public string FormaPagamento { get; set; } = string.Empty;

        [StringLength(255)]
        public string? EnderecoEntrega { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "Finalizado";

        public Tenant? Tenant { get; set; }

        public List<ItemPedido> Itens { get; set; } = new();
        public bool PrecisaTroco { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TrocoPara { get; set; }
    }
}