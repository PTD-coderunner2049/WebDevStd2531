using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
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
            // curr user's ID
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                // to login page
                return RedirectToAction("Login", "User");
            }
            var cart = _db.Orders
                .Include(o => o.OrderProducts!)
                .ThenInclude(op => op.Product)
                .FirstOrDefault(o => o.UserId == currentUserId && o.Status == "Pending");

            // 3. Project the OrderProducts into your CartItemViewModel
            var cartItems = new List<CartItemViewModel>();

            if (cart != null && cart.OrderProducts != null)
            {
                cartItems = cart.OrderProducts
                    .Select(op => new CartItemViewModel
                    {
                        OrderProductId = op.Id,
                        Quantity = op.Quantity ?? 0,
                        Price = op.Price ?? 0,
                        ProductId = op.ProductId,
                        ProductName = op.Product!.Name,
                        ImageUrl = op.Product.ImageUrl,
                        MaxStock = op.Product.Stock
                    })
                    .ToList();
            }
            return View(cartItems);
        }
    }
}
