namespace Shared.Models;

public class ChatRoom
{
    public string RoomId { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public ChatRoomType Type { get; set; }
    public int MaxParticipants { get; set; } = 100;
    public List<string> Participants { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public enum ChatRoomType
{
    Lobby,
    GameRoom,
    OutdoorActivity,
    Private
}