using Shared.DTOs;

namespace Shared.Contracts;

public interface IChatServiceClient  
{
    Task<bool> SendMessageAsync(string roomId, string userId, string message);
    Task<bool> BroadcastToRoomAsync(string roomId, string message);
}

public interface ILogServiceClient
{
    Task LogAsync(string level, string message, string category = "", Dictionary<string, object>? data = null);
    Task LogBatchAsync(IEnumerable<LogEntry> logs);
}

public interface IDbServiceClient
{
    Task<UserInfo?> GetUserAsync(string userId);
    Task<bool> UpdateUserAsync(string userId, Dictionary<string, object> updates);
}

public class UserInfo
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastLogin { get; set; }
}