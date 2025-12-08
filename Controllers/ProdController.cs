using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebDevStd2531.AppData;
using WebDevStd2531.Models;

namespace WebDevStd2531.Controllers
{
    public class ProdController : Controller
    {
        private readonly AppDBContext _db;

        public ProdController(AppDBContext context)
        {
            _db = context;
        }
        public IActionResult ProdDetail(int Id)
        {
            Product? currProd = _db.Products
                .Include(p => p.Category)
                .ThenInclude(c => c.Products)
                .Include(p => p.AvailableOptions)
                .Where(p => p.Id == Id)
                .FirstOrDefault();
            //fully loaded currProd with Category and GrandCategory? for the detail view? ABSOLUTELY UNNECESSARY but why not :D
            //Now I will do another querry. accessing the already obtained CategoryId to get related products
            if (currProd == null)
            {
                return NotFound();
            }
            //var relatedProducts = _db.Products
            //    .Where(p => p.CategoryId == currProd.CategoryId && p.Id != currProd.Id).Take(4)
            //    .ToList();
            return View(currProd);

            //return View(new ProductDetailModel
            //{
            //    MainProduct = currProd!,
            //    RelatedProds = relatedProducts
            //});
        }
    }
}