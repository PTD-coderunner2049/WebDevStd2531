using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebDevStd2531.AppData;
using WebDevStd2531.Models;

namespace WebDevStd2531.Areas.Admin.Controllers
{
    public class ProdAdminController : AdminController
    {
        private readonly AppDBContext _db;

        public ProdAdminController(AppDBContext context)
        {
            _db = context;
        }
        public async Task<IActionResult> ProdAdminist()
        {
            // Fetch
            var products = await _db.Products
                .Include(p => p.Category) // Eager :DDDDD
                .ToListAsync();
            return View(products);
        }
        public IActionResult EditProd(int Id)
        {
            Product? currProd = _db.Products
                .Include(p => p.Category)
                .ThenInclude(c => c.GrandCategory)
                .Include(p => p.AvailableOptions)
                .Where(p => p.Id == Id)
                .FirstOrDefault();
            //fully loaded currProd with Category and GrandCategory? for the edit view? ABSOLUTELY NECESSARY
            if (currProd == null)
            {
                return NotFound();
            }
            //return View("EditProd", currProd);

            return View(currProd);
        }
        public IActionResult AddProd()
        {
            var emptyProduct = new Product
            {
                Id = 0,
                Name = string.Empty,
                Description = string.Empty,
                Price = 0.0,
                Stock = 0,
                ImageUrl = string.Empty,
                CategoryId = 0,
                Discount = 0.0,
                Tax = 0.0
            };

            return View("EditProd", emptyProduct);
        }
        [HttpPost]
        public async Task<IActionResult> AddProd(Product product, string CategoryNameInput)
        {
            // I dont need this anymore since AdminController already handle this with [Authorize]
            //var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            //var appUser = await _userManager.FindByIdAsync(currentUserId);
            //if (appUser == null || !appUser.IsAdmin)
            //{
            //    return RedirectToAction("Index", "Home");
            //}

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var categoryName = CategoryNameInput.Trim();
                var category = await _db.Categories
                    // case-insensitive
                    .FirstOrDefaultAsync(c => c.Name.ToUpper() == categoryName.ToUpper());

                if (category == null)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = $"Operation failed: Category '{categoryName}' does not exist. Please use an existing category name.";
                    return View("EditProd", product);
                }
                product.CategoryId = category.Id;

                if (product.Id > 0)
                {
                    // Id is > 0, so it's an existing product (UPDATE)
                    _db.Products.Update(product);
                }
                else
                {
                    // Id is 0 (or default value), so it's a new product (INSERT)
                    var existingProduct = await _db.Products
                        .FirstOrDefaultAsync(p => p.Name.ToUpper() == product.Name!.ToUpper());

                    if (existingProduct != null)
                    {
                        await transaction.RollbackAsync();
                        TempData["ErrorMessage"] = $"Operation failed: A Product named '{product.Name}' already exists. Please use a unique product name.";
                        product.Category = category;
                        return View("EditProd", product);
                    }
                    _db.Products.Add(product);
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"Product '{product.Name}' was {(product.Id > 0 ? "updated" : "added")} successfully.";
                return RedirectToAction("ProdAdminist");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "A database error occurred while saving the product. Operation rolled back.";
                return RedirectToAction("EditProd");
            }
        }
        [HttpPost]
        public async Task<IActionResult> DeleteProd(int Id)
        {
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // Fetch
                var productToDelete = await _db.Products.FindAsync(Id);
                if (productToDelete == null)
                {
                    // Product already gone??? ok
                    return RedirectToAction("ProdAdminist");
                }

                //ongoing OrderProduct (active carts) that has this product
                var orderProductsInPendingCarts = await _db.OrderProducts
                    .Where(op => op.ProductId == Id && op.Order!.Status == "Pending")
                    .ToListAsync();

                if (orderProductsInPendingCarts.Any())
                {
                    _db.OrderProducts.RemoveRange(orderProductsInPendingCarts);
                }
                _db.Products.Remove(productToDelete);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"Product '{productToDelete.Name}' and {orderProductsInPendingCarts.Count} line item(s) from pending carts were successfully deleted.";
                return RedirectToAction("ProdAdminist");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Log the error (not shown here)
                TempData["ErrorMessage"] = "A critical error occurred while deleting the product. Operation rolled back.";
                return RedirectToAction("ProdAdminist");
            }
        }
    }
}
