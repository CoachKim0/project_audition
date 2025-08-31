# M1_Services 마이크로서비스 아키텍처 매뉴얼

> 모놀리식 게임 서버를 4개의 독립적인 마이크로서비스로 분리한 아키텍처 가이드

## 📋 목차
- [아키텍처 개요](#아키텍처-개요)
- [서비스 구성](#서비스-구성)
- [포트 할당](#포트-할당)
- [서비스별 상세 가이드](#서비스별-상세-가이드)
- [서비스 간 통신](#서비스-간-통신)
- [실행 방법](#실행-방법)
- [개발 가이드](#개발-가이드)
- [모니터링 및 로깅](#모니터링-및-로깅)
- [트러블슈팅](#트러블슈팅)

---

## 🏗️ 아키텍처 개요

### 설계 원칙
- **단일 책임**: 각 서비스는 명확한 하나의 역할 담당
- **독립 배포**: 서비스별로 독립적 배포 및 확장 가능
- **기술 다양성**: 서비스별 최적 기술 스택 선택
- **장애 격리**: 한 서비스 장애가 전체 시스템에 미치는 영향 최소화

### 전체 구조
```
M1_Services/
├── InGame_Server/      # 게임 로직 + 8인 네트워크 대전
├── Chat_Server/        # 채팅 시스템 (TCP + gRPC 하이브리드)
├── DB_Server/          # 데이터베이스 및 인증 서비스
├── Log_Server/         # 통합 로깅 및 모니터링
├── Shared/             # 공통 모델 및 인터페이스
├── Core/               # 네트워크 코어 라이브러리
└── PacketGenerator/    # Protocol Buffer 코드 생성 도구
```

---

## 🎯 서비스 구성

### 1. InGame_Server - 게임 로직 서비스
**역할**: 순수 게임 로직 및 8인 네트워크 대전 처리

**핵심 기능**:
- 8인 실시간 네트워크 대전 (30fps 동기화)
- 게임방 생성/관리
- 플레이어 상태 동기화
- 실시간 액션 브로드캐스트
- 게임 세션 관리

**주요 모듈**:
- `NetworkBattle/` - 8인 대전 전용 로직
- `GamePlay/Room/` - 일반 게임방 관리
- `Room/` - 방 생성/삭제/입장/퇴장
- `Ping/` - 연결 상태 관리

### 2. Chat_Server - 채팅 서비스
**역할**: 모든 채팅 관련 처리 (하이브리드 프로토콜 지원)

**핵심 기능**:
- 로비 채팅
- 게임방 내 채팅
- 야외활동 채팅
- 메시지 필터링 및 검열
- 실시간 브로드캐스트

**통신 방식**:
- **gRPC (5552)**: 서비스 간 통신
- **TCP (7778)**: 직접 채팅 클라이언트 연결 (고성능)

### 3. DB_Server - 데이터베이스 서비스
**역할**: 모든 데이터 저장 및 관리

**핵심 기능**:
- 사용자 인증/등록
- 사용자 정보 관리
- 게임 데이터 CRUD
- 세션 관리
- 보안 이벤트 처리

**데이터 저장**:
- MySQL + Entity Framework Core (Code First)
- 개발/운영 환경 모두 MySQL 사용
- 자동 마이그레이션 및 시드 데이터 지원

### 4. Log_Server - 로깅 및 모니터링 서비스
**역할**: 통합 로그 수집 및 분석

**핵심 기능**:
- 모든 서비스 로그 중앙 수집
- 실시간 로그 분석
- 성능 메트릭 수집
- 자동 알림 시스템
- 웹 대시보드 제공

**저장 방식**:
- 파일 기반 저장
- 로그 레벨별 보존 정책
- 서비스별 로그 분리

---

## 🔌 포트 할당

| 서비스 | TCP 포트 | gRPC 포트 | HTTP 포트 | 용도 |
|--------|----------|-----------|-----------|------|
| **InGame_Server** | 7777 | 5551 | - | 게임 클라이언트, 서비스 간 통신 |
| **Chat_Server** | 7778 | 5552 | - | 채팅 클라이언트, 서비스 간 통신 |
| **DB_Server** | - | 5553 | - | 데이터베이스 서비스 |
| **Log_Server** | 7779* | 5554 | 8080 | 로그 수집, 대시보드 |

> *Log_Server의 TCP 포트는 고속 로그 스트리밍용 (선택적)

---

## 🔧 서비스별 상세 가이드

> 각 서비스별 자세한 매뉴얼은 아래 링크를 참조하세요:
> - [InGame_Server 매뉴얼](./InGame_Server/INGAME_SERVER_MANUAL.md)
> - [Chat_Server 매뉴얼](./Chat_Server/CHAT_SERVER_MANUAL.md)  
> - [DB_Server 매뉴얼](./DB_Server/DB_SERVER_MANUAL.md)
> - [Log_Server 매뉴얼](./Log_Server/LOG_SERVER_MANUAL.md)

### InGame_Server

#### 주요 클래스
```csharp
// 8인 네트워크 대전
BattleRoom          - 대전방 관리
BattlePlayer        - 플레이어 상태
PlayerAction        - 플레이어 액션
BattleRoomManager   - 대전방 매니저

// 서비스 통신
LogServiceClient    - Log_Server 통신
AuthServiceClient   - DB_Server 인증 통신
ChatServiceClient   - Chat_Server 통신
```

#### 8인 네트워크 대전 플로우
```
1. 방 생성 → BattleRoom 인스턴스 생성
2. 플레이어 입장 → 최대 8명까지 입장
3. 게임 시작 → 모든 참가자에게 시작 신호
4. 실시간 동기화 → 30fps로 상태 동기화
   - 플레이어 움직임/액션 수집
   - 다른 플레이어들에게 브로드캐스트
5. 게임 종료 → 결과 DB_Server에 저장
```

### Chat_Server

#### 하이브리드 통신 구조
```csharp
// gRPC 서비스 (서비스 간 통신)
ChatGrpcService     - 서비스 간 채팅 요청 처리

// TCP 서비스 (직접 클라이언트 연결)
ChatClientSession   - TCP 채팅 클라이언트 세션
ChatTcpService      - TCP 채팅 서버
```

#### 채팅 처리 옵션
**Option 1**: gRPC 중계 (권장)
```
Client → InGame_Server → Chat_Server (gRPC) → 브로드캐스트
```

**Option 2**: TCP 직접 연결 (고성능)
```
Client → Chat_Server (TCP 7778) → 직접 브로드캐스트
```

### DB_Server

#### gRPC 서비스
```protobuf
service AuthService {
  rpc Login(LoginRequest) returns (LoginResponse);
  rpc Register(RegisterRequest) returns (RegisterResponse);
  rpc ValidateToken(ValidateTokenRequest) returns (ValidateTokenResponse);
}

service UserService {
  rpc GetUser(GetUserRequest) returns (GetUserResponse);
  rpc UpdateUser(UpdateUserRequest) returns (UpdateUserResponse);
}
```

### Log_Server

#### 로그 수집 구조
```csharp
// 로그 저장
ILogStorageService      - 저장 추상화
FileLogStorageService   - 파일 기반 저장

// 로그 처리  
LogCollectionService    - 로그 수집 및 처리
LogGrpcService         - gRPC 로그 수집 엔드포인트
```

#### 로그 보존 정책
```csharp
DEBUG   → 7일
INFO    → 30일  
WARN    → 90일
ERROR   → 1년
CRITICAL → 3년

특별 카테고리:
GamePlay → 180일
Chat     → 1년 (규정상)
Auth     → 3년 (보안상)
```

---

## 🔄 서비스 간 통신

### 통신 패턴
```
[Client] ─TCP:7777─→ [InGame_Server]
    │                      │  ↓ (logs)
    │                      ↓  ↓ 
    └─TCP:7778─→ [Chat_Server] ─gRPC─→ [Log_Server]
                      │  ↓ (logs)          ↑ (logs)
                      │  ↓                 │
                 [DB_Server] ─────────────┘
```

### 주요 통신 시나리오

#### 1. 사용자 로그인
```
Client → InGame_Server → DB_Server (인증 요청)
       ← InGame_Server ← DB_Server (인증 결과)
```

#### 2. 채팅 메시지 전송  
```
Client → InGame_Server → Chat_Server (메시지)
       → Chat_Server → DB_Server (저장)
       → Chat_Server → 브로드캐스트 (다른 클라이언트)
```

#### 3. 로그 수집
```
모든 서비스 → Log_Server (비동기 배치 전송)
           → 파일 저장 + 알림 체크
```

---

## 🚀 실행 방법

### 전체 시스템 시작

#### 1. 순서대로 실행 (권장)
```bash
# 1단계: 로깅 서비스 먼저 시작
cd Log_Server
dotnet run

# 2단계: 데이터베이스 서비스 시작
cd ../DB_Server  
dotnet run

# 3단계: 채팅 서비스 시작
cd ../Chat_Server
dotnet run

# 4단계: 게임 서비스 시작
cd ../InGame_Server
dotnet run
```

#### 2. 병렬 실행 (개발용)
각 서비스를 별도 터미널에서 동시 실행

### 개별 서비스 실행

#### InGame_Server
```bash
cd InGame_Server
dotnet run

# 출력 확인사항:
# - InGame gRPC 서버 시작됨 (포트: 5551) 
# - 소켓 서버 초기화 성공! (포트: 7777)
# - JobQueue 성능 테스트 결과
```

#### Chat_Server
```bash
cd Chat_Server
dotnet run

# 출력 확인사항:
# - Chat Server gRPC 시작됨 (포트: 5552)
# - Chat Server TCP 시작됨 (포트: 7778)
```

#### DB_Server
```bash
cd DB_Server
dotnet run

# 출력 확인사항:
# - DB Server 시작됨 (gRPC 포트: 5553)
# - 데이터베이스 초기화 완료
```

#### Log_Server
```bash
cd Log_Server
dotnet run

# 출력 확인사항:
# - Log Server 시작됨:
#   - gRPC 포트: 5554
#   - HTTP API 포트: 8080
```

### 상태 확인 방법

#### 1. 서비스 헬스 체크
```bash
# gRPC 서비스 상태 확인 (grpcurl 필요)
grpcurl -plaintext localhost:5551 list
grpcurl -plaintext localhost:5552 list  
grpcurl -plaintext localhost:5553 list
grpcurl -plaintext localhost:5554 list
```

#### 2. 로그 대시보드
```
http://localhost:8080/dashboard
```

#### 3. 네트워크 연결 확인
```bash
# 포트 사용 현황 확인
netstat -an | findstr "777"
netstat -an | findstr "555"
netstat -an | findstr "8080"
```

---

## 💻 개발 가이드

### 새로운 기능 추가

#### InGame_Server에 새 게임 모드 추가
```csharp
// 1. Modules/GamePlay/ 하위에 새 폴더 생성
// 2. 게임 모드별 클래스 구현
public class NewGameMode
{
    // 게임 모드 로직 구현
}

// 3. Program.cs에 서비스 등록
builder.Services.AddScoped<NewGameMode>();
```

#### Chat_Server에 새 채팅 타입 추가
```csharp
// 1. 채팅 타입 enum 확장 (Shared/Models/ChatRoom.cs)
public enum ChatRoomType
{
    Lobby,
    GameRoom,
    OutdoorActivity,
    Private,
    NewChatType  // 추가
}

// 2. 해당 채팅 처리 로직 구현
```

#### DB_Server에 새 데이터 모델 추가
```csharp
// 1. Entity 클래스 생성 (Data/Entities/)
public class NewEntity
{
    public string Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    // 추가 필드들
}

// 2. DbContext에 추가 (Data/Context/GameDbContext.cs)
public DbSet<NewEntity> NewEntities { get; set; }

// 3. 마이그레이션 생성 및 적용
dotnet ef migrations add AddNewEntity
dotnet ef database update

// 4. gRPC 서비스 확장
```

### 공통 모델 수정

#### Shared 프로젝트 변경 시 주의사항
```bash
# 1. Shared 프로젝트 먼저 빌드
cd Shared
dotnet build

# 2. 의존하는 모든 프로젝트 재빌드  
cd ..
dotnet build
```

### gRPC Proto 파일 수정
```bash
# proto 파일 변경 후 재생성
dotnet build [ProjectName]

# 변경사항이 반영 안 되면 clean 후 rebuild
dotnet clean [ProjectName]
dotnet build [ProjectName]
```

---

## 📊 모니터링 및 로깅

### 로그 레벨 가이드

#### InGame_Server 로그 카테고리
```csharp
// GamePlay 로그
await _logClient.LogAsync("INFO", "8인 대전 시작", "GamePlay", new Dictionary<string, object>
{
    ["RoomId"] = roomId,
    ["PlayerCount"] = 8
});

// Performance 로그  
await _logClient.LogAsync("WARN", "높은 지연시간 감지", "Performance", new Dictionary<string, object>
{
    ["Latency"] = latencyMs,
    ["Threshold"] = 100
});

// Security 로그
await _logClient.LogAsync("ERROR", "의심스러운 패킷 감지", "Security", new Dictionary<string, object>
{
    ["UserId"] = userId,
    ["PacketType"] = packetType
});
```

### 대시보드 활용

#### Log_Server 웹 대시보드 (http://localhost:8080)
```
/overview          - 전체 서비스 상태
/ingame           - 게임 서버 모니터링  
/chat             - 채팅 서버 모니터링
/database         - DB 서버 모니터링
/performance      - 성능 메트릭
/security         - 보안 이벤트
/alerts           - 실시간 알림
```

### 알림 설정

#### 자동 알림 트리거
```csharp
// 성능 관련
CPU 사용률 > 80% (5분간)
메모리 사용률 > 90% (3분간)  
응답시간 > 1000ms

// 게임 관련
동시 접속 해제 > 100명/분
치팅 의심 행동 감지
서버 다운

// 보안 관련
로그인 실패 시도 > 50회/분
SQL 인젝션 시도 감지
비인가 API 호출
```

---

## 🔍 트러블슈팅

### 자주 발생하는 문제들

#### 1. 서비스 시작 실패

**문제**: 포트 이미 사용 중
```bash
# 해결방법: 포트 사용 프로세스 종료
netstat -ano | findstr :7777
taskkill /PID [PID번호] /F
```

**문제**: gRPC 연결 실패
```csharp
// 해결방법: 서비스 시작 순서 확인
// 1. Log_Server → 2. DB_Server → 3. Chat_Server → 4. InGame_Server
```

#### 2. 서비스 간 통신 오류

**문제**: gRPC 호출 타임아웃
```csharp
// 해결방법: 연결 설정 확인
var channel = GrpcChannel.ForAddress("https://localhost:5553", new GrpcChannelOptions
{
    HttpHandler = new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    }
});
```

**문제**: 인증 서비스 응답 없음
```bash
# 해결방법: DB_Server 상태 확인
grpcurl -plaintext localhost:5553 list  # gRPC 서비스 목록 확인
# MySQL 연결 상태 확인 (DB_Server 로그 참조)
```

#### 3. 빌드 오류

**문제**: Shared 프로젝트 참조 오류
```bash
# 해결방법: 프로젝트 참조 재구성
dotnet remove reference ../Shared/Shared.csproj
dotnet add reference ../Shared/Shared.csproj
dotnet restore
```

**문제**: Proto 파일 컴파일 오류
```bash
# 해결방법: 캐시 정리 후 재빌드
dotnet clean
rm -rf obj/ bin/
dotnet build
```

#### 4. 런타임 오류

**문제**: 8인 대전 동기화 지연
```csharp
// 해결방법: JobQueue 성능 최적화
// BattleRoom.cs의 프레임 타임 조정
var targetFrameTime = TimeSpan.FromMilliseconds(16.67); // 60fps로 변경
```

**문제**: 채팅 메시지 유실
```csharp
// 해결방법: TCP 연결 상태 확인 및 재연결 로직 추가
public async Task<bool> EnsureConnectionAsync()
{
    if (_tcpClient?.Connected != true)
    {
        await ReconnectAsync();
    }
    return _tcpClient?.Connected == true;
}
```

### 성능 최적화

#### InGame_Server 최적화
```csharp
// 1. ObjectPool 사용
private readonly ObjectPool<PlayerAction> _actionPool;

// 2. 메모리 풀링
private static readonly ArrayPool<byte> BytePool = ArrayPool<byte>.Shared;

// 3. 비동기 처리 최적화
await Task.WhenAll(
    LogAsync(playerAction),
    BroadcastAsync(playerAction),
    UpdateStatsAsync(playerAction)
);
```

#### Chat_Server 최적화
```csharp
// 1. 연결 풀링
private readonly ConcurrentDictionary<string, ChatClientSession> _sessions;

// 2. 메시지 배치 처리
private readonly Channel<ChatMessage> _messageQueue;

// 3. 브로드캐스트 최적화
await Parallel.ForEachAsync(targetSessions, async (session, ct) =>
{
    await session.SendAsync(message, ct);
});
```

---

## 📚 추가 자료

### 관련 문서
- [CLAUDE.md](./CLAUDE.md) - 기본 개발 가이드
- **서비스별 상세 매뉴얼**:
  - [InGame_Server 매뉴얼](./InGame_Server/INGAME_SERVER_MANUAL.md) - 8인 네트워크 대전 및 게임 로직
  - [Chat_Server 매뉴얼](./Chat_Server/CHAT_SERVER_MANUAL.md) - 하이브리드 채팅 시스템
  - [DB_Server 매뉴얼](./DB_Server/DB_SERVER_MANUAL.md) - MySQL + EF Core 데이터베이스
  - [Log_Server 매뉴얼](./Log_Server/LOG_SERVER_MANUAL.md) - 중앙집중식 로깅

### 외부 의존성
- .NET 8.0
- gRPC (Grpc.AspNetCore 2.62.0)
- Entity Framework Core 8.0.0
- MySQL (Pomelo.EntityFrameworkCore.MySql)
- Serilog (로깅)
- Protocol Buffers (protobuf)

### 개발 도구 권장사항
- Visual Studio 2022 또는 VS Code
- Postman (gRPC 테스트용)
- Docker (컨테이너 배포시)
- Kubernetes (오케스트레이션)

---

## 🔄 버전 히스토리

### v1.0.0 (현재)
- ✅ 4개 마이크로서비스 분리 완료
- ✅ 8인 네트워크 대전 구현
- ✅ 하이브리드 채팅 시스템
- ✅ 통합 로깅 시스템
- ✅ gRPC 서비스 간 통신

### 향후 계획
- [ ] 서비스 디스커버리 (Consul/etcd)
- [ ] API Gateway 구현
- [ ] 컨테이너 배포 (Docker)
- [ ] 오케스트레이션 (Kubernetes)
- [ ] 분산 트레이싱 (Jaeger)
- [ ] 메시지 큐 (RabbitMQ/Kafka)

---

## 💬 문의 및 지원

개발 관련 문의사항이나 버그 리포트는 다음을 통해 연락해주세요:

- GitHub Issues (저장소 설정 시)
- 개발팀 슬랙 채널
- 이메일: [개발팀 이메일]

---

*이 문서는 M1_Services 마이크로서비스 아키텍처의 완전한 가이드입니다. 궁금한 사항이나 개선 제안이 있으시면 언제든 연락해주세요.*