using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HubComercio.Controllers
{
    [Authorize]
    public class GestaoController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}