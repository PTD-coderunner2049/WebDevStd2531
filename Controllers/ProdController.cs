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
            // Fetch
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
                return RedirectToAction("Login", "User");

            var cart = _db.Orders
                .Include(o => o.OrderProducts!)
                .ThenInclude(op => op.Product)
                .FirstOrDefault(o => o.UserId == currentUserId && o.Status == "Pending");

            // got the order, now access orderproduct and map all it's products to CartItemViewModel
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
                        MaxStock = op.Product.Stock,
                        Description = op.Type,
                        Discount = op.Product.Discount,
                        Tax = op.Product.Tax
                    })
                    .ToList();
            }
            return View(cartItems);
        }
        [HttpPost]
        public async Task<IActionResult> Pay(List<CartItemViewModel> list)
        {
            if (list == null || !list.Any())
                return RedirectToAction("CartDetail");
            //fetch order (only one pending per user)
            var orderProduct = await _db.OrderProducts
                .Include(op => op.Order)
                .FirstOrDefaultAsync(op => op.Id == list.First().OrderProductId);

            var orderToUpdate = orderProduct?.Order;

            if (orderToUpdate == null)
                return RedirectToAction("CartDetail");

            // Start a transaction so all operation go at once.(stock updates and status changes)
            using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                orderToUpdate.Status = "Completed";
                orderToUpdate.PaidAt = DateTime.UtcNow;

                foreach (var item in list)
                {
                    //fetch product
                    var product = await _db.Products.FindAsync(item.ProductId);

                    if (product != null)
                    {
                        product.Stock -= item.Quantity;

                        // stock safeguard, undo entire transaction if any product goes below 0
                        // I should add somthing to notify user that stock is insufficient... but the
                        // requirement doesn't ask for it so...
                        if (product.Stock < 0)
                        {
                            await transaction.RollbackAsync();
                            return RedirectToAction("CartDetail");
                        }
                    }
                }

                await _db.SaveChangesAsync();
                //Commit the transaction
                await transaction.CommitAsync();
                return RedirectToAction("Index", "Home");
            }
            catch (Exception)
            {
                // Rollback on any failure (database error, unexpected exception)
                await transaction.RollbackAsync();
                return RedirectToAction("CartDetail");
            }
        }
        [HttpPost]
        public async Task<IActionResult> AddCart(AddCartViewModel model)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
                return RedirectToAction("Login", "User");

            // Fetch Product
            var product = await _db.Products.FindAsync(model.ProductId);
            if (product == null)
                return NotFound();

            if (model.Quantity <= 0)
                return RedirectToAction("CartDetail"); // Or display an error??

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // Find/Create Pending Order
                var cart = await _db.Orders
                    .Include(o => o.OrderProducts!)
                    .FirstOrDefaultAsync(o => o.UserId == currentUserId && o.Status == "Pending");

                if (cart == null)
                {
                    cart = new Order
                    {
                        UserId = currentUserId,
                        Status = "Pending",
                        CreatedAt = DateTime.UtcNow,
                        OrderProducts = new List<OrderProduct>()
                    };
                    _db.Orders.Add(cart);
                    await _db.SaveChangesAsync();
                }

                // Add OrderProduct
                var existingOrderProduct = cart.OrderProducts!
                    .FirstOrDefault(op => op.ProductId == model.ProductId && op.Type == model.SelectedType);

                if (existingOrderProduct != null)
                {
                    // Exist, increase
                    existingOrderProduct.Quantity = (existingOrderProduct.Quantity ?? 0) + model.Quantity;
                }
                else // Add new
                {
                    var newOrderProduct = new OrderProduct
                    {
                        ProductId = model.ProductId,
                        OrderId = cart.Id!.Value,
                        Quantity = model.Quantity,
                        Price = product.Price,
                        Type = model.SelectedType
                    };
                    _db.OrderProducts.Add(newOrderProduct);
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return RedirectToAction("CartDetail");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return RedirectToAction("CartDetail");
            }
        }
    }
}
