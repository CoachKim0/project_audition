namespace NetworkManager.Core
{
    /// <summary>
    /// 네트워크 전송 방식 열거형
    /// </summary>
    public enum TransportType
    {
        gRPC,   // HTTP/2 기반 gRPC 
        TCP,    // TCP 소켓
        UDP     // UDP 소켓
    }
}