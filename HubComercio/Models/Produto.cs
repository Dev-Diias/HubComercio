using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HubComercio.Models
{
    public class Produto
    {
        [Key]
        public int IdProduto { get; set; } // idProduto 

        [Required]
        [StringLength(30)]
        public string Nome { get; set; } // Nome 

        [Column(TypeName = "decimal(10,2)")]
        public decimal Preco { get; set; } // Preço com precisão decimal 

        [StringLength(2)]
        public string UnidadeMedida { get; set; } // Unidade de Medida 

        public int Qtde { get; set; } // Quantidade em estoque 

        public string? ImagemUrl { get; set; } // Imagem para o catálogo visual [cite: 42]

        // Relacionamentos obrigatórios conforme o DER [cite: 43]
        public int CategoriaId { get; set; }
        [ForeignKey("CategoriaId")]
        public virtual Categoria Categoria { get; set; }

        public int TenantId { get; set; }
        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; }
    }
}