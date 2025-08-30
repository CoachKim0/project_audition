using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Grpc.Net.Client;
using GrpcApp;
using Cysharp.Net.Http;

/// <summary>
/// OnGUI 방식으로 직접 gRPC 인증 테스트를 위한 클래스
/// protobuf 메시지를 사용한 회원가입/로그인 패킷 처리 연습용
/// </summary>
public class NetTest : MonoBehaviour
{
    [Header("서버 설정")]
    public string serverAddress = "127.0.0.1";
    public int serverPort = 5554;
    
    [Header("UI 상태")]
    private string _username = "testuser";
    private string _password = "password123";
    private string _email = "test@example.com";
    private string _nickname = "테스터";
    
    private string _statusMessage = "준비";
    private string _lastResponse = "";
    private Vector2 _scrollPosition;
    
    private bool _isProcessing = false;
    
    // gRPC 클라이언트
    private GrpcChannel _channel;
    private GameService.GameServiceClient _client;
    private bool _isConnected = false;
    private bool _isAuthenticated = false;
    
    // GUI 스타일
    private GUIStyle _headerStyle;
    private GUIStyle _buttonStyle;
    private GUIStyle _statusStyle;
    private GUIStyle _responseStyle;
    
    void Start()
    {
        // HTTP/2 환경 설정
        System.AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        
        _statusMessage = "YetAnotherHttpHandler gRPC 클라이언트 준비 완료";
        AddToLog("=== YetAnotherHttpHandler HTTP/2 gRPC 테스트 시작 ===");
        AddToLog("HTTP/2 Only 모드로 강제 설정됨");
    }
    
    void OnDestroy()
    {
        DisconnectFromServer().Forget();
    }
    
    void OnGUI()
    {
        // GUI 스타일 초기화
        InitializeGUIStyles();
        
        // 메인 창 설정
        GUILayout.BeginArea(new Rect(10, 10, Screen.width - 20, Screen.height - 20));
        
        // 제목
        GUILayout.Label("🌐 네트워크 인증 테스트", _headerStyle);
        GUILayout.Space(10);
        
        // 서버 연결 섹션
        DrawServerConnectionSection();
        GUILayout.Space(10);
        
        // 인증 정보 입력 섹션
        DrawAuthInputSection();
        GUILayout.Space(10);
        
        // 인증 버튼 섹션
        DrawAuthButtonSection();
        GUILayout.Space(10);
        
        // 상태 및 응답 섹션
        DrawStatusSection();
        
        GUILayout.EndArea();
    }
    
    private void InitializeGUIStyles()
    {
        if (_headerStyle == null)
        {
            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
        }
        
        if (_buttonStyle == null)
        {
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fixedHeight = 30
            };
        }
        
        if (_statusStyle == null)
        {
            _statusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.cyan }
            };
        }
        
        if (_responseStyle == null)
        {
            _responseStyle = new GUIStyle(GUI.skin.textArea)
            {
                fontSize = 11,
                wordWrap = true
            };
        }
    }
    
    private void DrawServerConnectionSection()
    {
        GUILayout.Label("📡 서버 연결", GUI.skin.box);
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("주소:", GUILayout.Width(50));
        serverAddress = GUILayout.TextField(serverAddress, GUILayout.Width(120));
        GUILayout.Label("포트:", GUILayout.Width(40));
        string portStr = GUILayout.TextField(serverPort.ToString(), GUILayout.Width(60));
        if (int.TryParse(portStr, out int port))
            serverPort = port;
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        
        GUI.enabled = !_isProcessing && !_isConnected;
        if (GUILayout.Button("🔌 연결", _buttonStyle))
        {
            ConnectToServer().Forget();
        }
        
        GUI.enabled = !_isProcessing && _isConnected;
        if (GUILayout.Button("🔌 연결해제", _buttonStyle))
        {
            DisconnectFromServer().Forget();
        }
        
        GUI.enabled = true;
        GUILayout.EndHorizontal();
        
        // 연결 상태 표시
        string connectionStatus = _isConnected ? "✅ 연결됨" : "❌ 연결 안됨";
        string authStatus = _isAuthenticated ? "✅ 인증됨" : "❌ 인증 안됨";
        GUILayout.Label($"상태: {connectionStatus} | {authStatus}", _statusStyle);
    }
    
    private void DrawAuthInputSection()
    {
        GUILayout.Label("👤 인증 정보", GUI.skin.box);
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("사용자명:", GUILayout.Width(70));
        _username = GUILayout.TextField(_username);
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("비밀번호:", GUILayout.Width(70));
        _password = GUILayout.PasswordField(_password, '*');
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("이메일:", GUILayout.Width(70));
        _email = GUILayout.TextField(_email);
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("닉네임:", GUILayout.Width(70));
        _nickname = GUILayout.TextField(_nickname);
        GUILayout.EndHorizontal();
    }
    
    private void DrawAuthButtonSection()
    {
        GUILayout.Label("🔐 인증 작업", GUI.skin.box);
        
        GUILayout.BeginHorizontal();
        
        GUI.enabled = !_isProcessing && _isConnected;
        if (GUILayout.Button("📝 회원가입", _buttonStyle))
        {
            RegisterUser().Forget();
        }
        
        if (GUILayout.Button("🔑 로그인", _buttonStyle))
        {
            LoginUser().Forget();
        }
        
        GUI.enabled = !_isProcessing && _isAuthenticated;
        if (GUILayout.Button("🚪 로그아웃", _buttonStyle))
        {
            LogoutUser().Forget();
        }
        
        GUI.enabled = true;
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        GUI.enabled = !_isProcessing;
        if (GUILayout.Button("🧪 빠른 테스트", _buttonStyle))
        {
            QuickTest().Forget();
        }
        
        if (GUILayout.Button("📋 연결 정보", _buttonStyle))
        {
            ShowConnectionInfo();
        }
        
        if (GUILayout.Button("🗑️ 로그 지우기", _buttonStyle))
        {
            _lastResponse = "";
        }
        GUI.enabled = true;
        GUILayout.EndHorizontal();
    }
    
    private void DrawStatusSection()
    {
        GUILayout.Label("📊 상태 & 응답", GUI.skin.box);
        
        GUILayout.Label($"현재 상태: {_statusMessage}", _statusStyle);
        
        if (!string.IsNullOrEmpty(_lastResponse))
        {
            GUILayout.Label("마지막 응답:");
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(200));
            GUILayout.TextArea(_lastResponse, _responseStyle);
            GUILayout.EndScrollView();
        }
    }
    
    // gRPC 네트워크 작업 메서드들
    
    private async UniTask ConnectToServer()
    {
        _isProcessing = true;
        _statusMessage = "gRPC 서버 연결 중...";
        
        try
        {
            // 기존 연결이 있으면 해제
            if (_channel != null)
            {
                await _channel.ShutdownAsync();
                _channel.Dispose();
            }
            
            // YetAnotherHttpHandler를 사용한 HTTP/2 gRPC 채널 생성
            string address = $"http://{serverAddress}:{serverPort}";
            
            // HTTP 핸들러 설정
            var handler = new YetAnotherHttpHandler
            {
                Http2Only = true // HTTP/2만 사용
            };
            
            var httpClient = new System.Net.Http.HttpClient(handler);
            
            _channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions
            {
                HttpClient = httpClient,
                MaxReceiveMessageSize = 4 * 1024 * 1024, // 4MB
                MaxSendMessageSize = 4 * 1024 * 1024,
                DisposeHttpClient = true
            });
            _client = new GameService.GameServiceClient(_channel);
            
            // 연결 테스트용 간단한 호출
            AddToLog($"gRPC 채널 생성: {address}");
            
            _isConnected = true;
            _statusMessage = "✅ gRPC 서버 연결 성공";
            AddToLog($"gRPC 서버 연결 성공: {address}");
        }
        catch (Exception ex)
        {
            _statusMessage = "❌ gRPC 연결 중 오류 발생";
            AddToLog($"gRPC 연결 오류: {ex.Message}");
            _isConnected = false;
        }
        finally
        {
            _isProcessing = false;
        }
    }
    
    private async UniTask DisconnectFromServer()
    {
        _isProcessing = true;
        _statusMessage = "gRPC 연결 해제 중...";
        
        try
        {
            if (_channel != null)
            {
                await _channel.ShutdownAsync();
                _channel.Dispose();
                _channel = null;
                _client = null;
            }
            
            _isConnected = false;
            _isAuthenticated = false;
            _statusMessage = "✅ gRPC 연결 해제 완료";
            AddToLog("gRPC 서버 연결 해제됨");
        }
        catch (Exception ex)
        {
            _statusMessage = "❌ gRPC 연결 해제 중 오류";
            AddToLog($"gRPC 연결 해제 오류: {ex.Message}");
        }
        finally
        {
            _isProcessing = false;
        }
    }
    
    private async UniTask RegisterUser()
    {
        if (_client == null)
        {
            AddToLog("❌ gRPC 클라이언트가 연결되지 않았습니다");
            return;
        }
        
        _isProcessing = true;
        _statusMessage = "gRPC 회원가입 처리 중...";
        
        try
        {
            // protobuf GameMessage 생성
            var gameMessage = new GameMessage
            {
                UserId = _username,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                AuthUser = new AuthUser
                {
                    PlatformType = 2, // 회원가입
                    AuthKey = _username,
                    RetPassKey = _password
                }
            };
            
            AddToLog($"📝 gRPC 회원가입 요청: {_username}");
            AddToLog($"   PlatformType: {gameMessage.AuthUser.PlatformType}");
            AddToLog($"   AuthKey: {gameMessage.AuthUser.AuthKey}");
            
            // gRPC 스트리밍 호출
            using var call = _client.Game();
            await call.RequestStream.WriteAsync(gameMessage);
            await call.RequestStream.CompleteAsync();
            
            // 응답 대기
            var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
            
            if (await call.ResponseStream.MoveNext(cts.Token))
            {
                var response = call.ResponseStream.Current;
                AddToLog($"📨 gRPC 회원가입 응답 수신:");
                AddToLog($"   ResultCode: {response.ResultCode}");
                AddToLog($"   ResultMessage: {response.ResultMessage}");
                
                if (response.ResultCode == (int)ResultCode.Success)
                {
                    _statusMessage = "✅ 회원가입 성공";
                    AddToLog($"   토큰: {response.Token?[..Math.Min(response.Token?.Length ?? 0, 20)]}...");
                }
                else
                {
                    _statusMessage = "❌ 회원가입 실패";
                }
            }
            else
            {
                _statusMessage = "❌ 회원가입 응답 시간초과";
                AddToLog("❌ gRPC 응답을 받지 못했습니다 (10초 시간초과)");
            }
        }
        catch (Exception ex)
        {
            _statusMessage = "❌ gRPC 회원가입 중 오류";
            AddToLog($"gRPC 회원가입 오류: {ex.Message}");
        }
        finally
        {
            _isProcessing = false;
        }
    }
    
    private async UniTask LoginUser()
    {
        if (_client == null)
        {
            AddToLog("❌ gRPC 클라이언트가 연결되지 않았습니다");
            return;
        }
        
        _isProcessing = true;
        _statusMessage = "gRPC 로그인 처리 중...";
        
        try
        {
            // protobuf GameMessage 생성
            var gameMessage = new GameMessage
            {
                UserId = _username,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                AuthUser = new AuthUser
                {
                    PlatformType = 1, // 로그인
                    AuthKey = _username,
                    RetPassKey = _password
                }
            };
            
            AddToLog($"🔑 gRPC 로그인 요청: {_username}");
            AddToLog($"   PlatformType: {gameMessage.AuthUser.PlatformType}");
            AddToLog($"   AuthKey: {gameMessage.AuthUser.AuthKey}");
            
            // gRPC 스트리밍 호출
            using var call = _client.Game();
            await call.RequestStream.WriteAsync(gameMessage);
            await call.RequestStream.CompleteAsync();
            
            // 응답 대기
            var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
            
            if (await call.ResponseStream.MoveNext(cts.Token))
            {
                var response = call.ResponseStream.Current;
                AddToLog($"📨 gRPC 로그인 응답 수신:");
                AddToLog($"   ResultCode: {response.ResultCode}");
                AddToLog($"   ResultMessage: {response.ResultMessage}");
                
                if (response.ResultCode == (int)ResultCode.Success)
                {
                    _statusMessage = "✅ 로그인 성공";
                    _isAuthenticated = true;
                    AddToLog($"   토큰: {response.Token?[..Math.Min(response.Token?.Length ?? 0, 20)]}...");
                    AddToLog("🔐 인증 상태 변경: 인증됨");
                }
                else
                {
                    _statusMessage = "❌ 로그인 실패";
                    _isAuthenticated = false;
                }
            }
            else
            {
                _statusMessage = "❌ 로그인 응답 시간초과";
                AddToLog("❌ gRPC 응답을 받지 못했습니다 (10초 시간초과)");
            }
        }
        catch (Exception ex)
        {
            _statusMessage = "❌ gRPC 로그인 중 오류";
            AddToLog($"gRPC 로그인 오류: {ex.Message}");
        }
        finally
        {
            _isProcessing = false;
        }
    }
    
    private async UniTask LogoutUser()
    {
        _isProcessing = true;
        _statusMessage = "gRPC 로그아웃 처리 중...";
        
        try
        {
            _isAuthenticated = false;
            _statusMessage = "🚪 로그아웃 완료";
            AddToLog("🔐 인증 상태 변경: 인증 해제됨");
            AddToLog("로그아웃 처리됨 (로컬 세션 클리어)");
        }
        catch (Exception ex)
        {
            _statusMessage = "❌ 로그아웃 중 오류";
            AddToLog($"로그아웃 오류: {ex.Message}");
        }
        finally
        {
            _isProcessing = false;
        }
    }
    
    private async UniTask QuickTest()
    {
        _statusMessage = "gRPC 빠른 테스트 실행 중...";
        AddToLog("=== gRPC 빠른 테스트 시작 ===");
        
        // 1. 연결
        if (!_isConnected)
        {
            await ConnectToServer();
            await UniTask.Delay(1000);
        }
        
        if (!_isConnected)
        {
            AddToLog("❌ gRPC 연결 실패로 테스트 중단");
            return;
        }
        
        // 2. 회원가입 시도
        await RegisterUser();
        await UniTask.Delay(2000);
        
        // 3. 로그인 시도
        await LoginUser();
        await UniTask.Delay(2000);
        
        AddToLog("=== gRPC 빠른 테스트 완료 ===");
        _statusMessage = "✅ gRPC 빠른 테스트 완료";
    }
    
    private void ShowConnectionInfo()
    {
        AddToLog("=== gRPC 연결 정보 ===");
        AddToLog($"서버: {serverAddress}:{serverPort}");
        AddToLog($"연결 상태: {(_isConnected ? "연결됨" : "연결 안됨")}");
        AddToLog($"인증 상태: {(_isAuthenticated ? "인증됨" : "인증 안됨")}");
        AddToLog($"gRPC 채널: {(_channel != null ? "활성" : "비활성")}");
        AddToLog($"gRPC 클라이언트: {(_client != null ? "준비됨" : "준비 안됨")}");
    }
    
    // 유틸리티 메서드들
    
    private void AddToLog(string message)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        _lastResponse += $"[{timestamp}] {message}\n";
        
        // 로그가 너무 길어지면 자르기
        if (_lastResponse.Length > 5000)
        {
            _lastResponse = _lastResponse.Substring(_lastResponse.Length - 4000);
        }
    }
    
}