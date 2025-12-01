using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
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
            var featuredProducts = _db.Products
                .Include(p => p.AvailableOptions)
                .ToList();
            var Categories = _db.Categories.ToList();
            var grandCategoriesWithAllData = _db.GrandCategories
                .Include(gc => gc.Categories)          // if a gcate have 0 cate inside, it is fine. I dont care.
                .ThenInclude(c => c.Products)          // I want to load everything before sending to view so I can count them in the sidebar, it has a counting number there so....
                .ThenInclude(p => p.AvailableOptions)
                .ToList();
            return View(new HomeViewModelIndex {
                FeaturedProducts = featuredProducts,
                AllCategories = Categories,
                AllGrandCategories = grandCategoriesWithAllData,
            });
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
