using Microsoft.AspNetCore.Mvc;
using WebDevStd2531.AppData;

namespace WebDevStd2531.Controllers
{
    public class UserController : Controller
    {
        private readonly AppDBContext _db;
        public UserController(AppDBContext context)
        {
            _db = context;
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Register()
        {
            return View();
        }
    }
}
