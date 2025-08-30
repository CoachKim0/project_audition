using System;

namespace NetworkManager.Messages
{
    /// <summary>
    /// 네트워크 메시지 기본 인터페이스
    /// 모든 네트워크 메시지는 이 인터페이스를 구현해야 합니다
    /// </summary>
    public interface INetworkMessage
    {
        /// <summary>
        /// 메시지 타입 식별자
        /// </summary>
        string MessageType { get; }
        
        /// <summary>
        /// 사용자 ID
        /// </summary>
        string UserId { get; set; }
        
        /// <summary>
        /// 인증 토큰
        /// </summary>
        string Token { get; set; }
        
        /// <summary>
        /// 타임스탬프
        /// </summary>
        long Timestamp { get; set; }
        
        /// <summary>
        /// 인증이 필요한 메시지인지 여부
        /// </summary>
        bool RequiresAuth { get; }
        
        /// <summary>
        /// 메시지를 바이트 배열로 직렬화
        /// </summary>
        byte[] Serialize();
        
        /// <summary>
        /// 바이트 배열에서 메시지로 역직렬화
        /// </summary>
        void Deserialize(byte[] data);
    }
}