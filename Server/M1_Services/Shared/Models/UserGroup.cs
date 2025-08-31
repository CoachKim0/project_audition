namespace Shared.Models;

public class UserGroup
{
    public string GroupId { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public List<string> UserIds { get; set; } = new();
    public UserGroupType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public int MaxMembers { get; set; } = 8;
}

public enum UserGroupType
{
    GameRoom,
    ChatRoom,
    Guild,
    Party
}