using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebDevStd2531.Models;
using WebDevStd2531.Services;

namespace WebDevStd2531.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger; // " _ " mean private field
        private readonly ICatalogGrpcClient _catalogClient;

        public HomeController(ILogger<HomeController> logger, ICatalogGrpcClient catalogClient)
        {
            _logger = logger;
            _catalogClient = catalogClient;
        }

        public async Task<IActionResult> Index()
        {
            var homeModel = await _catalogClient.GetHomeAsync();
            return View(homeModel);
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
