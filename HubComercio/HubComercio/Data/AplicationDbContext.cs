using Microsoft.EntityFrameworkCore;
using HubComercio.Models;

namespace HubComercio.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // Mapeando as tabelas do seu projeto
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Produto> Produtos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configura a precisão do preço (você já tinha isso)
            modelBuilder.Entity<Produto>()
                .Property(p => p.Preco)
                .HasPrecision(10, 2);

            // RESOLUÇÃO DO ERRO:
            // Remove o "Cascade Delete" entre Tenant e Produto para evitar o ciclo.
            // Isso significa que se um Tenant for deletado, o EF não tentará 
            // deletar os produtos por esse caminho (ele fará via Categoria ou manualmente).
            modelBuilder.Entity<Produto>()
                .HasOne(p => p.Tenant)
                .WithMany(t => t.Produtos)
                .HasForeignKey(p => p.TenantId)
                .OnDelete(DeleteBehavior.Restrict); // Mudei de Cascade para Restrict
        }
    }
}