using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HubComercio.Models
{
    public class Produto
    {
        [Key]
        public int IdProduto { get; set; }

        [Required]
        [StringLength(30)]
        public string Nome { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Preco { get; set; }

        [StringLength(2)]
        public string UnidadeMedida { get; set; }

        public int Qtde { get; set; }

        public string? ImagemUrl { get; set; }

        // FK
        [Range(1, int.MaxValue, ErrorMessage = "Selecione uma categoria.")]
        public int CategoriaId { get; set; }

        [ForeignKey("CategoriaId")]
        public virtual Categoria? Categoria { get; set; }   // <<< AQUI

        public int TenantId { get; set; }

        [ForeignKey("TenantId")]
        public virtual Tenant? Tenant { get; set; }         // <<< AQUI
    }
}