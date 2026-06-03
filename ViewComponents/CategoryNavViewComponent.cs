using Microsoft.AspNetCore.Mvc;
using WebDevStd2531.Models;
using WebDevStd2531.Services;

public class CategoryNavViewComponent : ViewComponent
{
    private readonly ICatalogGrpcClient _catalogClient;

    public CategoryNavViewComponent(ICatalogGrpcClient catalogClient)
    {
        _catalogClient = catalogClient;
    }

    // This method runs whenever the View Component is called
    public async Task<IViewComponentResult> InvokeAsync()
    {
        HomeViewModelIndex homeModel = await _catalogClient.GetHomeAsync();
        return View("_Sidebar", homeModel.AllGrandCategories);
    }
}
