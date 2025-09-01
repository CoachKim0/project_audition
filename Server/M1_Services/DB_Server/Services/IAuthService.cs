using DbServer.Data.Entities;

namespace DbServer.Services;

public interface IAuthService
{
    Task<(bool Success, string? Token, string? UserId, long UserIdx, string Message)> LoginAsync(string username, string password);
    Task<(bool Success, string? UserId, long UserIdx, string Message)> RegisterAsync(string username, string password, string email, string nickname);
    Task<(bool Valid, string? UserId, long UserIdx, string? Username)> ValidateTokenAsync(string token);
    Task<bool> LogoutAsync(string token);
    Task<string> GenerateTokenAsync(string userId);
}