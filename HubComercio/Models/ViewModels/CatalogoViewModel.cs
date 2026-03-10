using System.Collections.Generic;
using HubComercio.Models;

namespace HubComercio.Models.ViewModels
{
    public class CatalogoViewModel
    {
        public int TenantId { get; set; }
        public string NomeEstabelecimento { get; set; }
        public string LogoUrl { get; set; }
        public string BannerUrl { get; set; }
        public string CorPrincipal { get; set; }

        public List<Produto> Produtos { get; set; } = new();
    }
}
