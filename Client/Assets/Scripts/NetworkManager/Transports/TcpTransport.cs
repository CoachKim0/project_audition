using System;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using NetworkManager.Core;
using NetworkManager.Messages;

namespace NetworkManager.Transports
{
    /// <summary>
    /// TCP 전송 계층 구현체
    /// </summary>
    public class TcpTransport : INetworkTransport
    {
        // TCP 관련 필드
        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _connectionCts;
        
        // 상태 관리
        private bool _isConnected;
        private bool _isAuthenticated;
        private bool _intentionalDisconnect = false;
        private bool _isReconnecting = false;
        private int _reconnectAttempts = 0;
        
        // 수신 버퍼
        private byte[] _receiveBuffer = new byte[4096];
        
        // 프로퍼티
        public bool IsConnected => _isConnected && _client?.Connected == true;
        public bool IsAuthenticated => _isAuthenticated;
        public TransportType TransportType => TransportType.TCP;
        
        // 이벤트
        public event Action<bool> OnConnectionChanged;
        public event Action<bool> OnAuthenticationChanged;
        public event Action<INetworkMessage> OnMessageReceived;
        public event Action<string> OnError;
        
        public TcpTransport()
        {
            LogDebug("TcpTransport 생성됨");
        }
        
        ~TcpTransport()
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
                LogDebug($"TCP 연결 시도: {host}:{port}");
                
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
                
                // TCP 클라이언트 생성 및 연결
                _client = new TcpClient();
                
                var config = NetworkManager.Core.NetworkManager.Instance?.Config;
                float timeout = config?.connectionTimeout ?? 30f;
                
                // 연결 시도 (타임아웃 적용)
                var connectTask = _client.ConnectAsync(host, port);
                var timeoutTask = UniTask.Delay(TimeSpan.FromSeconds(timeout), cancellationToken: _connectionCts.Token);
                
                var completedTask = await UniTask.WhenAny(connectTask.AsUniTask(), timeoutTask);
                
                if (completedTask != 0) // 타임아웃
                {
                    throw new TimeoutException($"연결 시간 초과 ({timeout}초)");
                }
                
                // 스트림 가져오기
                _stream = _client.GetStream();
                
                // 연결 상태 업데이트
                _isConnected = true;
                _intentionalDisconnect = false;
                OnConnectionChanged?.Invoke(true);
                
                // 수신 루프 시작
                ReceiveLoopAsync(_connectionCts.Token).Forget();
                
                // 재연결 시도가 성공했으면 재연결 상태 초기화
                if (_isReconnecting)
                {
                    _isReconnecting = false;
                    _reconnectAttempts = 0;
                    LogDebug("재연결 성공");
                }
                
                LogDebug("TCP 연결 성공");
                return true;
            }
            catch (Exception ex)
            {
                _isConnected = false;
                var errorMsg = $"TCP 연결 실패: {ex.Message}";
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
                LogDebug("TCP 연결 해제 시작");
                
                _intentionalDisconnect = true;
                
                // 모든 작업 취소
                _connectionCts?.Cancel();
                
                if (_stream != null)
                {
                    _stream.Close();
                    _stream.Dispose();
                    _stream = null;
                }
                
                if (_client != null)
                {
                    _client.Close();
                    _client.Dispose();
                    _client = null;
                }
                
                // 상태 초기화
                _isConnected = false;
                _isAuthenticated = false;
                
                OnConnectionChanged?.Invoke(false);
                OnAuthenticationChanged?.Invoke(false);
                
                LogDebug("TCP 연결 해제 완료");
                
                // 약간의 지연 (리소스 정리 시간)
                await UniTask.Delay(100);
            }
            catch (Exception ex)
            {
                LogError($"TCP 연결 해제 중 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 메시지 전송
        /// </summary>
        public async UniTask<bool> SendMessageAsync(INetworkMessage message)
        {
            if (!IsConnected || _stream == null)
            {
                LogError("TCP 연결되어 있지 않음");
                return false;
            }
            
            try
            {
                // 메시지 직렬화
                byte[] data = message.Serialize();
                if (data == null || data.Length == 0)
                {
                    LogError("메시지 직렬화 실패");
                    return false;
                }
                
                // 길이 헤더 (4바이트) + 메시지 타입 (1바이트) + 데이터
                byte[] lengthPrefix = BitConverter.GetBytes(data.Length + 1); // +1 for message type
                byte messageTypeId = GetMessageTypeId(message);
                
                // 전송
                await _stream.WriteAsync(lengthPrefix, 0, 4);
                await _stream.WriteAsync(new byte[] { messageTypeId }, 0, 1);
                await _stream.WriteAsync(data, 0, data.Length);
                await _stream.FlushAsync();
                
                LogDebug($"TCP 메시지 전송 완료: {message.MessageType} ({data.Length} bytes)");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"TCP 메시지 전송 실패: {ex.Message}");
                
                // 전송 실패시 연결 상태 확인
                await HandleConnectionFailure();
                return false;
            }
        }
        
        // Private Methods
        
        /// <summary>
        /// 수신 루프
        /// </summary>
        private async UniTaskVoid ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                LogDebug("TCP 수신 루프 시작");
                
                while (!cancellationToken.IsCancellationRequested && IsConnected)
                {
                    try
                    {
                        // 길이 헤더 읽기 (4바이트)
                        byte[] lengthBuffer = new byte[4];
                        int bytesRead = await ReadExactAsync(lengthBuffer, 4, cancellationToken);
                        
                        if (bytesRead != 4)
                        {
                            LogDebug("길이 헤더 읽기 실패 - 연결 종료");
                            break;
                        }
                        
                        int messageLength = BitConverter.ToInt32(lengthBuffer, 0);
                        
                        if (messageLength <= 0 || messageLength > 1024 * 1024) // 1MB 제한
                        {
                            LogError($"잘못된 메시지 길이: {messageLength}");
                            break;
                        }
                        
                        // 메시지 타입 읽기 (1바이트)
                        byte[] typeBuffer = new byte[1];
                        bytesRead = await ReadExactAsync(typeBuffer, 1, cancellationToken);
                        
                        if (bytesRead != 1)
                        {
                            LogError("메시지 타입 읽기 실패");
                            break;
                        }
                        
                        byte messageTypeId = typeBuffer[0];
                        
                        // 메시지 데이터 읽기
                        byte[] messageBuffer = new byte[messageLength - 1]; // -1 for message type
                        bytesRead = await ReadExactAsync(messageBuffer, messageLength - 1, cancellationToken);
                        
                        if (bytesRead != messageLength - 1)
                        {
                            LogError("메시지 데이터 읽기 실패");
                            break;
                        }
                        
                        // 메시지 처리
                        HandleTcpMessage(messageTypeId, messageBuffer);
                    }
                    catch (OperationCanceledException)
                    {
                        LogDebug("수신 루프 취소됨");
                        break;
                    }
                    catch (Exception ex)
                    {
                        LogError($"메시지 수신 중 오류: {ex.Message}");
                        
                        // 일시적 오류인지 확인
                        if (IsConnectionError(ex))
                        {
                            LogDebug("연결 오류 감지 - 수신 루프 종료");
                            break;
                        }
                        
                        // 일시적 오류면 계속 시도
                        await UniTask.Delay(1000, cancellationToken: cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"수신 루프 치명적 오류: {ex.Message}");
            }
            finally
            {
                LogDebug("TCP 수신 루프 종료");
                
                // 의도적 종료가 아니면 재연결 시도
                if (!_intentionalDisconnect && !cancellationToken.IsCancellationRequested)
                {
                    await HandleConnectionFailure();
                }
            }
        }
        
        /// <summary>
        /// 정확한 바이트 수만큼 읽기
        /// </summary>
        private async UniTask<int> ReadExactAsync(byte[] buffer, int count, CancellationToken cancellationToken)
        {
            int totalRead = 0;
            
            while (totalRead < count && !cancellationToken.IsCancellationRequested)
            {
                int bytesRead = await _stream.ReadAsync(buffer, totalRead, count - totalRead, cancellationToken);
                
                if (bytesRead == 0)
                {
                    // 연결 종료됨
                    break;
                }
                
                totalRead += bytesRead;
            }
            
            return totalRead;
        }
        
        /// <summary>
        /// TCP 메시지 처리
        /// </summary>
        private void HandleTcpMessage(byte messageTypeId, byte[] messageData)
        {
            try
            {
                var message = CreateMessageFromTypeId(messageTypeId);
                if (message == null)
                {
                    LogError($"알 수 없는 메시지 타입 ID: {messageTypeId}");
                    return;
                }
                
                message.Deserialize(messageData);
                
                LogDebug($"TCP 메시지 수신: {message.MessageType}");
                
                // 인증 메시지 특별 처리
                if (message is AuthMessage authMessage)
                {
                    HandleAuthResponse(authMessage);
                }
                
                // 상위 레이어에 전달
                OnMessageReceived?.Invoke(message);
            }
            catch (Exception ex)
            {
                LogError($"TCP 메시지 처리 중 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 인증 응답 처리
        /// </summary>
        private void HandleAuthResponse(AuthMessage authMessage)
        {
            if (authMessage.ResultCode == 0) // 성공
            {
                _isAuthenticated = true;
                OnAuthenticationChanged?.Invoke(true);
                LogDebug("TCP 인증 성공");
            }
            else
            {
                _isAuthenticated = false;
                OnAuthenticationChanged?.Invoke(false);
                LogError($"TCP 인증 실패: {authMessage.ResultMessage}");
            }
        }
        
        /// <summary>
        /// 메시지 타입 ID 반환
        /// </summary>
        private byte GetMessageTypeId(INetworkMessage message)
        {
            return message switch
            {
                AuthMessage => 1,
                PingMessage => 2,
                ChatMessage => 3,
                _ => 0
            };
        }
        
        /// <summary>
        /// 타입 ID로부터 메시지 인스턴스 생성
        /// </summary>
        private INetworkMessage CreateMessageFromTypeId(byte typeId)
        {
            return typeId switch
            {
                1 => new AuthMessage(),
                2 => new PingMessage(),
                3 => new ChatMessage(),
                _ => null
            };
        }
        
        /// <summary>
        /// 연결 오류인지 확인
        /// </summary>
        private bool IsConnectionError(Exception ex)
        {
            return ex is SocketException ||
                   ex is System.IO.IOException ||
                   ex is ObjectDisposedException ||
                   (ex.InnerException != null && IsConnectionError(ex.InnerException));
        }
        
        /// <summary>
        /// 연결 실패 처리
        /// </summary>
        private async UniTask HandleConnectionFailure()
        {
            if (_intentionalDisconnect || _isReconnecting) return;
            
            _isConnected = false;
            _isAuthenticated = false;
            OnConnectionChanged?.Invoke(false);
            OnAuthenticationChanged?.Invoke(false);
            
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
                    
                    bool result = await ConnectAsync(config?.serverAddress ?? "127.0.0.1", config?.serverPort ?? 7777);
                    
                    if (result)
                    {
                        _isReconnecting = false;
                        LogDebug("재연결 성공");
                        
                        // 자동 재인증 시도
                        if (config?.autoAuthenticate == true)
                        {
                            var authMessage = new AuthMessage
                            {
                                AuthType = AuthType.TokenAuth,
                                AuthKey = config.defaultAuthKey,
                                PlatformType = config.defaultPlatformType,
                                UserId = config.defaultAuthId
                            };
                            
                            await SendMessageAsync(authMessage);
                        }
                        
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
                if (_stream != null)
                {
                    _stream.Close();
                    _stream.Dispose();
                    _stream = null;
                }
                
                if (_client != null)
                {
                    _client.Close();
                    _client.Dispose();
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
                Debug.Log($"[TcpTransport] {message}");
            }
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[TcpTransport] {message}");
        }
    }
}