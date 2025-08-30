using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using NetworkManager.Core;
using NetworkManager.Messages;
using System.Collections.Generic;
using TransportType = NetworkManager.Core.TransportType;

namespace NetworkManager.Transports
{
    /// <summary>
    /// UDP 전송 계층 구현체
    /// </summary>
    public class UdpTransport : INetworkTransport
    {
        // UDP 관련 필드
        private UdpClient _client;
        private IPEndPoint _serverEndPoint;
        private CancellationTokenSource _connectionCts;
        
        // 상태 관리
        private bool _isConnected;
        private bool _isAuthenticated;
        private bool _intentionalDisconnect = false;
        private bool _isReconnecting = false;
        private int _reconnectAttempts = 0;
        
        // 패킷 순서 관리 (UDP는 신뢰성 없음)
        private int _sendSequence = 0;
        private Dictionary<int, DateTime> _sentPackets = new Dictionary<int, DateTime>();
        
        // 프로퍼티
        public bool IsConnected => _isConnected && _client != null;
        public bool IsAuthenticated => _isAuthenticated;
        public TransportType TransportType => TransportType.UDP;
        
        // 이벤트
        public event Action<bool> OnConnectionChanged;
        public event Action<bool> OnAuthenticationChanged;
        public event Action<INetworkMessage> OnMessageReceived;
        public event Action<string> OnError;
        
        public UdpTransport()
        {
            LogDebug("UdpTransport 생성됨");
        }
        
        ~UdpTransport()
        {
            DisconnectAsync().Forget();
        }
        
        /// <summary>
        /// 서버에 연결 (UDP는 연결이 없으므로 엔드포인트 설정)
        /// </summary>
        public async UniTask<bool> ConnectAsync(string host, int port)
        {
            try
            {
                LogDebug($"UDP 연결 시도: {host}:{port}");
                
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
                
                // UDP 클라이언트 생성
                _client = new UdpClient();
                
                // 서버 엔드포인트 설정
                var addresses = await Dns.GetHostAddressesAsync(host);
                if (addresses.Length == 0)
                {
                    throw new Exception($"호스트를 찾을 수 없습니다: {host}");
                }
                
                _serverEndPoint = new IPEndPoint(addresses[0], port);
                
                // UDP는 연결이 없으므로 바로 연결된 것으로 간주
                _isConnected = true;
                _intentionalDisconnect = false;
                OnConnectionChanged?.Invoke(true);
                
                // 수신 루프 시작
                ReceiveLoopAsync(_connectionCts.Token).Forget();
                
                // 연결 확인을 위한 핑 전송
                var pingMessage = new PingMessage(0);
                await SendMessageAsync(pingMessage);
                
                // 재연결 시도가 성공했으면 재연결 상태 초기화
                if (_isReconnecting)
                {
                    _isReconnecting = false;
                    _reconnectAttempts = 0;
                    LogDebug("재연결 성공");
                }
                
                LogDebug("UDP 연결 성공");
                return true;
            }
            catch (Exception ex)
            {
                _isConnected = false;
                var errorMsg = $"UDP 연결 실패: {ex.Message}";
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
                LogDebug("UDP 연결 해제 시작");
                
                _intentionalDisconnect = true;
                
                // 모든 작업 취소
                _connectionCts?.Cancel();
                
                // 연결 해제 메시지 전송 (가능한 경우)
                if (_isConnected && _client != null && _serverEndPoint != null)
                {
                    try
                    {
                        var disconnectMessage = new AuthMessage
                        {
                            AuthType = AuthType.Logout,
                            UserId = NetworkManager.Core.NetworkManager.Instance?.Config?.defaultAuthId ?? ""
                        };
                        
                        await SendMessageAsync(disconnectMessage);
                    }
                    catch
                    {
                        // 연결 해제 메시지 전송 실패는 무시
                    }
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
                _serverEndPoint = null;
                _sendSequence = 0;
                _sentPackets.Clear();
                
                OnConnectionChanged?.Invoke(false);
                OnAuthenticationChanged?.Invoke(false);
                
                LogDebug("UDP 연결 해제 완료");
                
                // 약간의 지연 (리소스 정리 시간)
                await UniTask.Delay(100);
            }
            catch (Exception ex)
            {
                LogError($"UDP 연결 해제 중 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 메시지 전송
        /// </summary>
        public async UniTask<bool> SendMessageAsync(INetworkMessage message)
        {
            if (!IsConnected || _client == null || _serverEndPoint == null)
            {
                LogError("UDP 연결되어 있지 않음");
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
                
                // UDP 패킷 헤더 생성
                var packet = CreateUdpPacket(message, data);
                
                // 전송
                int bytesSent = await _client.SendAsync(packet, packet.Length, _serverEndPoint);
                
                // 전송된 패킷 기록 (응답 확인용)
                _sentPackets[_sendSequence] = DateTime.UtcNow;
                
                // 오래된 패킷 기록 정리 (30초 이상)
                CleanupOldPackets();
                
                LogDebug($"UDP 메시지 전송 완료: {message.MessageType} ({bytesSent} bytes, seq: {_sendSequence})");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"UDP 메시지 전송 실패: {ex.Message}");
                
                // 전송 실패시 연결 상태 확인
                await HandleConnectionFailure();
                return false;
            }
        }
        
        // Private Methods
        
        /// <summary>
        /// UDP 패킷 생성
        /// </summary>
        private byte[] CreateUdpPacket(INetworkMessage message, byte[] messageData)
        {
            _sendSequence++;
            
            // UDP 헤더: 시퀀스(4) + 메시지타입(1) + 길이(4) + 데이터
            var header = new List<byte>();
            header.AddRange(BitConverter.GetBytes(_sendSequence)); // 4바이트
            header.Add(GetMessageTypeId(message)); // 1바이트
            header.AddRange(BitConverter.GetBytes(messageData.Length)); // 4바이트
            header.AddRange(messageData);
            
            return header.ToArray();
        }
        
        /// <summary>
        /// 수신 루프
        /// </summary>
        private async UniTaskVoid ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                LogDebug("UDP 수신 루프 시작");
                
                while (!cancellationToken.IsCancellationRequested && IsConnected)
                {
                    try
                    {
                        // UDP 패킷 수신 대기
                        var result = await _client.ReceiveAsync().AsUniTask();
                        
                        if (result.Buffer != null && result.Buffer.Length > 0)
                        {
                            HandleUdpPacket(result.Buffer, result.RemoteEndPoint);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        LogDebug("UDP 수신 루프 취소됨");
                        break;
                    }
                    catch (Exception ex)
                    {
                        LogError($"UDP 패킷 수신 중 오류: {ex.Message}");
                        
                        // UDP는 연결이 없으므로 패킷 손실을 일시적 오류로 간주
                        if (IsConnectionError(ex))
                        {
                            LogDebug("UDP 연결 오류 감지");
                            await HandleConnectionFailure();
                            break;
                        }
                        
                        // 일시적 오류면 계속 시도
                        await UniTask.Delay(1000, cancellationToken: cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"UDP 수신 루프 치명적 오류: {ex.Message}");
            }
            finally
            {
                LogDebug("UDP 수신 루프 종료");
                
                // 의도적 종료가 아니면 재연결 시도
                if (!_intentionalDisconnect && !cancellationToken.IsCancellationRequested)
                {
                    await HandleConnectionFailure();
                }
            }
        }
        
        /// <summary>
        /// UDP 패킷 처리
        /// </summary>
        private void HandleUdpPacket(byte[] packetData, IPEndPoint senderEndPoint)
        {
            try
            {
                if (packetData.Length < 9) // 최소 헤더 크기
                {
                    LogError($"UDP 패킷 크기가 너무 작음: {packetData.Length} bytes");
                    return;
                }
                
                // 헤더 파싱
                int sequence = BitConverter.ToInt32(packetData, 0);
                byte messageTypeId = packetData[4];
                int dataLength = BitConverter.ToInt32(packetData, 5);
                
                if (dataLength != packetData.Length - 9)
                {
                    LogError($"UDP 패킷 길이 불일치: 헤더={dataLength}, 실제={packetData.Length - 9}");
                    return;
                }
                
                // 메시지 데이터 추출
                byte[] messageData = new byte[dataLength];
                Array.Copy(packetData, 9, messageData, 0, dataLength);
                
                // 메시지 생성 및 처리
                var message = CreateMessageFromTypeId(messageTypeId);
                if (message == null)
                {
                    LogError($"알 수 없는 UDP 메시지 타입 ID: {messageTypeId}");
                    return;
                }
                
                message.Deserialize(messageData);
                
                LogDebug($"UDP 메시지 수신: {message.MessageType} (seq: {sequence})");
                
                // 인증 메시지 특별 처리
                if (message is AuthMessage authMessage)
                {
                    HandleAuthResponse(authMessage);
                }
                
                // Ping 응답 처리
                if (message is PingMessage pingMessage)
                {
                    // 연결 상태 확인용 - 별도 처리 없음
                }
                
                // 상위 레이어에 전달
                OnMessageReceived?.Invoke(message);
            }
            catch (Exception ex)
            {
                LogError($"UDP 패킷 처리 중 오류: {ex.Message}");
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
                LogDebug("UDP 인증 성공");
            }
            else
            {
                _isAuthenticated = false;
                OnAuthenticationChanged?.Invoke(false);
                LogError($"UDP 인증 실패: {authMessage.ResultMessage}");
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
                   ex is ObjectDisposedException ||
                   (ex.InnerException != null && IsConnectionError(ex.InnerException));
        }
        
        /// <summary>
        /// 오래된 패킷 기록 정리
        /// </summary>
        private void CleanupOldPackets()
        {
            var cutoffTime = DateTime.UtcNow.AddSeconds(-30);
            var keysToRemove = new List<int>();
            
            foreach (var kvp in _sentPackets)
            {
                if (kvp.Value < cutoffTime)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _sentPackets.Remove(key);
            }
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
                    
                    bool result = await ConnectAsync(config?.serverAddress ?? "127.0.0.1", config?.serverPort ?? 8888);
                    
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
                if (_client != null)
                {
                    _client.Close();
                    _client.Dispose();
                    _client = null;
                }
                
                _serverEndPoint = null;
                _sendSequence = 0;
                _sentPackets.Clear();
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
                Debug.Log($"[UdpTransport] {message}");
            }
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[UdpTransport] {message}");
        }
    }
}