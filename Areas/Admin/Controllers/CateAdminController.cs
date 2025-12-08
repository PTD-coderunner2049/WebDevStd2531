using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebDevStd2531.AppData;
using WebDevStd2531.Models;

namespace WebDevStd2531.Areas.Admin.Controllers
{
    public class CateAdminController : AdminController
    {
        private readonly AppDBContext _db;

        public CateAdminController(AppDBContext context)
        {
            _db = context;
        }
        public async Task<IActionResult> CateAdminist()
        {
            // Fetch
            var categories = await _db.Categories
                .Include(c => c.GrandCategory) // Eager :DDDDD
                .ToListAsync();
            return View(categories);
        }
        public IActionResult EditCate(int Id)
        {
            var currCate = _db.Categories
                .Include(c => c.GrandCategory)
                .Where(c => c.Id == Id)
                .FirstOrDefault();

            if (currCate == null)
            {
                return NotFound();
            }
            //return View("EditCate", currProd);
            return View(currCate);
        }
        public IActionResult AddCate()
        {
            var emptyCategory = new Category
            {
                Id = 0,
                Name = string.Empty,
                Description = string.Empty,
                GrandCategoryId = 0,
                GrandCategory = new GrandCategory { Name = string.Empty, Id = 0 }
            };

            return View("EditCate", emptyCategory);
        }
        [HttpPost]
        public async Task<IActionResult> AddCate(Category category, string GrandCategoryNameInput)
        {
            // I dont need this anymore since AdminController already handle this with [Authorize]
            //var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            //var appUser = await _userManager.FindByIdAsync(currentUserId);
            //if (appUser == null || !appUser.IsAdmin)
            //{
            //    return RedirectToAction("Index", "Home");
            //}

            if (string.IsNullOrWhiteSpace(GrandCategoryNameInput))
            {
                TempData["ErrorMessage"] = "The Grand Category Name is required.";
                // Ensure GrandCategory is still attached for the view to render correctly if it fails validation
                category.GrandCategory = new GrandCategory { Name = GrandCategoryNameInput };
                return View("EditCate", category);
            }

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var grandCategoryName = GrandCategoryNameInput.Trim();

                // EXISTING GRAND CATEGORY??
                var grandCategory = await _db.GrandCategories
                    // case-insensitive
                    .FirstOrDefaultAsync(gc => gc.Name.ToUpper() == grandCategoryName.ToUpper());

                if (grandCategory == null)
                {
                    grandCategory = new GrandCategory { Name = grandCategoryName };
                    _db.GrandCategories.Add(grandCategory);
                    await _db.SaveChangesAsync(); // Save to generate the GrandCategory.Id
                }
                category.GrandCategoryId = grandCategory.Id;

                if (category.Id > 0)
                {
                    _db.Categories.Update(category);
                }
                else
                {
                    // Check for duplicate category name within this Grand Category
                    var existingCategory = await _db.Categories
                        .FirstOrDefaultAsync(c => c.Name.ToUpper() == category.Name!.ToUpper() && c.GrandCategoryId == grandCategory.Id);

                    if (existingCategory != null)
                    {
                        await transaction.RollbackAsync();
                        TempData["ErrorMessage"] = $"Operation failed: A Category named '{category.Name}' already exists under the Grand Category '{grandCategory.Name}'.";
                        category.GrandCategory = grandCategory; // Attach for view rendering
                        return View("EditCate", category);
                    }

                    _db.Categories.Add(category);
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"Category '{category.Name}' was {(category.Id > 0 ? "updated" : "added")} successfully.";
                return RedirectToAction("CateAdminist");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "A database error occurred while saving the category. Operation rolled back.";
                return RedirectToAction("CateAdminist");
            }
        }
        [HttpPost]
        public async Task<IActionResult> DeleteCate(int Id)
        {
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var categoryToDelete = await _db.Categories
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.Id == Id);

                if (categoryToDelete == null)
                {
                    return RedirectToAction("CateAdminist");
                }

                var linkedProductsCount = categoryToDelete.Products?.Count ?? 0;
                if (linkedProductsCount > 0)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = $"Cannot delete Category '{categoryToDelete.Name}'. It currently has {linkedProductsCount} linked product(s). Remove or reassign the products first.";
                    return RedirectToAction("CateAdminist");
                }

                _db.Categories.Remove(categoryToDelete);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"Category '{categoryToDelete.Name}' was successfully deleted.";
                return RedirectToAction("CateAdminist");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "A critical error occurred while deleting the category. Operation rolled back.";
                return RedirectToAction("CateAdminist");
            }
        }

    }
}
