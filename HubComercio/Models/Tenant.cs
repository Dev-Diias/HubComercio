using System.ComponentModel.DataAnnotations;

namespace HubComercio.Models
{
    public class Tenant
    {
        [Key]
        public int Id { get; set; } // idTenants no seu DER

        [Required]
        [StringLength(150)]
        public string NomeEstabelecimento { get; set; } // Nome_Estabelecimento [cite: 36]

        [StringLength(14)]
        public string CNPJ { get; set; } // CNPJ [cite: 36]

        public DateTime DataCadastro { get; set; } = DateTime.Now; // DataCadastro [cite: 36]

        public bool Ativo { get; set; } = true; // Ativo [cite: 36]

        // Identidade Visual para o catálogo
        public string? LogoUrl { get; set; } // LogoUrl [cite: 36]
        public string? BannerUrl { get; set; } // BannerUrl [cite: 36]
        public string? CorPrincipal { get; set; } // CorPrincipal (Hexadecimal) [cite: 36]

        // Relacionamentos: Essencial para o isolamento de dados SaaS 
        public virtual ICollection<Categoria> Categorias { get; set; } = new List<Categoria>();
        public virtual ICollection<Produto> Produtos { get; set; } = new List<Produto>();
    }
}