using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HubComercio.Models { 
public class Categoria
{
    [Key]
    public int IdCategoria { get; set; }

    [Required]
    public string Nome { get; set; }

    public string? ImagemUrl { get; set; }

    public int TenantId { get; set; }

    [ForeignKey("TenantId")]
    // A interrogação aqui diz ao Entity Framework: 
    // "Não precisa carregar o mercado inteiro para validar este objeto"
    public virtual Tenant? Tenant { get; set; }
}
}