using WebDevStd2531.Models;

namespace WebDevStd2531.Services;

public interface ICatalogGrpcClient
{
    Task<HomeViewModelIndex> GetHomeAsync(CancellationToken cancellationToken = default);
    Task<Product?> GetProductAsync(int id, CancellationToken cancellationToken = default);
    Task<Category?> GetCategoryAsync(int id, CancellationToken cancellationToken = default);
    Task<List<Product>> ListProductsAsync(CancellationToken cancellationToken = default);
    Task<List<Category>> ListCategoriesAsync(CancellationToken cancellationToken = default);
    Task<bool> UpsertProductAsync(Product product, string categoryName, CancellationToken cancellationToken = default);
    Task<bool> DeleteProductAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> UpsertCategoryAsync(Category category, string grandCategoryName, CancellationToken cancellationToken = default);
    Task<bool> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default);
}
