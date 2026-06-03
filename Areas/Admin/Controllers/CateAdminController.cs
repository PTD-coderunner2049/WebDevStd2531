using Microsoft.AspNetCore.Mvc;
using WebDevStd2531.Models;
using WebDevStd2531.Services;

namespace WebDevStd2531.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CateAdminController : AdminController
    {
        private readonly ICatalogGrpcClient _catalogClient;

        public CateAdminController(ICatalogGrpcClient catalogClient)
        {
            _catalogClient = catalogClient;
        }

        public async Task<IActionResult> CateAdminist()
        {
            var categories = await _catalogClient.ListCategoriesAsync();
            return View(categories);
        }

        public async Task<IActionResult> EditCate(int Id)
        {
            var currCate = await _catalogClient.GetCategoryAsync(Id);
            if (currCate == null)
            {
                return NotFound();
            }

            return View(currCate);
        }

        public IActionResult AddCate()
        {
            return View("EditCate", CreateEmptyCategory());
        }

        [HttpPost]
        public async Task<IActionResult> AddCate(Category category, string GrandCategoryNameInput)
        {
            if (string.IsNullOrWhiteSpace(GrandCategoryNameInput))
            {
                TempData["ErrorMessage"] = "The Grand Category Name is required.";
                category.GrandCategory ??= new GrandCategory { Id = 0, Name = string.Empty };
                return View("EditCate", category);
            }

            var success = await _catalogClient.UpsertCategoryAsync(category, GrandCategoryNameInput);
            if (!success)
            {
                TempData["ErrorMessage"] = $"Operation failed: Grand Category '{GrandCategoryNameInput.Trim()}' does not exist or the category could not be saved.";
                category.GrandCategory ??= new GrandCategory { Id = 0, Name = GrandCategoryNameInput.Trim() };
                return View("EditCate", category);
            }

            TempData["SuccessMessage"] = $"Category '{category.Name}' was {(category.Id > 0 ? "updated" : "added")} successfully.";
            return RedirectToAction("CateAdminist");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCate(int Id)
        {
            var success = await _catalogClient.DeleteCategoryAsync(Id);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? "Category was successfully deleted."
                : "A critical error occurred while deleting the category.";
            return RedirectToAction("CateAdminist");
        }

        private static Category CreateEmptyCategory()
        {
            return new Category
            {
                Id = 0,
                Name = string.Empty,
                Description = string.Empty,
                GrandCategoryId = 0,
                GrandCategory = new GrandCategory { Id = 0, Name = string.Empty }
            };
        }
    }
}
