using Grpc.Net.Client;
using Auth_Server.Protos;

namespace Shared.Services;

public class AuthServiceClient : IDisposable
{
    private readonly GrpcChannel _channel;
    private readonly AuthService.AuthServiceClient _client;
    private bool _disposed = false;

    public AuthServiceClient(string address = "http://localhost:5555")
    {
        _channel = GrpcChannel.ForAddress(address);
        _client = new AuthService.AuthServiceClient(_channel);
    }

    public async Task<LoginResponse> LoginAsync(string username, string password)
    {
        var request = new LoginRequest
        {
            Username = username,
            Password = password
        };

        return await _client.LoginAsync(request);
    }

    public async Task<RegisterResponse> RegisterAsync(string username, string password, string email)
    {
        var request = new RegisterRequest
        {
            Username = username,
            Password = password,
            Email = email
        };

        return await _client.RegisterAsync(request);
    }

    public async Task<ValidateTokenResponse> ValidateTokenAsync(string token)
    {
        var request = new ValidateTokenRequest
        {
            Token = token
        };

        return await _client.ValidateTokenAsync(request);
    }

    public async Task<RefreshTokenResponse> RefreshTokenAsync(string refreshToken)
    {
        var request = new RefreshTokenRequest
        {
            RefreshToken = refreshToken
        };

        return await _client.RefreshTokenAsync(request);
    }

    public async Task<GetUserInfoResponse> GetUserInfoAsync(int userId)
    {
        var request = new GetUserInfoRequest
        {
            UserId = userId
        };

        return await _client.GetUserInfoAsync(request);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _channel?.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}