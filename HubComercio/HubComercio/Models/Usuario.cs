using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HubComercio.Models
{
    public class Usuario
    {
        [Key]
        public int IdUsuario { get; set; } // idUsuario

        [Required]
        [StringLength(45)]
        public string Nome { get; set; }

        [StringLength(15)]
        public string Telefone { get; set; }

        [StringLength(45)]
        public string Email { get; set; }

        [StringLength(255)]
        public string Senha { get; set; }

        [StringLength(30)]
        public string Cargo { get; set; } // "Dono", "Administrador", etc.

        // Chave estrangeira para o Tenant (Isolamento SaaS)
        public int TenantId { get; set; }
        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; }
    }
}