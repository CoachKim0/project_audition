namespace Shared.Models;

public class ClientInfo
{
    public string UserId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string NickName { get; set; } = string.Empty;
    public DateTime ConnectedAt { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public ClientState State { get; set; } = ClientState.Connected;
}

public enum ClientState
{
    Connected,
    InGame,
    InChat,
    Disconnected
}