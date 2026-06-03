using Grpc.Net.Client;
using WebDevStd2531.Models;
using WebDevStd2531.Protos;

namespace WebDevStd2531.Services;

public class UserAccountGrpcClient : IUserAccountGrpcClient, IDisposable
{
    private readonly GrpcChannel _channel;
    private readonly UserAccount.UserAccountClient _client;

    public UserAccountGrpcClient(IConfiguration configuration)
    {
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        var address = configuration["UserService:Address"] ?? "http://localhost:8081";
        _channel = GrpcChannel.ForAddress(address);
        _client = new UserAccount.UserAccountClient(_channel);
    }

    public async Task<UserAuthReply> RegisterAsync(RegisterViewModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.RegisterUserAsync(new RegisterUserRequest
            {
                Email = model.Email,
                UserName = model.UserName,
                Password = model.Password,
                FullName = model.FullName,
                DateOfBirth = model.DateOfBirth.ToString("O"),
                Address = model.Address ?? string.Empty,
                Gender = model.Gender,
                IsAdmin = model.IsAdmin
            }, cancellationToken: cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            return new UserAuthReply
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<UserAuthReply> LoginAsync(LoginViewModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.LoginUserAsync(new LoginUserRequest
            {
                UserName = model.UserName,
                Password = model.Password
            }, cancellationToken: cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            return new UserAuthReply
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public void Dispose()
    {
        _channel.Dispose();
    }
}
