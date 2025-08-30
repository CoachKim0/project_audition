using System;
using System.Threading.Tasks;
using NetworkManager.Messages;
using Cysharp.Threading.Tasks;

namespace NetworkManager.Core
{
    /// <summary>
    /// 네트워크 전송 계층 인터페이스
    /// gRPC, TCP, UDP 구현체가 이를 구현합니다
    /// </summary>
    public interface INetworkTransport
    {
        /// <summary>
        /// 연결 상태
        /// </summary>
        bool IsConnected { get; }
        
        /// <summary>
        /// 인증 상태
        /// </summary>
        bool IsAuthenticated { get; }
        
        /// <summary>
        /// 전송 방식 타입
        /// </summary>
        TransportType TransportType { get; }
        
        /// <summary>
        /// 서버에 연결
        /// </summary>
        UniTask<bool> ConnectAsync(string host, int port);
        
        /// <summary>
        /// 서버 연결 해제
        /// </summary>
        UniTask DisconnectAsync();
        
        /// <summary>
        /// 메시지 전송
        /// </summary>
        UniTask<bool> SendMessageAsync(INetworkMessage message);
        
        /// <summary>
        /// 연결 상태 변경 이벤트
        /// </summary>
        event Action<bool> OnConnectionChanged;
        
        /// <summary>
        /// 인증 상태 변경 이벤트
        /// </summary>
        event Action<bool> OnAuthenticationChanged;
        
        /// <summary>
        /// 메시지 수신 이벤트
        /// </summary>
        event Action<INetworkMessage> OnMessageReceived;
        
        /// <summary>
        /// 오류 발생 이벤트
        /// </summary>
        event Action<string> OnError;
    }
}