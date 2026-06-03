using Microsoft.AspNetCore.Mvc;
using WebDevStd2531.Models;
using WebDevStd2531.Services;

namespace WebDevStd2531.Controllers
{
    public class ProdController : Controller
    {
        private readonly ICatalogGrpcClient _catalogClient;

        public ProdController(ICatalogGrpcClient catalogClient)
        {
            _catalogClient = catalogClient;
        }
        public async Task<IActionResult> ProdDetail(int Id)
        {
            Product? currProd = await _catalogClient.GetProductAsync(Id);
            if (currProd == null)
            {
                return NotFound();
            }
            return View(currProd);
        }
    }
}
