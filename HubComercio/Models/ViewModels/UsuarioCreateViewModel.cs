namespace HubComercio.Models.ViewModels
{
    public class UsuarioCreateViewModel
    {
        public string Nome { get; set; }
        public string Telefone { get; set; }
        public string Email { get; set; }
        public string Senha { get; set; }
        public string ConfirmarSenha { get; set; }
        public string Cargo { get; set; }
    }
}