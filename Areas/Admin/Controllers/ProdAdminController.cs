using Microsoft.AspNetCore.Mvc;
using WebDevStd2531.Models;
using WebDevStd2531.Services;

namespace WebDevStd2531.Areas.Admin.Controllers
{
    public class ProdAdminController : AdminController
    {
        private readonly ICatalogGrpcClient _catalogClient;

        public ProdAdminController(ICatalogGrpcClient catalogClient)
        {
            _catalogClient = catalogClient;
        }

        public async Task<IActionResult> ProdAdminist()
        {
            var products = await _catalogClient.ListProductsAsync();
            return View(products);
        }

        public async Task<IActionResult> EditProd(int Id)
        {
            Product? currProd = await _catalogClient.GetProductAsync(Id);
            if (currProd == null)
            {
                return NotFound();
            }

            return View(currProd);
        }

        public IActionResult AddProd()
        {
            return View("EditProd", CreateEmptyProduct());
        }

        [HttpPost]
        public async Task<IActionResult> AddProd(Product product, string CategoryNameInput)
        {
            if (string.IsNullOrWhiteSpace(CategoryNameInput))
            {
                TempData["ErrorMessage"] = "The Category Name is required.";
                product.Category ??= new Category
                {
                    Id = 0,
                    Name = string.Empty,
                    Description = string.Empty,
                    GrandCategoryId = 0,
                    GrandCategory = new GrandCategory { Id = 0, Name = string.Empty }
                };
                return View("EditProd", product);
            }

            var success = await _catalogClient.UpsertProductAsync(product, CategoryNameInput);
            if (!success)
            {
                TempData["ErrorMessage"] = $"Operation failed: Category '{CategoryNameInput.Trim()}' does not exist or the product could not be saved.";
                product.Category ??= new Category
                {
                    Id = 0,
                    Name = CategoryNameInput.Trim(),
                    Description = string.Empty,
                    GrandCategoryId = 0,
                    GrandCategory = new GrandCategory { Id = 0, Name = string.Empty }
                };
                return View("EditProd", product);
            }

            TempData["SuccessMessage"] = $"Product '{product.Name}' was {(product.Id > 0 ? "updated" : "added")} successfully.";
            return RedirectToAction("ProdAdminist");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProd(int Id)
        {
            var success = await _catalogClient.DeleteProductAsync(Id);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? "Product was successfully deleted."
                : "A critical error occurred while deleting the product.";
            return RedirectToAction("ProdAdminist");
        }

        private static Product CreateEmptyProduct()
        {
            return new Product
            {
                Id = 0,
                Name = string.Empty,
                Description = string.Empty,
                Price = 0.0,
                Stock = 0,
                ImageUrl = string.Empty,
                CategoryId = 0,
                Discount = 0.0,
                Tax = 0.0,
                Category = new Category
                {
                    Id = 0,
                    Name = string.Empty,
                    Description = string.Empty,
                    GrandCategoryId = 0,
                    GrandCategory = new GrandCategory { Id = 0, Name = string.Empty }
                },
                AvailableOptions = new List<ProductOption>()
            };
        }
    }
}
