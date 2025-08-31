# InGame_Server 매뉴얼

## 개요
InGame_Server는 마이크로서비스 아키텍처의 핵심 게임 로직 서버로, 실시간 8인 네트워크 대전, 게임 방 관리, 플레이어 매칭을 담당합니다. 하이브리드 아키텍처를 채택하여 TCP 소켓(포트 7777)과 gRPC(포트 5551)를 동시 지원합니다.

## 주요 기능
- **실시간 8인 네트워크 대전**: 30fps 동기화를 통한 고성능 멀티플레이어 게임
- **게임방 관리**: 방 생성, 플레이어 매칭, 게임 상태 관리
- **플레이어 세션 관리**: 연결 상태, 인증, 게임 내 액션 처리
- **마이크로서비스 통신**: gRPC를 통한 다른 서비스(DB, Chat, Log)와의 연동
- **성능 최적화**: JobQueue 기반 비동기 처리 및 메모리 풀링

## 아키텍처

### 네트워킹 구조
```
InGame_Server
├── TCP Socket Server (포트 7777) - 게임 클라이언트와 실시간 통신
├── gRPC Server (포트 5551) - 다른 서비스와의 통신
└── Service Clients - 외부 서비스 호출 (DB, Chat, Log)
```

### 서비스 의존성
- **DB_Server** (포트 5553): 사용자 인증 및 데이터 관리
- **Chat_Server** (포트 5552): 게임 내 채팅 기능
- **Log_Server** (포트 5554): 중앙집중식 로깅

## 설정 및 설치

### 전제 조건
- .NET 8.0 SDK
- Visual Studio 2022 또는 VS Code
- 실행 중인 DB_Server, Chat_Server, Log_Server

### 프로젝트 구조
```
InGame_Server/
├── Program.cs              # 서버 진입점 및 초기화
├── InGame_Server.csproj    # 프로젝트 설정
├── Modules/
│   ├── GamePlay/           # 게임 로직 모듈
│   ├── NetworkBattle/      # 8인 네트워크 대전 시스템
│   ├── Room/              # 게임방 관리
│   └── Ping/              # 연결 상태 확인
├── Services/              # 마이크로서비스 클라이언트
├── Session/               # 플레이어 세션 관리
├── Gateway/               # API 게이트웨이
└── Protos/               # gRPC 프로토콜 정의
```

### 빌드 및 실행
```bash
# 프로젝트 빌드
cd InGame_Server
dotnet build

# 서버 실행
dotnet run

# 특정 설정으로 실행
dotnet run --project InGame_Server.csproj --configuration Release
```

## 핵심 구성 요소

### 1. NetworkBattle 시스템
8인 실시간 네트워크 대전을 담당하는 핵심 모듈입니다.

#### BattleRoom 클래스
```csharp
// 배틀룸 생성
var battleRoom = new BattleRoom();

// 플레이어 추가
var player = new BattlePlayer 
{
    UserId = "user123",
    Nickname = "Player1",
    Session = clientSession
};
battleRoom.AddPlayer(player);

// 게임 시작 (8인 풀방일 때 자동)
battleRoom.StartBattle();
```

#### 주요 특징
- **30fps 동기화**: 33.33ms 간격으로 게임 상태 업데이트
- **실시간 액션 브로드캐스트**: 플레이어 액션을 다른 참가자에게 즉시 전송
- **상태 관리**: Empty → Waiting → Ready → Playing → Finished

### 2. 하이브리드 서버 구조

#### TCP 소켓 서버 (포트 7777)
```csharp
// 소켓 서버 초기화 (Program.cs)
IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7777);
listener.Init(endPoint, () => { return Session_Manager.Instance.Generate(); });
```

#### gRPC 서버 (포트 5551)
```csharp
// gRPC 서비스 등록
builder.Services.AddGrpc();
builder.Services.AddScoped<IRoomHandler, RoomHandler>();
builder.Services.AddScoped<IPingHandler, PingHandler>();

// 서비스 매핑
app.MapGrpcService<GameGrpcService>();
```

### 3. 마이크로서비스 클라이언트

#### AuthServiceClient
```csharp
var authClient = new AuthServiceClient();

// 사용자 토큰 검증
bool isValid = await authClient.ValidateUserAsync(userId, token);

// 로그인 처리
string? token = await authClient.LoginAsync(username, password);
```

#### ChatServiceClient
```csharp
var chatClient = new ChatServiceClient();

// 메시지 전송
await chatClient.SendMessageAsync(roomId, userId, message);

// 방 전체 브로드캐스트
await chatClient.BroadcastToRoomAsync(roomId, message);
```

#### LogServiceClient
```csharp
var logClient = new LogServiceClient();

// 단일 로그 전송
await logClient.LogAsync("INFO", "게임 시작", "Battle");

// 배치 로그 전송
await logClient.LogBatchAsync(logEntries);
```

## 게임플레이 시스템

### 플레이어 액션 처리
```csharp
public class PlayerAction
{
    public string UserId { get; set; }
    public ActionType Type { get; set; }  // Move, Attack, UseSkill, etc.
    public PlayerPosition Position { get; set; }
    public Dictionary<string, object> Data { get; set; }
    public DateTime Timestamp { get; set; }
}
```

### 실시간 동기화 플로우
1. **클라이언트 액션**: 플레이어가 게임 내에서 액션 수행
2. **서버 검증**: InGame_Server에서 액션 유효성 검사
3. **상태 업데이트**: 게임 상태 업데이트 (30fps)
4. **브로드캐스트**: 다른 플레이어들에게 변경사항 전송
5. **로깅**: 중요 이벤트를 Log_Server로 전송

### 방 생성 및 매칭 시스템
```csharp
// 게임방 생성
var gameRoom = new GameRoom();
Program.room = gameRoom;

// 플레이어 매칭
if (battleRoom.PlayerCount < battleRoom.MaxPlayers)
{
    battleRoom.AddPlayer(new BattlePlayer 
    {
        UserId = userId,
        Session = clientSession
    });
}
```

## 성능 최적화

### JobQueue 시스템
```csharp
// JobQueue 성능 모니터링
var stats = ServerCore.JobQueueManager.Instance.GetQueueStats();
foreach (var stat in stats)
{
    Console.WriteLine($"Queue '{stat.Key}': {stat.Value} jobs pending");
}
```

### 메모리 관리
- **버퍼 풀링**: SendBuffer, RecvBuffer의 재사용을 통한 GC 압박 감소
- **객체 풀링**: 자주 생성되는 객체들의 풀 관리
- **세션 관리**: 효율적인 연결 관리 및 리소스 해제

### 네트워크 최적화
- **비동기 I/O**: SocketAsyncEventArgs를 활용한 고성능 네트워킹
- **패킷 압축**: 필요시 protobuf 압축을 통한 대역폭 절약
- **배치 처리**: 여러 패킷을 한 번에 처리하여 성능 향상

## API 엔드포인트

### gRPC 서비스
- **GameGrpcService**: 게임 관련 gRPC 엔드포인트
- **포트**: 5551 (HTTP/2)

### TCP 소켓 엔드포인트
- **메인 게임 서버**: 포트 7777
- **프로토콜**: 커스텀 바이너리 (protobuf 기반)

## 모니터링 및 로깅

### 실시간 통계
```bash
=== JobQueue 통계 ===
Queue 'BattleRoom_001': 15 jobs pending
Queue 'PlayerActions': 8 jobs pending
Queue 'NetworkSync': 3 jobs pending
=====================
```

### 로그 카테고리
- **Battle**: 게임 대전 관련 로그
- **Network**: 네트워크 연결 및 통신 로그
- **Performance**: 성능 관련 메트릭
- **Error**: 오류 및 예외 상황

### 성능 벤치마크
```csharp
// JobQueue 성능 테스트 실행
JobQueuePerformanceTest.RunBenchmark();
```

## 에러 처리 및 복구

### 연결 장애 처리
```csharp
try
{
    // 마이크로서비스 호출
    await authClient.ValidateUserAsync(userId, token);
}
catch (Exception ex)
{
    // 로컬 캐시 또는 대체 로직
    Console.WriteLine($"인증 서비스 연결 실패: {ex.Message}");
    // 임시 인증 또는 오프라인 모드
}
```

### 게임 상태 복구
- **플레이어 재연결**: 세션 복구 및 게임 상태 동기화
- **방 상태 보존**: 일시적 연결 끊김 시 게임 진행 상태 유지
- **장애 감지**: 자동 장애 감지 및 알림

## 개발 가이드

### 새로운 게임 모드 추가
1. `Modules/GamePlay/` 에 새 모듈 생성
2. `BattleRoom` 클래스 확장 또는 새 Room 클래스 생성
3. 필요한 패킷 타입 정의
4. gRPC 서비스 엔드포인트 추가

### 성능 튜닝 팁
- JobQueue 처리 속도 모니터링
- 메모리 사용량 프로파일링
- 네트워크 대역폭 최적화
- 데이터베이스 쿼리 최적화 (DB_Server와 협업)

### 보안 고려사항
- 플레이어 액션 검증 강화
- 치트 방지 시스템
- 토큰 기반 인증 (DB_Server 연동)
- 접속 제한 및 DDoS 방어

## 배포

### 서버 시작 순서
1. DB_Server 시작 (포트 5553)
2. Log_Server 시작 (포트 5554)  
3. Chat_Server 시작 (포트 5552)
4. **InGame_Server 시작** (포트 7777, 5551)

### 환경별 설정
```bash
# 개발 환경
dotnet run --environment Development

# 프로덕션 환경  
dotnet run --environment Production --urls "https://localhost:5551;http://localhost:7777"
```

### Docker 배포 (권장)
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY . .
EXPOSE 7777 5551
ENTRYPOINT ["dotnet", "InGame_Server.dll"]
```

## 문제 해결

### 일반적인 문제

1. **포트 충돌**: 7777, 5551 포트가 이미 사용 중인 경우
   ```bash
   # 포트 사용 확인
   netstat -an | findstr 7777
   # 프로세스 종료 후 재시작
   ```

2. **마이크로서비스 연결 실패**: 다른 서비스가 실행되지 않은 경우
   ```bash
   # 서비스 상태 확인
   curl https://localhost:5553/health  # DB_Server
   curl https://localhost:5552/health  # Chat_Server
   curl https://localhost:5554/health  # Log_Server
   ```

3. **게임 성능 저하**: JobQueue 오버로드
   ```csharp
   // 큐 상태 모니터링
   var stats = JobQueueManager.Instance.GetQueueStats();
   // 필요시 처리 스레드 수 증가
   ```

4. **메모리 누수**: 장시간 실행 시 메모리 증가
   - 세션 정리 로직 확인
   - 버퍼 풀 상태 점검
   - GC 로그 분석

### 로그 분석
주요 로그 패턴:
- `게임 시작`: 새 게임 세션 시작
- `플레이어 입장/퇴장`: 방 입/퇴장 이벤트  
- `액션 처리`: 플레이어 액션 검증 및 처리
- `네트워크 오류`: 연결 문제 또는 패킷 손실

### 성능 튜닝
- **CPU**: JobQueue 처리 스레드 조정
- **메모리**: 버퍼 풀 크기 최적화
- **네트워크**: TCP 버퍼 크기 튜닝
- **디스크**: 로그 레벨 조정

## 참고 자료
- [마이크로서비스 아키텍처 전체 가이드](./MICROSERVICES_ARCHITECTURE.md)
- [DB_Server 매뉴얼](./DB_Server/DB_SERVER_MANUAL.md)
- [Chat_Server 매뉴얼](./Chat_Server/CHAT_SERVER_MANUAL.md)
- [Log_Server 매뉴얼](./Log_Server/LOG_SERVER_MANUAL.md)
- [gRPC 공식 문서](https://grpc.io/docs/)
- [.NET 8.0 성능 가이드](https://docs.microsoft.com/dotnet/core/whats-new/dotnet-8)