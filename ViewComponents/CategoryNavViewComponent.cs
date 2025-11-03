using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebDevStd2531.AppData;
using WebDevStd2531.Models;

public class CategoryNavViewComponent : ViewComponent
{
    private readonly AppDBContext _db;

    public CategoryNavViewComponent(AppDBContext context)
    {
        _db = context;
    }

    // This method runs whenever the View Component is called
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var grandCategories = await _db.GrandCategories
            .Include(gc => gc.Categories)
            .ThenInclude(c => c.Products) // Still needed for product counts, like always sadly
            .ToListAsync();
        return View("_Sidebar", grandCategories);
    }


}