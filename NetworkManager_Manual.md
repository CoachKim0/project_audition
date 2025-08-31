# NetworkManager 사용 매뉴얼

## 개요

이 매뉴얼은 Unity Client에서 서버와의 인증 테스트를 위한 NetTest.cs 시스템 사용법을 다룹니다. 직접 gRPC 통신을 사용하여 HTTP/2 프로토콜로 서버의 인증 기능을 테스트할 수 있습니다.

## 주요 특징

- **직접 gRPC 통신**: Unity NetworkManager 대신 gRPC 클라이언트 직접 사용
- **HTTP/2 강제 사용**: YetAnotherHttpHandler를 통한 HTTP/2 전용 통신
- **OnGUI 인터페이스**: 빠른 테스트를 위한 즉시 모드 UI
- **실시간 로깅**: 네트워크 상태 및 응답 실시간 확인

## 기본 설정

### 1. 필수 패키지 설치

Unity Package Manager를 통해 다음 패키지들을 설치해야 합니다:

- **Grpc.Net.Client**: gRPC 클라이언트 기능
- **YetAnotherHttpHandler**: Unity에서 HTTP/2 지원
- **Cysharp.Threading.Tasks (UniTask)**: 비동기 작업 최적화

```bash
# Package Manager Console에서
Install-Package Grpc.Net.Client
Install-Package YetAnotherHttpHandler
Install-Package UniTask
```

### 2. protobuf 파일 복사

서버에서 생성된 protobuf 파일들을 Unity 프로젝트로 복사:

- `Game.cs` → `Assets/Scripts/NetworkManager/Generated/`
- `GameGrpc.cs` → `Assets/Scripts/NetworkManager/Generated/`

### 3. NetTest.cs 컴포넌트 추가

씬의 GameObject에 NetTest.cs 스크립트를 컴포넌트로 추가합니다.

## 기본 사용법

### NetTest UI 인터페이스

NetTest.cs는 OnGUI를 사용한 즉시 모드 UI를 제공합니다:

#### 연결 설정 섹션
- **Server Address**: 서버 주소 입력 (기본: http://localhost:5554)
- **Connect/Disconnect**: 서버 연결/해제 버튼
- **연결 상태**: 실시간 연결 상태 표시

#### 인증 테스트 섹션  
- **Username/Password**: 계정 정보 입력
- **Sign Up**: 회원가입 테스트
- **Login**: 로그인 테스트
- **로그**: 서버 응답 및 오류 메시지 표시

#### 서버 제어 섹션
- **Start Server**: 서버 실행 (dotnet run)
- **Kill Server**: 서버 종료
- **Server Status**: 서버 상태 및 출력 로그

### gRPC 메시지 전송 과정

#### 1. gRPC 채널 생성
```csharp
var handler = new YetAnotherHttpHandler
{
    Http2Only = true // HTTP/2만 사용
};
var httpClient = new System.Net.Http.HttpClient(handler);
_channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions
{
    HttpClient = httpClient,
    MaxReceiveMessageSize = 4 * 1024 * 1024,
    MaxSendMessageSize = 4 * 1024 * 1024,
    DisposeHttpClient = true
});
```

#### 2. 인증 메시지 생성 및 전송
```csharp
// 회원가입 메시지
var signupMessage = new GameMessage
{
    AuthUser = new AuthUser
    {
        Username = username,
        Password = password
    },
    Type = GameMessage.Types.MessageType.Signup
};

// 로그인 메시지
var loginMessage = new GameMessage
{
    AuthUser = new AuthUser
    {
        Username = username,
        Password = password
    },
    Type = GameMessage.Types.MessageType.Login
};
```

### HTTP/2 프로토콜 강제 사용

Unity에서 HTTP/2를 사용하려면 YetAnotherHttpHandler가 필요합니다:

```csharp
// HTTP/2 전용 핸들러 설정
var handler = new YetAnotherHttpHandler
{
    Http2Only = true
};

// gRPC 채널 옵션 설정
var options = new GrpcChannelOptions
{
    HttpClient = new System.Net.Http.HttpClient(handler),
    MaxReceiveMessageSize = 4 * 1024 * 1024,
    MaxSendMessageSize = 4 * 1024 * 1024,
    DisposeHttpClient = true
};
```

**중요**: 서버도 HTTP/2만 허용하도록 설정되어 있어야 합니다:
```csharp
// 서버의 Kestrel 설정
listenOptions.Protocols = HttpProtocols.Http2;
```

### 인증 응답 처리

서버에서 받는 인증 응답 처리:

```csharp
// 응답 결과 확인
if (response.ResultCode == ResultCode.Success)
{
    AddLog($"✅ 성공: {response.Message}");
}
else
{
    AddLog($"❌ 실패: {response.Message} (코드: {response.ResultCode})");
}

// 응답 시간 측정
var responseTime = stopwatch.ElapsedMilliseconds;
AddLog($"응답시간: {responseTime}ms");
```

**주요 응답 코드:**
- `Success`: 성공
- `UserAlreadyExists`: 사용자가 이미 존재함
- `InvalidCredentials`: 잘못된 인증 정보
- `InternalError`: 서버 내부 오류

## 상태 모니터링

NetTest.cs는 실시간 상태 모니터링을 제공합니다:

### 연결 상태 표시

```csharp
// UI에 실시간 연결 상태 표시
GUI.Label(new Rect(10, 40, 300, 20), $"연결 상태: {(_isConnected ? "연결됨" : "연결 안됨")}");
if (_channel != null)
{
    GUI.Label(new Rect(10, 60, 300, 20), $"채널 상태: {_channel.State}");
}
```

### 서버 로그 모니터링

서버 출력을 실시간으로 모니터링:

```csharp
// 서버 상태 확인
if (_serverBashId != null)
{
    // BashOutput을 통한 서버 로그 확인
    var output = BashOutput.GetOutput(_serverBashId);
    if (!string.IsNullOrEmpty(output))
    {
        _serverLogs.Add($"[서버] {output}");
    }
}
```

### 에러 처리 및 로깅

```csharp
// gRPC 에러 처리
catch (RpcException ex)
{
    AddLog($"❌ gRPC 오류: {ex.Status.Detail}");
}
catch (Exception ex)
{
    AddLog($"❌ 일반 오류: {ex.Message}");
}
```

## 고급 사용법

### 성능 측정 및 분석

```csharp
// 응답 시간 측정
var stopwatch = Stopwatch.StartNew();
// ... gRPC 호출 ...
stopwatch.Stop();
AddLog($"응답시간: {stopwatch.ElapsedMilliseconds}ms");
```

**일반적인 응답 시간:**
- 로컬 서버: 0.3-6ms
- 원격 서버: 10-50ms (네트워크 상황에 따라)

### 서버 로그 분석

서버 출력에서 HTTP/2 사용 확인:
```
[서버] info: Microsoft.AspNetCore.Hosting.Diagnostics[1]
      Request starting HTTP/2 POST http://localhost:5554/GameService/Game
```

### 채널 상태 모니터링

gRPC 채널의 연결 상태를 모니터링:

```csharp
// 채널 상태 확인
var channelState = _channel?.State;
switch (channelState)
{
    case ConnectivityState.Ready:
        // 연결 준비됨
        break;
    case ConnectivityState.Connecting:
        // 연결 중
        break;
    case ConnectivityState.Shutdown:
        // 연결 종료됨
        break;
}
```

## protobuf 메시지 구조

NetTest.cs에서 사용하는 메시지는 protobuf로 정의됩니다:

### GameMessage 구조

```csharp
public sealed partial class GameMessage : pb::IMessage<GameMessage>
{
    // 메시지 타입 열거형
    public enum Types
    {
        None = 0,
        Login = 1,
        Signup = 2,
        LoginResponse = 3,
        SignupResponse = 4
    }
    
    // 주요 필드들
    public Types Type { get; set; }           // 메시지 타입
    public AuthUser AuthUser { get; set; }    // 인증 정보
    public ResultCode ResultCode { get; set; } // 결과 코드
    public string Message { get; set; }        // 응답 메시지
}
```

### AuthUser 구조

```csharp
public sealed partial class AuthUser : pb::IMessage<AuthUser>
{
    public string Username { get; set; }  // 사용자명
    public string Password { get; set; }  // 비밀번호
    public string Token { get; set; }     // 세션 토큰
}
```

### ResultCode 열거형

```csharp
public enum ResultCode
{
    Success = 0,              // 성공
    UserAlreadyExists = 1,    // 사용자 이미 존재
    InvalidCredentials = 2,   // 잘못된 인증정보
    InternalError = 3         // 서버 내부 오류
}
```

## 문제 해결

### HTTP/2 관련 문제

**문제**: "Bad gRPC response. Response protocol downgraded to HTTP/1.1"
**해결**: YetAnotherHttpHandler 사용 및 서버 HTTP/2 설정 확인

```csharp
// 클라이언트: HTTP/2 강제 사용
var handler = new YetAnotherHttpHandler { Http2Only = true };

// 서버: HTTP/2만 허용
listenOptions.Protocols = HttpProtocols.Http2;
```

### 연결 문제

1. **서버 연결 실패**
   - 서버 주소 확인 (기본: http://localhost:5554)
   - 서버 실행 상태 확인
   - 방화벽 설정 확인

2. **gRPC 채널 오류**
   - 채널 상태 확인 (`_channel.State`)
   - 메시지 크기 제한 확인 (MaxReceiveMessageSize)
   - HTTP 클라이언트 Dispose 설정 확인

3. **protobuf 직렬화 오류**
   - Generated 파일들이 최신인지 확인
   - 서버와 클라이언트의 protobuf 버전 일치 확인

### 성능 문제

- **느린 응답**: 서버 상태 및 네트워크 확인
- **메모리 누수**: gRPC 채널과 스트림 적절한 Dispose 확인
- **CPU 사용량 증가**: 로그 출력 빈도 조절

## 완전한 사용 예시

### NetTest.cs 사용 시나리오

```csharp
// 1. 서버 시작
// UI에서 "Start Server" 버튼 클릭 → 서버 자동 실행

// 2. 클라이언트 연결
// Server Address: http://localhost:5554
// "Connect" 버튼 클릭

// 3. 회원가입 테스트
// Username: newuser123
// Password: password123
// "Sign Up" 버튼 클릭
// 예상 결과: ✅ 성공: User registered successfully

// 4. 로그인 테스트
// Username: newuser123 (또는 기존 사용자)
// Password: password123
// "Login" 버튼 클릭
// 예상 결과: ✅ 성공: Login successful

// 5. 중복 회원가입 테스트
// Username: testuser (기존 사용자)
// "Sign Up" 버튼 클릭
// 예상 결과: ❌ 실패: Username already exists (코드: UserAlreadyExists)
```

### 커스텀 테스트 시나리오

다른 사용자명으로 다양한 테스트:

```csharp
// 성공 케이스들
- newuser001, newuser002, ... (새로운 사용자명)
- 다양한 패스워드 길이 테스트

// 실패 케이스들  
- testuser (이미 존재하는 사용자)
- 잘못된 패스워드로 로그인 시도
- 빈 문자열 테스트
```

## 주의사항

1. **HTTP/2 필수**: 반드시 YetAnotherHttpHandler를 사용해야 합니다.
2. **서버 우선 실행**: 클라이언트 연결 전에 서버가 실행되어 있어야 합니다.
3. **protobuf 버전 일치**: 서버와 클라이언트의 Generated 파일이 동일해야 합니다.
4. **포트 충돌 주의**: 기본 포트 5554가 사용 중이면 다른 포트 사용.
5. **비동기 작업**: 모든 gRPC 호출은 UniTask를 사용한 비동기 처리.
6. **메모리 관리**: gRPC 채널과 스트림은 사용 후 반드시 Dispose.
7. **로그 모니터링**: UI 로그를 통해 네트워크 상태 실시간 확인 권장.

## 성능 벤치마크

**로컬 서버 (localhost:5554)**
- 연결 시간: ~100ms
- 인증 응답: 0.3-6ms
- HTTP/2 프로토콜 확인됨
- 안정적인 양방향 스트리밍 지원

**권장 테스트 환경**
- Unity 2022.3 LTS 이상
- .NET 8.0 서버
- Windows 10/11 또는 macOS