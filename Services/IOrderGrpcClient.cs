using WebDevStd2531.Models;

namespace WebDevStd2531.Services;

public interface IOrderGrpcClient
{
    Task<List<CartItemViewModel>> GetCartAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> AddCartItemAsync(string userId, AddCartViewModel model, CancellationToken cancellationToken = default);
    Task<bool> RemoveCartItemAsync(string userId, int orderProductId, CancellationToken cancellationToken = default);
    Task<bool> IncrementCartItemAsync(string userId, int orderProductId, CancellationToken cancellationToken = default);
    Task<bool> DecrementCartItemAsync(string userId, int orderProductId, CancellationToken cancellationToken = default);
    Task<bool> PayAsync(string userId, CancellationToken cancellationToken = default);
}
