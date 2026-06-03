using Microsoft.AspNetCore.Mvc;
using WebDevStd2531.Models;
using WebDevStd2531.Services;

namespace WebDevStd2531.Controllers
{
    public class CateController : Controller
    {
        private readonly ICatalogGrpcClient _catalogClient;

        public CateController(ICatalogGrpcClient catalogClient)
        {
            _catalogClient = catalogClient;
        }
        public async Task<IActionResult> CateDetail(int Id)
        {
            Category? currCate = await _catalogClient.GetCategoryAsync(Id);
            return View(currCate);
        }
    }
}
