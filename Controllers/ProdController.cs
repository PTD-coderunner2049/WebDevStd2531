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
                .ThenInclude(c => c.GrandCategory)
                .Where(p => p.Id == Id)     
                .FirstOrDefault();
            //fully loaded currProd with Category and GrandCategory? for the detail view? ABSOLUTELY UNNECESSARY but why not :D
            //Now I will do another querry. accessing the already obtained CategoryId to get related products
            if (currProd == null)
            {
                return NotFound();
            }
            var relatedProducts = _db.Products
                .Where(p => p.CategoryId == currProd.CategoryId && p.Id != currProd.Id).Take(4)
                .ToList();
            return View(new ProductDetailModel
            {
                MainProduct = currProd!,
                RelatedProds = relatedProducts
            });
        }
        public IActionResult CateDetail(int Id){
            Category? currCate = _db.Categories
                .Include(c => c.Products)
                .Where(c => c.Id == Id)
                .FirstOrDefault();
            //code for test
            //if (Id == 1)
            //{
            //    if (currCate != null)
            //    {
            //        currCate.Products = new List<Product>();
            //        currCate.Name = "(TEST EMPTY) " + currCate.Name; // Just to be sure
            //    }
            //}
            return View(currCate);
        }
        public IActionResult CartDetail()
        {
            //fetch cart data ("orders table" where current user pk is the fk of that order)
            return View();
        }
    }
}
