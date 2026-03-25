using Microsoft.AspNetCore.Http;

namespace HubComercio.Helpers
{
    public static class UploadHelper
    {
        public static async Task<string> SalvarImagem(IFormFile arquivo, string pastaBase, int tenantId)
        {
            var pasta = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "imagens",
                pastaBase,
                tenantId.ToString()
            );

            Directory.CreateDirectory(pasta);

            var extensao = Path.GetExtension(arquivo.FileName);
            var nomeArquivo = $"{Guid.NewGuid()}{extensao}";
            var caminhoCompleto = Path.Combine(pasta, nomeArquivo);

            using (var stream = new FileStream(caminhoCompleto, FileMode.Create))
            {
                await arquivo.CopyToAsync(stream);
            }

            return $"/imagens/{pastaBase}/{tenantId}/{nomeArquivo}";
        }

        public static void ExcluirImagem(string? imagemUrl)
        {
            if (string.IsNullOrWhiteSpace(imagemUrl))
                return;

            var caminhoRelativo = imagemUrl
                .TrimStart('/')
                .Replace("/", Path.DirectorySeparatorChar.ToString());

            var caminhoCompleto = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                caminhoRelativo
            );

            if (File.Exists(caminhoCompleto))
                File.Delete(caminhoCompleto);
        }
    }
}