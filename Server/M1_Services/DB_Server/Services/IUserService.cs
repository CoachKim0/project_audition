using DbServer.Data.Entities;

namespace DbServer.Services;

public interface IUserService
{
    Task<User?> GetUserAsync(string userId);
    Task<User?> GetUserByUsernameAsync(string username);
    Task<bool> UpdateUserAsync(string userId, string? nickname = null, string? email = null);
    Task<IEnumerable<User>> GetUsersAsync(int page = 1, int pageSize = 20);
    Task<bool> DeactivateUserAsync(string userId);
    Task<bool> ActivateUserAsync(string userId);
}