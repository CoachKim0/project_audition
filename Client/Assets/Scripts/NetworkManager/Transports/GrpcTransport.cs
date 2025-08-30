using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Grpc.Net.Client;
using Grpc.Core;
using GrpcApp;
using NetworkManager.Core;
using NetworkManager.Messages;
using System.Threading;

namespace NetworkManager.Transports
{
    /// <summary>
    /// gRPC 전송 계층 구현체
    /// 기존 Server_Manager 로직을 기반으로 구현
    /// </summary>
    public class GrpcTransport : INetworkTransport
    {
        // gRPC 관련 필드
        private GrpcChannel _channel;
        private GameService.GameServiceClient _client;
        private AsyncDuplexStreamingCall<GameMessage, GameMessage> _gameStream;
        private CancellationTokenSource _connectionCts;
        private CancellationTokenSource _pingCts;
        
        // 상태 관리
        private bool _isConnected;
        private bool _isAuthenticated;
        private bool _intentionalDisconnect = false;
        private bool _isReconnecting = false;
        private int _reconnectAttempts = 0;
        
        // 핑 관리
        private float _lastPingTime = 0f;
        private int _pingSeqNo = 0;
        
        // 인증 관련
        private string _passKey = "";
        private string _strIV = "";
        private long _authBaseTimeStamp = 0;
        
        // 프로퍼티
        public bool IsConnected => _isConnected;
        public bool IsAuthenticated => _isAuthenticated;
        public TransportType TransportType => TransportType.gRPC;
        
        // 이벤트
        public event Action<bool> OnConnectionChanged;
        public event Action<bool> OnAuthenticationChanged;
        public event Action<INetworkMessage> OnMessageReceived;
        public event Action<string> OnError;
        
        public GrpcTransport()
        {
            LogDebug("GrpcTransport 생성됨");
        }
        
        ~GrpcTransport()
        {
            DisconnectAsync().Forget();
        }
        
        /// <summary>
        /// 서버에 연결
        /// </summary>
        public async UniTask<bool> ConnectAsync(string host, int port)
        {
            try
            {
                LogDebug($"gRPC 연결 시도: {host}:{port}");
                
                // 재연결 시도 중이라면 로그 추가
                if (_isReconnecting)
                {
                    LogDebug($"재연결 시도 {_reconnectAttempts + 1}/5");
                }
                
                // 이미 연결 중이면 먼저 해제
                if (_isConnected)
                {
                    _intentionalDisconnect = true;
                    await DisconnectAsync();
                    _intentionalDisconnect = false;
                }
                
                // 연결 취소 토큰 생성
                _connectionCts?.Cancel();
                _connectionCts = new CancellationTokenSource();
                
                // 네트워크 설정 가져오기
                var config = NetworkManager.Core.NetworkManager.Instance?.Config;
                bool useSecure = config?.useSecureConnection ?? false;
                
                // 서버 URL 생성
                string protocol = useSecure ? "https" : "http";
                string serverUrl = $"{protocol}://{host}:{port}";
                
                // 채널 옵션 설정
                var channelOptions = new GrpcChannelOptions
                {
                    MaxReceiveMessageSize = 16 * 1024 * 1024, // 16MB
                    MaxSendMessageSize = 16 * 1024 * 1024 // 16MB
                };
                
                // 채널 생성
                _channel = GrpcChannel.ForAddress(serverUrl, channelOptions);
                _client = new GameService.GameServiceClient(_channel);
                
                // 게임 스트림 시작 (양방향 스트리밍)
                _gameStream = _client.Game();
                
                // 연결 상태 업데이트
                _isConnected = true;
                _intentionalDisconnect = false;
                OnConnectionChanged?.Invoke(true);
                
                // 응답 리스너 시작
                ListenForResponsesAsync(_connectionCts.Token).Forget();
                
                // 재연결 시도가 성공했으면 재연결 상태 초기화
                if (_isReconnecting)
                {
                    _isReconnecting = false;
                    _reconnectAttempts = 0;
                    LogDebug("재연결 성공");
                }
                
                // 자동 인증 시도
                var autoAuth = config?.autoAuthenticate ?? false;
                if (autoAuth && !string.IsNullOrEmpty(config?.defaultAuthKey))
                {
                    LogDebug("자동 인증 시작");
                    await AuthenticateAsync(config.defaultAuthKey, config.defaultPlatformType, config.defaultAuthId);
                }
                
                LogDebug("gRPC 연결 성공");
                return true;
            }
            catch (Exception ex)
            {
                _isConnected = false;
                var errorMsg = $"gRPC 연결 실패: {ex.Message}";
                LogError(errorMsg);
                OnError?.Invoke(errorMsg);
                
                // 자동 재연결 시도
                await HandleConnectionFailure();
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
                LogDebug("gRPC 연결 해제 시작");
                
                _intentionalDisconnect = true;
                
                // 모든 작업 취소
                _connectionCts?.Cancel();
                _pingCts?.Cancel();
                
                if (_gameStream != null)
                {
                    try
                    {
                        await _gameStream.RequestStream.CompleteAsync();
                    }
                    catch (Exception ex)
                    {
                        LogDebug($"스트림 완료 중 오류 (무시됨): {ex.Message}");
                    }
                    
                    _gameStream.Dispose();
                    _gameStream = null;
                }
                
                if (_channel != null)
                {
                    await _channel.ShutdownAsync();
                    _channel = null;
                    _client = null;
                }
                
                // 상태 초기화
                _isConnected = false;
                _isAuthenticated = false;
                _passKey = "";
                _strIV = "";
                _authBaseTimeStamp = 0;
                
                OnConnectionChanged?.Invoke(false);
                OnAuthenticationChanged?.Invoke(false);
                
                LogDebug("gRPC 연결 해제 완료");
            }
            catch (Exception ex)
            {
                LogError($"연결 해제 중 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 메시지 전송
        /// </summary>
        public async UniTask<bool> SendMessageAsync(INetworkMessage message)
        {
            if (!_isConnected || _gameStream == null)
            {
                LogError("gRPC 연결되어 있지 않음");
                return false;
            }
            
            try
            {
                // INetworkMessage를 GameMessage로 변환
                var grpcMessage = ConvertToGrpcMessage(message);
                if (grpcMessage == null)
                {
                    LogError("메시지 변환 실패");
                    return false;
                }
                
                await _gameStream.RequestStream.WriteAsync(grpcMessage);
                LogDebug($"gRPC 메시지 전송 완료: {message.MessageType}");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"gRPC 메시지 전송 실패: {ex.Message}");
                
                // 전송 실패시 연결 상태 확인
                await HandleConnectionFailure();
                return false;
            }
        }
        
        /// <summary>
        /// 핑 전송 (주기적 호출용)
        /// </summary>
        public async UniTask<bool> SendPingAsync()
        {
            if (!_isAuthenticated || !_isConnected || _gameStream == null)
            {
                return false;
            }
            
            try
            {
                _pingSeqNo++;
                
                var pingMessage = new GameMessage
                {
                    UserId = NetworkManager.Core.NetworkManager.Instance?.Config?.defaultAuthId ?? "testuser",
                    Token = NetworkManager.Core.NetworkManager.Instance?.GetSessionToken() ?? "",
                    Message = "Ping 요청",
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Ping = new GrpcApp.Ping
                    {
                        SeqNo = _pingSeqNo
                    }
                };
                
                await _gameStream.RequestStream.WriteAsync(pingMessage);
                _lastPingTime = Time.time;
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Ping 전송 실패: {ex.Message}");
                await HandleConnectionFailure();
                return false;
            }
        }
        
        // Private Methods
        
        /// <summary>
        /// 인증 처리 (내부용)
        /// </summary>
        private async UniTask<bool> AuthenticateAsync(string authKey, int platformType, string userId)
        {
            if (!_isConnected || _gameStream == null)
            {
                LogError("인증 실패: 서버에 연결되어 있지 않음");
                return false;
            }
            
            try
            {
                _authBaseTimeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                
                var authMessage = new GameMessage
                {
                    UserId = userId,
                    Token = authKey,
                    Message = "AuthUser 요청",
                    Timestamp = _authBaseTimeStamp,
                    AuthUser = new GrpcApp.AuthUser
                    {
                        PlatformType = platformType,
                        AuthKey = authKey
                    }
                };
                
                LogDebug($"인증 요청 전송: UserId={userId}");
                await _gameStream.RequestStream.WriteAsync(authMessage);
                return true;
            }
            catch (Exception ex)
            {
                LogError($"인증 요청 실패: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 서버 응답 수신 루프
        /// </summary>
        private async UniTaskVoid ListenForResponsesAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (await _gameStream.ResponseStream.MoveNext(cancellationToken))
                    {
                        var response = _gameStream.ResponseStream.Current;
                        HandleGrpcResponse(response);
                    }
                    else
                    {
                        LogDebug("응답 스트림 종료됨");
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                LogDebug("응답 리스너 취소됨");
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
            {
                LogDebug("gRPC 스트림 취소됨");
            }
            catch (Exception ex)
            {
                LogError($"응답 수신 중 오류: {ex.Message}");
                await HandleConnectionFailure();
            }
        }
        
        /// <summary>
        /// gRPC 응답 처리
        /// </summary>
        private void HandleGrpcResponse(GameMessage response)
        {
            try
            {
                LogDebug($"gRPC 응답 수신: {response.MessageTypeCase}, 결과: {response.ResultCode}");
                
                switch (response.MessageTypeCase)
                {
                    case GameMessage.MessageTypeOneofCase.AuthUser:
                        HandleAuthUserResponse(response);
                        break;
                        
                    case GameMessage.MessageTypeOneofCase.Ping:
                        LogDebug($"Ping 응답: SeqNo={response.Ping.SeqNo}");
                        break;
                        
                    case GameMessage.MessageTypeOneofCase.Kick:
                        LogError($"서버에서 연결 종료 요청: {response.ResultMessage}");
                        DisconnectAsync().Forget();
                        break;
                        
                    default:
                        // 기타 메시지는 INetworkMessage로 변환하여 전달
                        var networkMessage = ConvertToNetworkMessage(response);
                        if (networkMessage != null)
                        {
                            OnMessageReceived?.Invoke(networkMessage);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                LogError($"gRPC 응답 처리 중 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 인증 응답 처리
        /// </summary>
        private void HandleAuthUserResponse(GameMessage response)
        {
            if (response.ResultCode == (int)ResultCode.Success)
            {
                try
                {
                    _passKey = response.AuthUser?.RetPassKey ?? "";
                    _strIV = response.AuthUser?.RetSubPassKey ?? "";
                    
                    if (!string.IsNullOrEmpty(_passKey))
                    {
                        _isAuthenticated = true;
                        OnAuthenticationChanged?.Invoke(true);
                        LogDebug("gRPC 인증 성공");
                        
                        // AuthMessage로 변환하여 상위 레이어에 전달
                        var authMessage = ConvertAuthResponseToNetworkMessage(response);
                        if (authMessage != null)
                        {
                            OnMessageReceived?.Invoke(authMessage);
                        }
                    }
                    else
                    {
                        LogError("PassKey를 받지 못했습니다");
                        _isAuthenticated = false;
                        OnAuthenticationChanged?.Invoke(false);
                    }
                }
                catch (Exception ex)
                {
                    LogError($"인증 응답 처리 중 오류: {ex.Message}");
                    _isAuthenticated = false;
                    OnAuthenticationChanged?.Invoke(false);
                }
            }
            else
            {
                _isAuthenticated = false;
                OnAuthenticationChanged?.Invoke(false);
                LogError($"인증 실패: {response.ResultMessage} (코드: {response.ResultCode})");
                
                // 실패한 인증 응답도 상위 레이어에 전달
                var authMessage = ConvertAuthResponseToNetworkMessage(response);
                if (authMessage != null)
                {
                    OnMessageReceived?.Invoke(authMessage);
                }
            }
        }
        
        /// <summary>
        /// INetworkMessage를 GameMessage로 변환
        /// </summary>
        private GameMessage ConvertToGrpcMessage(INetworkMessage message)
        {
            try
            {
                var grpcMessage = new GameMessage
                {
                    UserId = message.UserId,
                    Token = message.Token,
                    Timestamp = message.Timestamp
                };
                
                switch (message)
                {
                    case AuthMessage authMsg:
                        grpcMessage.Message = "AuthUser 요청";
                        grpcMessage.AuthUser = new GrpcApp.AuthUser
                        {
                            PlatformType = authMsg.PlatformType,
                            AuthKey = authMsg.AuthKey,
                            RetPassKey = authMsg.RetPassKey,
                            RetSubPassKey = authMsg.RetSubPassKey
                        };
                        break;
                        
                    case PingMessage pingMsg:
                        grpcMessage.Message = "Ping 요청";
                        grpcMessage.Ping = new GrpcApp.Ping
                        {
                            SeqNo = pingMsg.SeqNo
                        };
                        break;
                        
                    case ChatMessage chatMsg:
                        grpcMessage.Message = chatMsg.Content;
                        // Chat 기능은 현재 protobuf에 정의되지 않아 주석처리
                        // grpcMessage.Chat = new Chat
                        // {
                        //     // Chat 구조에 맞게 수정 필요
                        // };
                        break;
                        
                    default:
                        LogError($"지원하지 않는 메시지 타입: {message.GetType().Name}");
                        return null;
                }
                
                return grpcMessage;
            }
            catch (Exception ex)
            {
                LogError($"메시지 변환 실패: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// GameMessage를 INetworkMessage로 변환
        /// </summary>
        private INetworkMessage ConvertToNetworkMessage(GameMessage grpcMessage)
        {
            try
            {
                switch (grpcMessage.MessageTypeCase)
                {
                    // Chat 기능은 현재 protobuf에 정의되지 않아 주석처리
                    // case GameMessage.MessageTypeOneofCase.Chat:
                    //     return new ChatMessage
                    //     {
                    //         UserId = grpcMessage.UserId,
                    //         Token = grpcMessage.Token,
                    //         Timestamp = grpcMessage.Timestamp,
                    //         // Chat 구조에 맞게 수정 필요
                    //     };
                        
                    default:
                        LogDebug($"변환되지 않는 메시지 타입: {grpcMessage.MessageTypeCase}");
                        return null;
                }
            }
            catch (Exception ex)
            {
                LogError($"gRPC 메시지 변환 실패: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 인증 응답을 AuthMessage로 변환
        /// </summary>
        private AuthMessage ConvertAuthResponseToNetworkMessage(GameMessage grpcResponse)
        {
            try
            {
                return new AuthMessage
                {
                    UserId = grpcResponse.UserId,
                    Token = grpcResponse.Token,
                    Timestamp = grpcResponse.Timestamp,
                    AuthType = AuthType.TokenAuth, // gRPC는 기본적으로 TokenAuth
                    RetPassKey = grpcResponse.AuthUser?.RetPassKey ?? "",
                    RetSubPassKey = grpcResponse.AuthUser?.RetSubPassKey ?? "",
                    ResultCode = grpcResponse.ResultCode,
                    ResultMessage = grpcResponse.ResultMessage ?? ""
                };
            }
            catch (Exception ex)
            {
                LogError($"인증 응답 변환 실패: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 연결 실패 처리
        /// </summary>
        private async UniTask HandleConnectionFailure()
        {
            if (_intentionalDisconnect || _isReconnecting) return;
            
            _isConnected = false;
            OnConnectionChanged?.Invoke(false);
            
            var config = NetworkManager.Core.NetworkManager.Instance?.Config;
            if (config?.autoReconnect == true)
            {
                LogDebug("자동 재연결 시작");
                _isReconnecting = true;
                ReconnectAfterDelayAsync().Forget();
            }
        }
        
        /// <summary>
        /// 재연결 시도
        /// </summary>
        private async UniTaskVoid ReconnectAfterDelayAsync()
        {
            try
            {
                var config = NetworkManager.Core.NetworkManager.Instance?.Config;
                int maxAttempts = config?.maxReconnectAttempts ?? 5;
                float interval = config?.reconnectInterval ?? 5f;
                
                while (_reconnectAttempts < maxAttempts && !_intentionalDisconnect)
                {
                    _reconnectAttempts++;
                    
                    float currentInterval = interval * Mathf.Min(Mathf.Pow(1.5f, _reconnectAttempts - 1), 5);
                    LogDebug($"{currentInterval:F1}초 후 재연결 시도... (시도 {_reconnectAttempts}/{maxAttempts})");
                    
                    await UniTask.Delay(TimeSpan.FromSeconds(currentInterval));
                    
                    if (_intentionalDisconnect || _isConnected) break;
                    
                    // 기존 리소스 정리
                    CleanupResources();
                    
                    bool result = await ConnectAsync(config?.serverAddress ?? "127.0.0.1", config?.serverPort ?? 5554);
                    
                    if (result)
                    {
                        _isReconnecting = false;
                        LogDebug("재연결 성공");
                        return;
                    }
                }
                
                if (_reconnectAttempts >= maxAttempts)
                {
                    LogError($"최대 재연결 시도 횟수 ({maxAttempts}회) 초과");
                }
                
                _isReconnecting = false;
            }
            catch (Exception ex)
            {
                LogError($"재연결 시도 중 오류: {ex.Message}");
                _isReconnecting = false;
            }
        }
        
        /// <summary>
        /// 리소스 정리
        /// </summary>
        private void CleanupResources()
        {
            try
            {
                if (_gameStream != null)
                {
                    _gameStream.Dispose();
                    _gameStream = null;
                }
                
                if (_channel != null)
                {
                    _channel.ShutdownAsync().AsUniTask().Forget();
                    _channel = null;
                    _client = null;
                }
            }
            catch (Exception ex)
            {
                LogDebug($"리소스 정리 중 오류 (무시됨): {ex.Message}");
            }
        }
        
        // Utility Methods
        
        private void LogDebug(string message)
        {
            var config = NetworkManager.Core.NetworkManager.Instance?.Config;
            if (config != null && config.enableDebugLog)
            {
                Debug.Log($"[GrpcTransport] {message}");
            }
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[GrpcTransport] {message}");
        }
    }
}