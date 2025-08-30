using UnityEngine;
using NetworkManager.Core;

namespace NetworkManager.Utils
{
    /// <summary>
    /// 네트워크 설정 관리 클래스
    /// </summary>
    [CreateAssetMenu(fileName = "NetworkConfig", menuName = "NetworkManager/Network Config")]
    public class NetworkConfig : ScriptableObject
    {
        [Header("서버 설정")]
        public string serverAddress = "127.0.0.1";
        public int serverPort = 5554;
        public bool useSecureConnection = false;
        
        [Header("전송 방식")]
        public TransportType defaultTransportType = TransportType.gRPC;
        
        [Header("연결 설정")]
        public bool autoReconnect = true;
        public float reconnectInterval = 5f;
        public int maxReconnectAttempts = 5;
        public float connectionTimeout = 30f;
        
        [Header("인증 설정")]
        public bool autoAuthenticate = true;
        public string defaultAuthId = "testuser";
        public string defaultAuthKey = "google_or_apple_auth_key_abcd_1234";
        public int defaultPlatformType = 1; // 1: Android
        
        [Header("세션 설정")]
        public float sessionExpireTime = 3600f; // 1시간
        public bool autoRefreshToken = true;
        public float refreshThreshold = 300f; // 5분 전 갱신
        
        [Header("핑 설정")]
        public float pingInterval = 30f;
        public float pingTimeout = 10f;
        
        [Header("디버그")]
        public bool enableDebugLog = true;
        public bool showNetworkTraffic = false;
    }
}