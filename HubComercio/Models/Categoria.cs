using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HubComercio.Models
{
    public class Categoria
    {
        [Key]
        public int IdCategoria { get; set; } // IdCategoria [cite: 40]

        [Required]
        [StringLength(30)]
        public string Nome { get; set; } // Nome [cite: 40]

        public string? ImagemUrl { get; set; } // ImagemUrl [cite: 40]

        // FK para isolamento de dados
        public int TenantId { get; set; }
        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; } // Cada categoria pertence a um Tenant 
    }
}