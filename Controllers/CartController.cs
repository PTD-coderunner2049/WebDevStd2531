using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebDevStd2531.AppData;
using WebDevStd2531.Models;

namespace WebDevStd2531.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDBContext _db;

        public CartController(AppDBContext context)
        {
            _db = context;
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
                        Discount = op.Product.Discount,
                        Tax = op.Product.Tax,
                        SelectedType = op.Type
                    })
                    .ToList();
            }
            return View(cartItems);
        }
        [HttpPost]
        public async Task<IActionResult> AddCartItem(AddCartViewModel model)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
                return RedirectToAction("Login", "User");

            if (model.Quantity <= 0)
                return RedirectToAction("CartDetail"); // Or display an error??

            var product = await _db.Products.FindAsync(model.ProductId);
            if (product == null)
                return NotFound();

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

                // Add: Exist? increase, None? addNew
                var existingOrderProduct = cart.OrderProducts!
                    .FirstOrDefault(op => op.ProductId == model.ProductId && op.Type == model.SelectedType);

                if (existingOrderProduct != null)
                {
                    existingOrderProduct.Quantity = (existingOrderProduct.Quantity ?? 0) + model.Quantity;
                }
                else
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
        [HttpPost]
        public async Task<IActionResult> RemoveCartItem(int OrderProductId)
        {
            // Find the OrderProduct entity (the M:N entry)
            var orderProductToRemove = await _db.OrderProducts.FindAsync(OrderProductId);

            if (orderProductToRemove != null)
            {
                _db.OrderProducts.Remove(orderProductToRemove);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction("CartDetail");
        }
        [HttpPost]
        public async Task<IActionResult> Pay(List<CartItemViewModel> list)
        {
            if (list == null || !list.Any())
                return RedirectToAction("CartDetail");
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
                return RedirectToAction("Login", "User");

            //fetch order (only one pending per user)
            var orderToUpdate = await _db.Orders
                .FirstOrDefaultAsync(o => o.UserId == currentUserId && o.Status == "Pending");
            if (orderToUpdate == null)
                return RedirectToAction("CartDetail");

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                foreach (var item in list)
                {
                    //fetch product (single)
                    var product = await _db.Products.FindAsync(item.ProductId);

                    if (product != null)
                    {

                        // stock safeguard, undo entire transaction if any product goes below 0
                        // I should add somthing to notify user that stock is insufficient... but the
                        // requirement doesn't ask for it so...
                        if (product.Stock < item.Quantity)
                        {
                            await transaction.RollbackAsync();
                            TempData["StockError"] = $"Transaction failed: Insufficient stock for {product.Name}. The payment cant be proceed.";
                            return RedirectToAction("CartDetail");
                        }
                        product.Stock -= item.Quantity;
                    }
                }
                orderToUpdate.Status = "Completed";
                orderToUpdate.PaidAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                //Commit the transaction
                await transaction.CommitAsync();
                return RedirectToAction("Index", "Home");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return RedirectToAction("CartDetail");
            }
        }
    }
}