using WebDevStd2531.Models;
using WebDevStd2531.Protos;

namespace WebDevStd2531.Services;

public interface IUserAccountGrpcClient
{
    Task<UserAuthReply> RegisterAsync(RegisterViewModel model, CancellationToken cancellationToken = default);
    Task<UserAuthReply> LoginAsync(LoginViewModel model, CancellationToken cancellationToken = default);
}
