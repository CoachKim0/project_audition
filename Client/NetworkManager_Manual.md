# NetworkManager 사용 매뉴얼

## 개요

NetworkManager는 Unity에서 멀티 프로토콜을 지원하는 통합 네트워크 관리 클래스입니다. gRPC, TCP, UDP 등 다양한 전송 방식을 지원하며, 런타임에 전송 방식을 변경할 수 있습니다.

## 기본 설정

### 1. NetworkConfig 설정

NetworkManager를 사용하기 전에 `Resources` 폴더에 `NetworkConfig` ScriptableObject를 생성해야 합니다.

```csharp
// NetworkConfig 설정 예시
serverAddress = "localhost"
serverPort = 50051
defaultTransportType = TransportType.gRPC
enableDebugLog = true
showNetworkTraffic = true
defaultAuthId = "user123"
```

### 2. 씬에 NetworkManager 추가

NetworkManager는 Singleton 패턴을 사용하므로, 씬에 GameObject를 생성하고 NetworkManager 컴포넌트를 추가하면 됩니다.

## 기본 사용법

### 연결 관리

#### 서버 연결

```csharp
// 기본 설정으로 연결
bool success = await NetworkManager.Instance.ConnectAsync();

// 특정 주소/포트로 연결
bool success = await NetworkManager.Instance.ConnectAsync("192.168.1.100", 8080);
```

#### 연결 해제

```csharp
await NetworkManager.Instance.DisconnectAsync();
```

#### 연결 상태 확인

```csharp
bool isConnected = NetworkManager.Instance.IsConnected;
bool isAuthenticated = NetworkManager.Instance.IsAuthenticated;
```

### 메시지 전송

```csharp
// INetworkMessage를 구현한 메시지 객체 생성
var message = new YourCustomMessage
{
    MessageType = "CUSTOM_MESSAGE",
    Data = "Hello Server!"
};

// 메시지 전송
bool success = await NetworkManager.Instance.SendMessageAsync(message);
```

### 전송 방식 변경

```csharp
// gRPC로 전송 방식 변경
bool success = await NetworkManager.Instance.SwitchTransportAsync(TransportType.gRPC);

// TCP로 전송 방식 변경
bool success = await NetworkManager.Instance.SwitchTransportAsync(TransportType.TCP);

// UDP로 전송 방식 변경
bool success = await NetworkManager.Instance.SwitchTransportAsync(TransportType.UDP);
```

### 세션 토큰 관리

```csharp
// 세션 토큰 설정 (인증 후 받은 토큰)
NetworkManager.Instance.SetSessionToken("your_session_token_here");

// 현재 세션 토큰 가져오기
string currentToken = NetworkManager.Instance.GetSessionToken();
```

## 이벤트 처리

NetworkManager는 다음 이벤트들을 제공합니다:

### 연결 상태 변경

```csharp
NetworkManager.Instance.OnConnectionChanged += (isConnected) =>
{
    if (isConnected)
    {
        Debug.Log("서버에 연결되었습니다.");
    }
    else
    {
        Debug.Log("서버와의 연결이 끊어졌습니다.");
    }
};
```

### 인증 상태 변경

```csharp
NetworkManager.Instance.OnAuthenticationChanged += (isAuthenticated) =>
{
    if (isAuthenticated)
    {
        Debug.Log("인증이 완료되었습니다.");
    }
    else
    {
        Debug.Log("인증이 해제되었습니다.");
    }
};
```

### 메시지 수신

```csharp
NetworkManager.Instance.OnMessageReceived += (message) =>
{
    Debug.Log($"메시지 수신: {message.MessageType}");
    
    // 메시지 타입에 따른 처리
    switch (message.MessageType)
    {
        case "CHAT_MESSAGE":
            HandleChatMessage(message);
            break;
        case "GAME_STATE":
            HandleGameState(message);
            break;
    }
};
```

### 에러 처리

```csharp
NetworkManager.Instance.OnError += (error) =>
{
    Debug.LogError($"네트워크 오류: {error}");
    // 에러에 따른 처리 로직
};
```

## 고급 사용법

### 연결 정보 확인

```csharp
string connectionInfo = NetworkManager.Instance.GetConnectionInfo();
Debug.Log(connectionInfo);
// 출력 예시:
// 전송방식: gRPC
// 서버: localhost:50051
// 연결: 연결됨
// 인증: 인증됨
// 세션토큰: 있음
// 마지막 오류: 
```

### 프로퍼티 접근

```csharp
// 현재 전송 방식 확인
TransportType currentType = NetworkManager.Instance.CurrentTransportType;

// 설정 정보 접근
NetworkConfig config = NetworkManager.Instance.Config;

// 마지막 에러 확인
string lastError = NetworkManager.Instance.LastError;
```

## 메시지 구조

NetworkManager에서 사용하는 메시지는 `INetworkMessage` 인터페이스를 구현해야 합니다:

```csharp
public interface INetworkMessage
{
    string MessageType { get; set; }
    string UserId { get; set; }
    long Timestamp { get; set; }
    string Token { get; set; }
    bool RequiresAuth { get; }
}
```

### 커스텀 메시지 예시

```csharp
public class ChatMessage : INetworkMessage
{
    public string MessageType { get; set; } = "CHAT";
    public string UserId { get; set; }
    public long Timestamp { get; set; }
    public string Token { get; set; }
    public bool RequiresAuth => true; // 인증이 필요한 메시지
    
    // 커스텀 데이터
    public string Content { get; set; }
    public string RoomId { get; set; }
}
```

## 디버깅

### 디버그 로그 활성화

NetworkConfig에서 다음 옵션들을 설정할 수 있습니다:

- `enableDebugLog`: 일반 디버그 로그 활성화
- `showNetworkTraffic`: 네트워크 트래픽 로그 활성화

### 일반적인 문제 해결

1. **연결 실패**
   - 서버 주소와 포트가 올바른지 확인
   - 서버가 실행 중인지 확인
   - 방화벽 설정 확인

2. **메시지 전송 실패**
   - 연결 상태 확인 (`IsConnected`)
   - 인증이 필요한 메시지인 경우 세션 토큰 확인
   - 메시지 구조가 올바른지 확인

3. **전송 방식 변경 실패**
   - 현재 연결을 먼저 해제해야 할 수 있음
   - 새로운 전송 방식이 서버에서 지원되는지 확인

## 예제 코드

### 완전한 연결 예시

```csharp
public class NetworkExample : MonoBehaviour
{
    async void Start()
    {
        // 이벤트 구독
        NetworkManager.Instance.OnConnectionChanged += OnConnectionChanged;
        NetworkManager.Instance.OnMessageReceived += OnMessageReceived;
        NetworkManager.Instance.OnError += OnError;
        
        // 서버 연결
        bool connected = await NetworkManager.Instance.ConnectAsync();
        if (connected)
        {
            Debug.Log("서버 연결 성공!");
            
            // 메시지 전송 예시
            var message = new ChatMessage
            {
                Content = "Hello Server!",
                RoomId = "room1"
            };
            
            await NetworkManager.Instance.SendMessageAsync(message);
        }
    }
    
    private void OnConnectionChanged(bool isConnected)
    {
        Debug.Log($"연결 상태 변경: {isConnected}");
    }
    
    private void OnMessageReceived(INetworkMessage message)
    {
        Debug.Log($"메시지 수신: {message.MessageType}");
    }
    
    private void OnError(string error)
    {
        Debug.LogError($"네트워크 오류: {error}");
    }
    
    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnConnectionChanged -= OnConnectionChanged;
            NetworkManager.Instance.OnMessageReceived -= OnMessageReceived;
            NetworkManager.Instance.OnError -= OnError;
        }
    }
}
```

## 주의사항

1. NetworkManager는 Singleton 패턴을 사용하므로 `NetworkManager.Instance`로 접근해야 합니다.
2. 모든 네트워크 작업은 비동기(`async/await`)로 수행됩니다.
3. 메시지 전송 전에 반드시 연결 상태를 확인하세요.
4. 인증이 필요한 메시지는 반드시 세션 토큰을 설정한 후 전송하세요.
5. 전송 방식 변경 시 기존 연결이 끊어지고 재연결됩니다.
6. 앱 종료 시 자동으로 연결이 해제됩니다.