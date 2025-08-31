# gRPC 개발 메뉴얼

## 목차
1. [gRPC 개요](#grpc-개요)
2. [Proto 파일 구조](#proto-파일-구조)
3. [자동 생성되는 클래스들](#자동-생성되는-클래스들)
4. [클라이언트 구현](#클라이언트-구현)
5. [서버 구현](#서버-구현)
6. [실전 예제](#실전-예제)
7. [Unity에서 gRPC 사용법](#unity에서-grpc-사용법)
8. [트러블슈팅](#트러블슈팅)

---

## gRPC 개요

gRPC는 Google이 개발한 고성능 RPC(Remote Procedure Call) 프레임워크입니다.
- **Protocol Buffers**를 사용하여 데이터 직렬화
- HTTP/2 기반으로 빠른 통신
- 다양한 언어 지원 (C#, Java, Python, Go 등)
- **하나의 .proto 파일**에서 클라이언트와 서버 코드 자동 생성

### 장점
- 타입 안정성 보장
- 컴파일 타임에 API 검증
- 높은 성능과 효율성
- 스트리밍 지원

---

## Proto 파일 구조

### 기본 구조
```proto
syntax = "proto3";

// 네임스페이스 설정
option csharp_namespace = "MyProject.Protos";

// 패키지 선언
package myservice;

// 메시지 정의 (데이터 구조)
message LoginRequest {
  string username = 1;
  string password = 2;
}

message LoginResponse {
  bool success = 1;
  string message = 2;
  string token = 3;
}

// 서비스 정의 (API 엔드포인트)
service AuthService {
  rpc Login(LoginRequest) returns (LoginResponse);
  rpc Register(RegisterRequest) returns (RegisterResponse);
  rpc GetUserInfo(GetUserInfoRequest) returns (GetUserInfoResponse);
}
```

### 데이터 타입 매핑
| Proto 타입 | C# 타입 |
|-----------|--------|
| string | string |
| bool | bool |
| int32 | int |
| int64 | long |
| float | float |
| double | double |
| bytes | ByteString |
| repeated | List&lt;T&gt; |

### 필드 번호 규칙
```proto
message UserInfo {
  string name = 1;      // 필드 번호는 1부터 시작
  int32 age = 2;        // 순차적으로 증가
  bool active = 3;      // 한 번 사용한 번호는 재사용 금지
  // string old_field = 4; // 삭제된 필드는 reserved로 표시
  reserved 4;
}
```

---

## 자동 생성되는 클래스들

### 1. Message 클래스
```proto
message LoginRequest {
  string username = 1;
  string password = 2;
}
```

**생성되는 C# 코드:**
```csharp
public sealed partial class LoginRequest : pb::IMessage<LoginRequest> 
{
    // 속성
    public string Username { get; set; }
    public string Password { get; set; }
    
    // 생성자
    public LoginRequest() { }
    public LoginRequest(LoginRequest other) { }
    
    // 직렬화/역직렬화
    public void WriteTo(pb::CodedOutputStream output) { }
    public static LoginRequest Parser { get; }
    
    // 기타 유틸리티 메서드
    public override string ToString() { }
    public bool Equals(LoginRequest other) { }
    public override int GetHashCode() { }
}
```

### 2. Service 클래스들

하나의 `service` 정의에서 **3개의 주요 클래스**가 생성됩니다:

```proto
service AuthService {
  rpc Login(LoginRequest) returns (LoginResponse);
}
```

#### A. Static Service Class
```csharp
public static partial class AuthService
{
    // 메서드 정의
    static readonly Method<LoginRequest, LoginResponse> __Method_Login = ...;
    
    // 서비스 디스크립터
    public static ServiceDescriptor Descriptor { get; }
}
```

#### B. Client Class (클라이언트용)
```csharp
public partial class AuthServiceClient : ClientBase<AuthServiceClient>
{
    // 생성자
    public AuthServiceClient(ChannelBase channel) : base(channel) { }
    
    // 동기 메서드
    public virtual LoginResponse Login(LoginRequest request, 
        Metadata headers = null, 
        DateTime? deadline = null, 
        CancellationToken cancellationToken = default)
    {
        return Login(request, new CallOptions(headers, deadline, cancellationToken));
    }
    
    // 비동기 메서드 ⭐ 이것이 LoginAsync!
    public virtual AsyncUnaryCall<LoginResponse> LoginAsync(LoginRequest request,
        Metadata headers = null,
        DateTime? deadline = null, 
        CancellationToken cancellationToken = default)
    {
        return LoginAsync(request, new CallOptions(headers, deadline, cancellationToken));
    }
}
```

#### C. Server Base Class (서버용)
```csharp
public abstract partial class AuthServiceBase
{
    // 서버에서 구현해야 할 가상 메서드
    public virtual Task<LoginResponse> Login(LoginRequest request, 
        ServerCallContext context)
    {
        throw new RpcException(new Status(StatusCode.Unimplemented, ""));
    }
}
```

---

## 클라이언트 구현

### 1. 기본 클라이언트 설정
```csharp
using Grpc.Core;
using MyProject.Protos;

public class GrpcClientManager
{
    private Channel _channel;
    private AuthService.AuthServiceClient _authClient;
    
    public async Task InitializeAsync()
    {
        // 채널 생성
        _channel = new Channel("localhost:50051", ChannelCredentials.Insecure);
        
        // 클라이언트 생성
        _authClient = new AuthService.AuthServiceClient(_channel);
        
        // 연결 대기
        await _channel.ConnectAsync(DateTime.UtcNow.AddSeconds(5));
    }
    
    public async Task<bool> LoginAsync(string username, string password)
    {
        try 
        {
            // 요청 객체 생성
            var request = new LoginRequest
            {
                Username = username,
                Password = password
            };
            
            // 비동기 호출 ⭐
            var response = await _authClient.LoginAsync(request);
            
            return response.Success;
        }
        catch (RpcException ex)
        {
            Debug.LogError($"gRPC Error: {ex.Status}");
            return false;
        }
    }
    
    public async Task CleanupAsync()
    {
        await _channel?.ShutdownAsync();
    }
}
```

### 2. Unity에서 사용 예제
```csharp
using UnityEngine;
using System.Threading.Tasks;

public class LoginManager : MonoBehaviour
{
    private GrpcClientManager _grpcClient;
    
    private async void Start()
    {
        _grpcClient = new GrpcClientManager();
        await _grpcClient.InitializeAsync();
    }
    
    public async void OnLoginButtonClick()
    {
        string username = usernameInput.text;
        string password = passwordInput.text;
        
        bool success = await _grpcClient.LoginAsync(username, password);
        
        if (success)
        {
            Debug.Log("로그인 성공!");
            // 게임 씬으로 이동
        }
        else
        {
            Debug.Log("로그인 실패!");
            // 에러 메시지 표시
        }
    }
    
    private async void OnDestroy()
    {
        await _grpcClient?.CleanupAsync();
    }
}
```

---

## 서버 구현

### 1. 서비스 구현 클래스
```csharp
using Grpc.Core;
using MyProject.Protos;

public class AuthServiceImpl : AuthService.AuthServiceBase
{
    // Login RPC 구현
    public override async Task<LoginResponse> Login(LoginRequest request, 
        ServerCallContext context)
    {
        // 비즈니스 로직
        bool isValid = await ValidateUser(request.Username, request.Password);
        
        if (isValid)
        {
            string token = GenerateJwtToken(request.Username);
            
            return new LoginResponse
            {
                Success = true,
                Message = "로그인 성공",
                Token = token
            };
        }
        else
        {
            return new LoginResponse
            {
                Success = false,
                Message = "잘못된 사용자명 또는 비밀번호"
            };
        }
    }
    
    // Register RPC 구현
    public override async Task<RegisterResponse> Register(RegisterRequest request,
        ServerCallContext context)
    {
        // 회원가입 로직 구현
        // ...
    }
    
    private async Task<bool> ValidateUser(string username, string password)
    {
        // 데이터베이스에서 사용자 검증
        // ...
    }
    
    private string GenerateJwtToken(string username)
    {
        // JWT 토큰 생성
        // ...
    }
}
```

### 2. 서버 시작
```csharp
class Program
{
    static async Task Main(string[] args)
    {
        const int Port = 50051;
        
        // 서버 생성
        var server = new Server
        {
            Services = { AuthService.BindService(new AuthServiceImpl()) },
            Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) }
        };
        
        // 서버 시작
        server.Start();
        Console.WriteLine($"gRPC 서버가 포트 {Port}에서 시작되었습니다.");
        
        // 종료 대기
        Console.WriteLine("서버를 종료하려면 아무 키나 누르세요...");
        Console.ReadKey();
        
        // 서버 종료
        await server.ShutdownAsync();
    }
}
```

---

## 실전 예제

### 1. 게임 인증 서비스

**auth_service.proto:**
```proto
syntax = "proto3";

option csharp_namespace = "GameServer.Auth";
package auth;

message LoginRequest {
  string username = 1;
  string password = 2;
  string device_id = 3;
}

message LoginResponse {
  bool success = 1;
  string message = 2;
  string access_token = 3;
  string refresh_token = 4;
  UserInfo user_info = 5;
}

message UserInfo {
  int64 user_id = 1;
  string username = 2;
  string email = 3;
  int32 level = 4;
  repeated Achievement achievements = 5;
}

message Achievement {
  int32 id = 1;
  string name = 2;
  string description = 3;
  bool unlocked = 4;
}

service AuthService {
  rpc Login(LoginRequest) returns (LoginResponse);
  rpc RefreshToken(RefreshTokenRequest) returns (RefreshTokenResponse);
  rpc Logout(LogoutRequest) returns (LogoutResponse);
}
```

**생성되는 주요 메서드들:**
```csharp
// 클라이언트에서 사용
var authClient = new AuthService.AuthServiceClient(channel);

// 1. 로그인 (동기)
LoginResponse response = authClient.Login(loginRequest);

// 2. 로그인 (비동기) ⭐ 주로 이걸 사용
LoginResponse response = await authClient.LoginAsync(loginRequest);

// 3. 토큰 갱신
RefreshTokenResponse refreshResponse = await authClient.RefreshTokenAsync(refreshRequest);
```

### 2. 실시간 채팅 시스템

**chat_service.proto:**
```proto
syntax = "proto3";

option csharp_namespace = "GameServer.Chat";
package chat;

message ChatMessage {
  string message_id = 1;
  string sender_id = 2;
  string sender_name = 3;
  string content = 4;
  int64 timestamp = 5;
  MessageType type = 6;
}

enum MessageType {
  TEXT = 0;
  EMOJI = 1;
  SYSTEM = 2;
}

message SendMessageRequest {
  string room_id = 1;
  string content = 2;
  MessageType type = 3;
}

message SendMessageResponse {
  bool success = 1;
  string message = 2;
  ChatMessage chat_message = 3;
}

service ChatService {
  // 단방향 스트리밍: 서버에서 클라이언트로 실시간 메시지 전송
  rpc SubscribeToChat(SubscribeRequest) returns (stream ChatMessage);
  
  // 일반 RPC: 메시지 전송
  rpc SendMessage(SendMessageRequest) returns (SendMessageResponse);
}
```

**스트리밍 사용 예제:**
```csharp
// 클라이언트에서 실시간 채팅 구독
public async Task SubscribeToChatAsync(string roomId)
{
    var request = new SubscribeRequest { RoomId = roomId };
    
    // 스트리밍 호출
    using var call = _chatClient.SubscribeToChat(request);
    
    // 실시간으로 메시지 수신
    await foreach (var message in call.ResponseStream.ReadAllAsync())
    {
        Debug.Log($"[{message.SenderName}]: {message.Content}");
        // UI에 메시지 표시
        DisplayMessage(message);
    }
}
```

---

## Unity에서 gRPC 사용법

### 1. 패키지 설치
```json
// Packages/manifest.json
{
  "dependencies": {
    "com.google.protobuf": "3.21.9",
    "com.grpc": "2.46.3"
  }
}
```

### 2. Unity용 gRPC 매니저
```csharp
using UnityEngine;
using Grpc.Core;
using System.Threading.Tasks;
using GameServer.Auth;

public class UnityGrpcManager : MonoBehaviour
{
    [Header("서버 설정")]
    public string serverHost = "localhost";
    public int serverPort = 50051;
    
    private Channel _channel;
    private AuthService.AuthServiceClient _authClient;
    
    // 싱글톤 패턴
    public static UnityGrpcManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGrpc();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private async void InitializeGrpc()
    {
        try
        {
            // 채널 생성
            _channel = new Channel($"{serverHost}:{serverPort}", ChannelCredentials.Insecure);
            
            // 클라이언트 생성
            _authClient = new AuthService.AuthServiceClient(_channel);
            
            // 연결 테스트
            await _channel.ConnectAsync(System.DateTime.UtcNow.AddSeconds(5));
            Debug.Log("gRPC 연결 성공!");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"gRPC 초기화 실패: {ex.Message}");
        }
    }
    
    public async Task<LoginResponse> LoginAsync(string username, string password)
    {
        var request = new LoginRequest
        {
            Username = username,
            Password = password,
            DeviceId = SystemInfo.deviceUniqueIdentifier
        };
        
        return await _authClient.LoginAsync(request);
    }
    
    private async void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // 앱이 백그라운드로 갈 때 연결 유지
        }
    }
    
    private async void OnDestroy()
    {
        if (_channel != null)
        {
            await _channel.ShutdownAsync();
        }
    }
}
```

### 3. UI 연동 예제
```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoginUI : MonoBehaviour
{
    [Header("UI 요소")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public Button loginButton;
    public TextMeshProUGUI statusText;
    
    private void Start()
    {
        loginButton.onClick.AddListener(OnLoginClick);
    }
    
    private async void OnLoginClick()
    {
        loginButton.interactable = false;
        statusText.text = "로그인 중...";
        
        try
        {
            var response = await UnityGrpcManager.Instance.LoginAsync(
                usernameInput.text, 
                passwordInput.text
            );
            
            if (response.Success)
            {
                statusText.text = "로그인 성공!";
                // 게임 씬으로 이동
                UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
            }
            else
            {
                statusText.text = response.Message;
            }
        }
        catch (System.Exception ex)
        {
            statusText.text = $"오류: {ex.Message}";
        }
        finally
        {
            loginButton.interactable = true;
        }
    }
}
```

---

## 트러블슈팅

### 1. 자주 발생하는 오류들

#### RpcException: Status(StatusCode="Unavailable")
```csharp
// 해결방법: 연결 재시도 로직 추가
private async Task<bool> EnsureConnectionAsync()
{
    try
    {
        if (_channel.State == ChannelState.Shutdown)
        {
            _channel = new Channel($"{_host}:{_port}", ChannelCredentials.Insecure);
            _authClient = new AuthService.AuthServiceClient(_channel);
        }
        
        await _channel.ConnectAsync(DateTime.UtcNow.AddSeconds(5));
        return true;
    }
    catch
    {
        return false;
    }
}
```

#### Unity에서 Async/Await 사용 시 주의사항
```csharp
// ❌ 잘못된 방법: UI 스레드 블로킹
void OnButtonClick()
{
    var result = LoginAsync(username, password).Result; // 데드락 위험!
}

// ✅ 올바른 방법: async void 사용
async void OnButtonClick()
{
    try
    {
        var result = await LoginAsync(username, password);
        // UI 업데이트
    }
    catch (System.Exception ex)
    {
        Debug.LogError(ex);
    }
}
```

### 2. 성능 최적화

#### 연결 풀링
```csharp
public class GrpcConnectionPool
{
    private static readonly Dictionary<string, Channel> _channels = new();
    
    public static Channel GetChannel(string address)
    {
        if (!_channels.ContainsKey(address))
        {
            _channels[address] = new Channel(address, ChannelCredentials.Insecure);
        }
        
        return _channels[address];
    }
}
```

#### 메시지 재사용
```csharp
public class MessagePool<T> where T : new()
{
    private readonly Stack<T> _pool = new Stack<T>();
    
    public T Get()
    {
        return _pool.Count > 0 ? _pool.Pop() : new T();
    }
    
    public void Return(T item)
    {
        // 필드 초기화
        _pool.Push(item);
    }
}
```

### 3. 디버깅 팁

#### 로깅 설정
```csharp
// gRPC 내부 로그 활성화
Environment.SetEnvironmentVariable("GRPC_VERBOSITY", "DEBUG");
Environment.SetEnvironmentVariable("GRPC_TRACE", "all");
```

#### 메타데이터 활용
```csharp
// 요청에 메타데이터 추가
var headers = new Metadata
{
    { "user-id", "12345" },
    { "session-id", sessionId }
};

var response = await _client.LoginAsync(request, headers);
```

---

## 마무리

이 메뉴얼을 통해 gRPC의 핵심 개념부터 Unity에서의 실제 구현까지 이해할 수 있습니다.

### 핵심 포인트 요약:
1. **Proto 파일 하나**로 클라이언트/서버 코드 자동 생성
2. **Service**에서 `ServiceClient`와 `ServiceBase` 클래스 생성
3. **Async 메서드**가 자동으로 생성되어 비동기 호출 가능
4. **타입 안전성**과 **높은 성능** 보장
5. **Unity**에서도 완벽하게 사용 가능

추가 질문이나 특정 상황에 대한 도움이 필요하면 언제든 문의하세요!