using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebDevStd2531.AppData;
using WebDevStd2531.Models;

namespace WebDevStd2531.Controllers
{
    public class CateController : Controller
    {
        private readonly AppDBContext _db;

        public CateController(AppDBContext context)
        {
            _db = context;
        }
        public IActionResult CateDetail(int Id)
        {
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
    }
}