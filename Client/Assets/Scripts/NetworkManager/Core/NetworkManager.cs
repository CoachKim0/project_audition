using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using gbBase;
using NetworkManager.Core;
using NetworkManager.Messages;
using NetworkManager.Utils;
using NetworkManager.Transports;

namespace NetworkManager.Core
{
    /// <summary>
    /// 네트워크 관리 메인 클래스
    /// 멀티 프로토콜을 지원하는 통합 네트워크 매니저
    /// </summary>
    public class NetworkManager : SingletonBase<NetworkManager>
    {
        [Header("설정 파일")]
        [SerializeField] private NetworkConfig _config;
        
        [Header("현재 전송 방식")]
        [SerializeField] private TransportType _currentTransportType;
        
        [Header("연결 상태 (읽기 전용)")]
        [SerializeField] private bool _isConnected;
        [SerializeField] private bool _isAuthenticated;
        [SerializeField] private string _lastError = "";
        
        // 현재 활성 전송 계층
        private INetworkTransport _currentTransport;
        private string _currentSessionToken = "";
        
        // 프로퍼티
        public bool IsConnected => _currentTransport?.IsConnected ?? false;
        public bool IsAuthenticated => _currentTransport?.IsAuthenticated ?? false;
        public TransportType CurrentTransportType => _currentTransportType;
        public NetworkConfig Config => _config;
        public string LastError => _lastError;
        
        // 통합 이벤트
        public event Action<bool> OnConnectionChanged;
        public event Action<bool> OnAuthenticationChanged;
        public event Action<INetworkMessage> OnMessageReceived;
        public event Action<string> OnError;
        
        void Awake()
        {
            // 기본 설정 로드
            if (_config == null)
            {
                _config = Resources.Load<NetworkConfig>("NetworkConfig");
                if (_config == null)
                {
                    Debug.LogWarning("[NetworkManager] NetworkConfig가 없습니다. 기본값을 사용합니다.");
                    CreateDefaultConfig();
                }
            }
            
            _currentTransportType = _config.defaultTransportType;
            
            // 초기 전송 계층 생성
            CreateTransport(_currentTransportType);
        }
        
        private void Start()
        {
            if (_config.enableDebugLog)
            {
                Debug.Log($"[NetworkManager] 초기화 완료 - 전송방식: {_currentTransportType}");
            }
        }
        
        private void Update()
        {
            // UI용 상태 업데이트
            _isConnected = IsConnected;
            _isAuthenticated = IsAuthenticated;
        }
        
        private void OnDestroy()
        {
            DisconnectAsync().Forget();
        }
        
        /// <summary>
        /// 서버에 연결
        /// </summary>
        public async UniTask<bool> ConnectAsync()
        {
            return await ConnectAsync(_config.serverAddress, _config.serverPort);
        }
        
        /// <summary>
        /// 서버에 연결 (주소/포트 지정)
        /// </summary>
        public async UniTask<bool> ConnectAsync(string host, int port)
        {
            try
            {
                if (_currentTransport == null)
                {
                    _lastError = "전송 계층이 초기화되지 않았습니다";
                    LogError(_lastError);
                    return false;
                }
                
                if (IsConnected)
                {
                    LogDebug("이미 연결되어 있습니다");
                    return true;
                }
                
                LogDebug($"서버 연결 시도: {host}:{port} ({_currentTransportType})");
                
                var result = await _currentTransport.ConnectAsync(host, port);
                
                if (result)
                {
                    LogDebug("서버 연결 성공");
                    _lastError = "";
                }
                else
                {
                    _lastError = "서버 연결 실패";
                    LogError(_lastError);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _lastError = $"연결 중 예외 발생: {ex.Message}";
                LogError(_lastError);
                return false;
            }
        }
        
        /// <summary>
        /// 서버 연결 해제
        /// </summary>
        public async UniTask DisconnectAsync()
        {
            try
            {
                if (_currentTransport != null && IsConnected)
                {
                    LogDebug("서버 연결 해제 중...");
                    await _currentTransport.DisconnectAsync();
                    LogDebug("서버 연결 해제 완료");
                }
                
                _lastError = "";
            }
            catch (Exception ex)
            {
                _lastError = $"연결 해제 중 예외 발생: {ex.Message}";
                LogError(_lastError);
            }
        }
        
        /// <summary>
        /// 메시지 전송
        /// </summary>
        public async UniTask<bool> SendMessageAsync(INetworkMessage message)
        {
            try
            {
                if (_currentTransport == null)
                {
                    _lastError = "전송 계층이 초기화되지 않았습니다";
                    LogError(_lastError);
                    return false;
                }
                
                if (!IsConnected)
                {
                    _lastError = "서버에 연결되어 있지 않습니다";
                    LogError(_lastError);
                    return false;
                }
                
                // 인증이 필요한 메시지면 세션 토큰 설정
                if (message.RequiresAuth)
                {
                    if (string.IsNullOrEmpty(_currentSessionToken))
                    {
                        _lastError = "인증 토큰이 없습니다";
                        LogError(_lastError);
                        return false;
                    }
                    message.Token = _currentSessionToken;
                }
                
                // 기본 정보 설정
                if (string.IsNullOrEmpty(message.UserId))
                    message.UserId = _config.defaultAuthId;
                if (message.Timestamp == 0)
                    message.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                
                if (_config.showNetworkTraffic)
                    LogDebug($"메시지 전송: {message.MessageType} - {message.GetType().Name}");
                
                var result = await _currentTransport.SendMessageAsync(message);
                
                if (!result)
                {
                    _lastError = "메시지 전송 실패";
                    LogError(_lastError);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _lastError = $"메시지 전송 중 예외 발생: {ex.Message}";
                LogError(_lastError);
                return false;
            }
        }
        
        /// <summary>
        /// 전송 방식 변경
        /// </summary>
        public async UniTask<bool> SwitchTransportAsync(TransportType newType)
        {
            try
            {
                if (_currentTransportType == newType)
                {
                    LogDebug($"이미 {newType} 방식을 사용 중입니다");
                    return true;
                }
                
                LogDebug($"전송 방식 변경: {_currentTransportType} → {newType}");
                
                // 현재 연결 상태 저장
                bool wasConnected = IsConnected;
                string host = _config.serverAddress;
                int port = _config.serverPort;
                
                // 현재 연결 해제
                if (_currentTransport != null)
                {
                    UnsubscribeTransportEvents(_currentTransport);
                    
                    if (IsConnected)
                    {
                        await _currentTransport.DisconnectAsync();
                    }
                }
                
                // 새 전송 계층 생성
                _currentTransportType = newType;
                CreateTransport(newType);
                
                // 이전에 연결되어 있었다면 재연결
                if (wasConnected)
                {
                    LogDebug("이전 연결 상태 복원 중...");
                    return await ConnectAsync(host, port);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _lastError = $"전송 방식 변경 중 예외 발생: {ex.Message}";
                LogError(_lastError);
                return false;
            }
        }
        
        /// <summary>
        /// 세션 토큰 설정
        /// </summary>
        public void SetSessionToken(string token)
        {
            _currentSessionToken = token;
            LogDebug($"세션 토큰 설정됨 (길이: {token?.Length ?? 0})");
        }
        
        /// <summary>
        /// 현재 세션 토큰 반환
        /// </summary>
        public string GetSessionToken()
        {
            return _currentSessionToken;
        }
        
        /// <summary>
        /// 연결 정보 문자열 반환
        /// </summary>
        public string GetConnectionInfo()
        {
            return $"전송방식: {_currentTransportType}\n" +
                   $"서버: {_config?.serverAddress}:{_config?.serverPort}\n" +
                   $"연결: {(IsConnected ? "연결됨" : "연결 안됨")}\n" +
                   $"인증: {(IsAuthenticated ? "인증됨" : "인증 안됨")}\n" +
                   $"세션토큰: {(!string.IsNullOrEmpty(_currentSessionToken) ? "있음" : "없음")}\n" +
                   $"마지막 오류: {_lastError}";
        }
        
        // Private Methods
        
        /// <summary>
        /// 전송 계층 생성
        /// </summary>
        private void CreateTransport(TransportType type)
        {
            try
            {
                _currentTransport = type switch
                {
                    TransportType.gRPC => new GrpcTransport(),
                    TransportType.TCP => new TcpTransport(),
                    TransportType.UDP => new UdpTransport(),
                    _ => throw new ArgumentException($"지원하지 않는 전송 방식: {type}")
                };
                
                SubscribeTransportEvents(_currentTransport);
                LogDebug($"전송 계층 생성됨: {type}");
            }
            catch (Exception ex)
            {
                _lastError = $"전송 계층 생성 실패: {ex.Message}";
                LogError(_lastError);
                throw;
            }
        }
        
        /// <summary>
        /// 전송 계층 이벤트 구독
        /// </summary>
        private void SubscribeTransportEvents(INetworkTransport transport)
        {
            transport.OnConnectionChanged += HandleConnectionChanged;
            transport.OnAuthenticationChanged += HandleAuthenticationChanged;
            transport.OnMessageReceived += HandleMessageReceived;
            transport.OnError += HandleTransportError;
        }
        
        /// <summary>
        /// 전송 계층 이벤트 구독 해제
        /// </summary>
        private void UnsubscribeTransportEvents(INetworkTransport transport)
        {
            transport.OnConnectionChanged -= HandleConnectionChanged;
            transport.OnAuthenticationChanged -= HandleAuthenticationChanged;
            transport.OnMessageReceived -= HandleMessageReceived;
            transport.OnError -= HandleTransportError;
        }
        
        /// <summary>
        /// 기본 설정 생성
        /// </summary>
        private void CreateDefaultConfig()
        {
            _config = ScriptableObject.CreateInstance<NetworkConfig>();
        }
        
        // Event Handlers
        
        private void HandleConnectionChanged(bool isConnected)
        {
            LogDebug($"연결 상태 변경: {isConnected}");
            OnConnectionChanged?.Invoke(isConnected);
        }
        
        private void HandleAuthenticationChanged(bool isAuthenticated)
        {
            LogDebug($"인증 상태 변경: {isAuthenticated}");
            OnAuthenticationChanged?.Invoke(isAuthenticated);
        }
        
        private void HandleMessageReceived(INetworkMessage message)
        {
            if (_config.showNetworkTraffic)
                LogDebug($"메시지 수신: {message.MessageType} - {message.GetType().Name}");
                
            OnMessageReceived?.Invoke(message);
        }
        
        private void HandleTransportError(string error)
        {
            _lastError = error;
            LogError($"전송 계층 오류: {error}");
            OnError?.Invoke(error);
        }
        
        // Utility Methods
        
        private void LogDebug(string message)
        {
            if (_config != null && _config.enableDebugLog)
            {
                Debug.Log($"[NetworkManager] {message}");
            }
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[NetworkManager] {message}");
        }
    }
}