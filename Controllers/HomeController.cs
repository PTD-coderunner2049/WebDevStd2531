using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebDevStd2531.AppData;
using WebDevStd2531.Models;

namespace WebDevStd2531.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger; // " _ " mean private field
        private readonly AppDBContext _db;

        public HomeController(ILogger<HomeController> logger, AppDBContext context)
        {
            _db = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            List<Product> objProductList = _db.Products.ToList();
            return View(objProductList);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
