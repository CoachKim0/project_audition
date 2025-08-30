# NetTest.cs 사용 매뉴얼

## 개요

NetTest.cs는 Unity OnGUI를 사용하여 **직접 gRPC 통신**으로 서버의 회원가입/로그인 인증 기능을 테스트하는 도구입니다. HTTP/2 프로토콜을 강제 사용하며, UGUI 컴포넌트 없이 코드만으로 빠르게 인증 기능을 테스트할 수 있습니다.

### 주요 특징
- **직접 gRPC 통신**: Unity NetworkManager 대신 gRPC 클라이언트 직접 사용
- **HTTP/2 강제**: YetAnotherHttpHandler를 통한 HTTP/2 전용 통신
- **실시간 서버 제어**: 서버 시작/종료 및 로그 모니터링
- **protobuf 메시지**: GameMessage, AuthUser를 사용한 타입 안전성

## 설치 및 설정

### 1. 필수 패키지 설치

Unity Package Manager를 통해 다음 패키지들을 설치:

```bash
# Package Manager Console에서
Install-Package Grpc.Net.Client
Install-Package YetAnotherHttpHandler
Install-Package UniTask
```

또는 manifest.json에 직접 추가:
```json
{
  "dependencies": {
    "com.cysharp.unitask": "2.3.3",
    "com.unity.nuget.newtonsoft-json": "3.2.1"
  },
  "scopedRegistries": [
    {
      "name": "Cysharp",
      "url": "https://package.openupm.com",
      "scopes": ["com.cysharp"]
    }
  ]
}
```

### 2. protobuf 파일 복사

서버에서 생성된 protobuf 파일들을 Unity 프로젝트로 복사:

```bash
# 서버의 Generated 파일들
Server/Server_Study/PacketGenerator/obj/Debug/net8.0/Protos/Game.cs
Server/Server_Study/PacketGenerator/obj/Debug/net8.0/Protos/GameGrpc.cs

# Unity 프로젝트로 복사
Assets/Scripts/NetworkManager/Generated/Game.cs
Assets/Scripts/NetworkManager/Generated/GameGrpc.cs
```

### 3. NetTest.cs 컴포넌트 추가

1. Unity 씬에 빈 GameObject 생성 (이름: "NetTest")
2. GameObject에 `NetTest.cs` 스크립트 추가
3. 자동으로 필요한 설정들이 초기화됨:
   - Server Address: `http://localhost:5554`
   - gRPC 채널 및 HTTP/2 핸들러 자동 설정

## UI 구성 및 기능

### 📡 연결 설정 섹션

#### 서버 주소 설정
- **Server Address**: gRPC 서버 주소 (기본: `http://localhost:5554`)
- **연결/해제 버튼**: gRPC 채널 연결/해제

#### 실시간 상태 표시
- **연결 상태**: 연결됨/연결 안됨
- **채널 상태**: Ready/Connecting/Shutdown 등 gRPC 채널 상태
- **HTTP 프로토콜**: HTTP/2 사용 확인

### 🖥️ 서버 제어 섹션

#### 서버 관리
- **Start Server**: 서버 자동 실행 (`dotnet run`)
- **Kill Server**: 서버 프로세스 종료
- **Server Status**: 서버 실행 상태 표시

#### 서버 로그 모니터링
- **실시간 로그**: 서버 출력 실시간 표시
- **HTTP/2 요청 확인**: 서버 로그에서 HTTP/2 POST 확인 가능
- **로그 스크롤**: 자동 스크롤 및 수동 스크롤 지원

### 👤 인증 테스트 섹션

#### 사용자 정보 입력
- **Username**: 사용자명 입력 (영문/숫자)
- **Password**: 비밀번호 입력 (마스킹 처리됨)

#### 테스트 버튼
- **Sign Up**: 회원가입 요청 (`GameMessage.Types.MessageType.Signup`)
- **Login**: 로그인 요청 (`GameMessage.Types.MessageType.Login`)

#### 미리 등록된 테스트 계정

| 사용자명 | 비밀번호 | 상태 |
|---------|----------|------|
| `testuser` | `password123` | 이미 등록됨 (로그인 테스트용) |
| `newuser123` | `password123` | 새 사용자 (회원가입 테스트용) |

**참고**: 서버는 중복된 사용자명으로 회원가입 시 `UserAlreadyExists` 오류를 반환합니다.

### 📊 로그 및 응답 섹션

#### 실시간 로그 표시
- **클라이언트 로그**: 요청/응답 상세 정보
- **서버 로그**: 서버 콘솔 출력 실시간 모니터링
- **성능 측정**: 응답 시간 (ms) 표시

#### 로그 정보
- **HTTP/2 확인**: 서버 로그에서 HTTP/2 POST 요청 확인
- **응답 코드**: Success/UserAlreadyExists/InvalidCredentials 등
- **타임스탬프**: 요청/응답 시간 기록
- **오류 메시지**: gRPC 오류 및 예외 상세 정보

### 🎛️ 유틸리티 기능

- **Clear Logs**: 모든 로그 초기화
- **Auto-scroll**: 새 로그 자동 스크롤
- **Log Filtering**: 특정 타입 로그만 표시 (예정)

## 테스트 시나리오

### 완전한 테스트 순서

#### 1. 서버 시작 및 연결
```bash
# 방법 1: UI에서 서버 시작
1. NetTest UI에서 "Start Server" 버튼 클릭
2. 서버 로그에서 시작 확인:
   - "gRPC 서버 시작됨 (포트: 5554)"
   - "소켓 서버 초기화 성공! (포트: 7777)"

# 방법 2: 수동 서버 시작
cd "E:/WORK/GIT/Project_Audition/Server/Server_Study/Server"
dotnet run
```

#### 2. gRPC 연결 테스트
1. Server Address: `http://localhost:5554` 확인
2. **Connect** 버튼 클릭
3. 연결 상태 확인: "연결됨"
4. 채널 상태 확인: "Ready"

#### 3. 새 사용자 회원가입 테스트
```csharp
// 입력값
Username: newuser456
Password: password123

// 버튼: "Sign Up" 클릭
// 예상 결과:
✅ 성공: User registered successfully
응답시간: 2-6ms
```

#### 4. 기존 사용자 로그인 테스트
```csharp
// 입력값 (이미 등록된 사용자)
Username: testuser  
Password: password123

// 버튼: "Login" 클릭
// 예상 결과:
✅ 성공: Login successful
응답시간: 0.3-2ms
```

#### 5. 중복 가입 오류 테스트
```csharp
// 입력값 (이미 존재하는 사용자)
Username: testuser
Password: anypassword

// 버튼: "Sign Up" 클릭
// 예상 결과:
❌ 실패: Username already exists (코드: UserAlreadyExists)
```

### HTTP/2 프로토콜 검증

#### 서버 로그에서 HTTP/2 확인
성공적인 요청 시 서버 로그에 다음과 같이 표시됩니다:

```bash
[서버] info: Microsoft.AspNetCore.Hosting.Diagnostics[1]
      Request starting HTTP/2 POST http://localhost:5554/GameService/Game
      application/grpc content-length:43
[서버] info: Microsoft.AspNetCore.Hosting.Diagnostics[2]  
      Request finished HTTP/2 POST http://localhost:5554/GameService/Game
      application/grpc - 200 0ms
```

**중요**: HTTP/1.1이 표시되면 YetAnotherHttpHandler 설정을 확인해야 합니다.

### 성능 및 오류 테스트

#### 1. 성능 테스트 (응답 시간)
```csharp
// 로컬 서버 기대값
- 연결: ~100ms
- 로그인: 0.3-6ms  
- 회원가입: 1-10ms

// 응답 시간이 100ms 이상이면 네트워크 또는 서버 상태 확인 필요
```

#### 2. 네트워크 오류 테스트
```csharp
// 서버 종료 후 요청
1. "Kill Server" 버튼으로 서버 종료
2. 로그인 시도
3. 예상 결과: gRPC 연결 오류

// 잘못된 주소
1. Server Address: "http://localhost:9999"
2. Connect 시도
3. 예상 결과: 연결 실패
```

#### 3. protobuf 직렬화 테스트
```csharp
// 빈 값 테스트
Username: "" (빈 문자열)
Password: ""
// 예상: 서버에서 validation 오류

// 특수문자 테스트  
Username: "test@user#123"
Password: "pass!@#$"
// 예상: 정상 처리 (서버 validation에 따라)
```

## 로그 해석 가이드

### 성공적인 회원가입 로그
```bash
# 클라이언트 로그
[gRPC] 회원가입 요청: newuser456
[gRPC] 응답 수신 - 성공: User registered successfully
응답시간: 3ms

# 서버 로그  
[서버] info: Request starting HTTP/2 POST http://localhost:5554/GameService/Game
[서버] [AuthHandler] 회원가입 요청: newuser456
[서버] [AuthHandler] 회원가입 성공: newuser456
[서버] info: Request finished HTTP/2 POST - 200 3ms
```

### 성공적인 로그인 로그
```bash
# 클라이언트 로그
[gRPC] 로그인 요청: testuser
[gRPC] 응답 수신 - 성공: Login successful  
응답시간: 1ms

# 서버 로그
[서버] info: Request starting HTTP/2 POST http://localhost:5554/GameService/Game
[서버] [AuthHandler] 로그인 요청: testuser
[서버] [AuthHandler] 로그인 성공: testuser
[서버] info: Request finished HTTP/2 POST - 200 1ms
```

### 오류 상황 로그

#### 1. 사용자 중복 오류
```bash
[gRPC] 회원가입 요청: testuser
❌ 실패: Username already exists (코드: UserAlreadyExists)
응답시간: 2ms
```

#### 2. gRPC 연결 오류
```bash
❌ gRPC 오류: Status(StatusCode="Unavailable", Detail="Connection refused")
```

#### 3. HTTP 프로토콜 다운그레이드 오류 (해결됨)
```bash
# 이전 오류 (현재는 해결됨)
❌ gRPC 오류: Bad gRPC response. Response protocol downgraded to HTTP/1.1

# 해결 후
✅ HTTP/2 연결 성공
```

## protobuf 응답 코드 참조

### ResultCode 열거형

| 코드 | 이름 | 설명 | 발생 상황 |
|------|------|------|----------|
| `0` | Success | 성공 | 정상적인 회원가입/로그인 |
| `1` | UserAlreadyExists | 사용자 이미 존재 | 중복된 사용자명으로 회원가입 시도 |
| `2` | InvalidCredentials | 잘못된 인증정보 | 틀린 사용자명/비밀번호로 로그인 |
| `3` | InternalError | 서버 내부 오류 | 서버 처리 중 예외 발생 |

### GameMessage.Types.MessageType

| 타입 | 값 | 용도 |
|------|----|----- |
| None | 0 | 기본값 |
| Login | 1 | 로그인 요청 |
| Signup | 2 | 회원가입 요청 |
| LoginResponse | 3 | 로그인 응답 (서버→클라이언트) |
| SignupResponse | 4 | 회원가입 응답 (서버→클라이언트) |

### gRPC StatusCode (오류 시)

| 코드 | 설명 | 해결 방법 |
|------|------|----------|
| Unavailable | 서버 연결 불가 | 서버 실행 상태 확인 |
| DeadlineExceeded | 응답 시간 초과 | 서버 성능 또는 네트워크 확인 |
| InvalidArgument | 잘못된 요청 | protobuf 메시지 구조 확인 |

## 문제 해결

### HTTP/2 관련 문제

#### 문제: "Bad gRPC response. Response protocol downgraded to HTTP/1.1"
**원인**: Unity의 기본 HttpClient가 HTTP/1.1 사용
**해결**: YetAnotherHttpHandler 사용 강제

```csharp
// 올바른 설정 (NetTest.cs에서 자동 처리됨)
var handler = new YetAnotherHttpHandler
{
    Http2Only = true  // HTTP/2만 사용
};
var httpClient = new System.Net.Http.HttpClient(handler);
```

#### 문제: YetAnotherHttpHandler 패키지 오류
**해결**: Package Manager에서 올바른 버전 설치
```bash
# GitHub URL을 통한 설치
https://github.com/Cysharp/YetAnotherHttpHandler.git
```

### gRPC 연결 문제

#### 1. 서버 연결 실패
```bash
# 서버 상태 확인
netstat -an | findstr :5554

# 서버 프로세스 확인  
tasklist | findstr dotnet

# 방화벽 해제 (Windows)
netsh advfirewall set allprofiles state off
```

#### 2. 채널 상태 오류
```csharp
// 채널 상태 확인
if (_channel.State == ConnectivityState.Shutdown)
{
    // 새로운 채널 생성 필요
    await CreateChannelAsync();
}
```

### protobuf 직렬화 문제

#### 1. Generated 파일 버전 불일치
```bash
# 서버와 클라이언트의 protobuf 파일이 다를 때
# 해결: 서버에서 최신 Generated 파일 복사
cp Server/PacketGenerator/obj/Debug/net8.0/Protos/* \
   Client/Assets/Scripts/NetworkManager/Generated/
```

#### 2. 메시지 필드 누락
```csharp
// GameMessage 필드 확인
var message = new GameMessage
{
    Type = GameMessage.Types.MessageType.Login,  // 필수
    AuthUser = new AuthUser  // 필수
    {
        Username = username,  // null이면 안됨
        Password = password   // null이면 안됨  
    }
};
```

### 성능 문제

#### 1. 응답 시간이 느린 경우 (>100ms)
- 서버 CPU 사용률 확인
- 네트워크 지연 측정
- gRPC 채널 재사용 확인

#### 2. 메모리 사용량 증가
```csharp
// gRPC 리소스 정리
if (_call != null)
{
    _call.Dispose();
    _call = null;
}
if (_channel != null)
{
    await _channel.ShutdownAsync();
    _channel.Dispose();
}
```

## 기술적 세부사항

### 사용된 핵심 기술

#### gRPC 스택
```csharp
// 주요 패키지
Grpc.Net.Client          // gRPC 클라이언트
YetAnotherHttpHandler    // Unity HTTP/2 지원
Google.Protobuf          // protobuf 직렬화
Cysharp.Threading.Tasks  // UniTask 비동기 처리
```

#### HTTP/2 강제 설정
```csharp
// YetAnotherHttpHandler 설정
var handler = new YetAnotherHttpHandler
{
    Http2Only = true,
    // PooledConnectionLifetime는 사용하지 않음 (Unity 호환성)
};

// gRPC 채널 설정
var options = new GrpcChannelOptions
{
    HttpClient = httpClient,
    MaxReceiveMessageSize = 4 * 1024 * 1024,
    MaxSendMessageSize = 4 * 1024 * 1024,
    DisposeHttpClient = true
};
```

### 아키텍처 특징

1. **직접 gRPC**: Unity NetworkManager 우회
2. **양방향 스트리밍**: DuplexStreaming gRPC 메서드
3. **타입 안전성**: protobuf로 컴파일 타임 검증
4. **비동기 처리**: UniTask로 메인 스레드 블록킹 방지
5. **실시간 모니터링**: 서버 로그 실시간 표시

### 확장 가능한 기능

#### 1. 추가 인증 방식
```csharp
// OAuth 토큰 인증
AuthUser = new AuthUser
{
    Token = "oauth_token_here"
}

// JWT 토큰 검증
// 서버 측에서 JWT 미들웨어 추가 가능
```

#### 2. 게임 메시지 확장
```csharp
// GameMessage에 새 타입 추가 가능
Gameplay = 5,
Chat = 6,
Inventory = 7
```

#### 3. 성능 최적화
- gRPC 연결 풀링
- protobuf 메시지 재사용
- 압축 활성화 (Gzip)

## 주의사항 및 제한사항

### 성능 관련
1. **OnGUI 성능**: 매 프레임 호출되므로 복잡한 렌더링 피하기
2. **gRPC 연결**: 불필요한 채널 생성/해제 지양
3. **로그 메모리**: 자동으로 최대 100줄로 제한됨
4. **서버 모니터링**: BashOutput 폴링으로 인한 약간의 오버헤드

### 플랫폼 제한
1. **Windows 전용**: dotnet run 명령어 Windows 경로 사용
2. **HTTP/2 요구**: 일부 구형 프록시/방화벽에서 차단 가능
3. **Unity 2022.3+**: YetAnotherHttpHandler 호환성 요구
4. **.NET 8.0**: 서버는 .NET 8.0 런타임 필요

### 보안 고려사항
1. **평문 전송**: 현재 HTTP (비암호화) 사용 중
   - 운영 환경에서는 HTTPS 사용 권장
2. **패스워드 저장**: UI에서 평문 표시됨 (개발 전용)
3. **서버 제어**: NetTest에서 서버 시작/종료 가능 (개발 편의성)

### 권장 사용 환경
```bash
# 개발 환경
Unity: 2022.3 LTS 이상
.NET: 8.0 Runtime  
OS: Windows 10/11
RAM: 8GB 이상 (서버 + Unity)

# 네트워크
Loopback: 127.0.0.1 (로컬 테스트)
HTTP/2: 필수 지원
Port: 5554 (gRPC), 7777 (Socket) 사용 가능
```

---

**이 매뉴얼을 통해 NetTest.cs의 직접 gRPC 통신을 활용하여 HTTP/2 기반의 안정적인 인증 시스템을 테스트할 수 있습니다.**