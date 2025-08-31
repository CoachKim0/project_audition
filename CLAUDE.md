# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 개발 명령어

### 솔루션 빌드
```bash
# 전체 솔루션 빌드
dotnet build

# 특정 서비스 빌드
dotnet build InGame_Server/InGame_Server.csproj
dotnet build DB_Server/DB_Server.csproj
dotnet build Chat_Server/Chat_Server.csproj
dotnet build Log_Server/Log_Server.csproj
```

### 서버 실행
서비스 간 의존성으로 인해 순서대로 실행해야 합니다:

```bash
# 1. Log_Server 먼저 시작 (포트 5554, 8080)
cd Log_Server
dotnet run

# 2. DB_Server 시작 (포트 5553) - MySQL 필요
cd ../DB_Server
dotnet run

# 3. Chat_Server 시작 (포트 5552, 7778)
cd ../Chat_Server
dotnet run

# 4. InGame_Server 시작 (포트 5551, 7777)
cd ../InGame_Server
dotnet run
```

### 데이터베이스 명령어 (DB_Server)
```bash
# 마이그레이션 생성
cd DB_Server
dotnet ef migrations add [마이그레이션이름]

# 마이그레이션 적용
dotnet ef database update

# 데이터베이스 삭제 (개발용)
dotnet ef database drop
```

### 클린 빌드
```bash
dotnet clean
dotnet build
```

## 아키텍처 개요

모놀리식 설계에서 발전된 C# .NET 8.0 기반의 마이크로서비스 MMO 서버 아키텍처입니다. 고성능 TCP 소켓 통신과 서비스 간 gRPC 통신을 구현합니다.

### 마이크로서비스 구조
```
M1_Services/
├── InGame_Server/      # 게임 로직 + 8인 네트워크 대전 (포트 7777, 5551)
├── Chat_Server/        # 하이브리드 TCP/gRPC 채팅 시스템 (포트 7778, 5552)
├── DB_Server/          # MySQL + EF Core 데이터베이스 서비스 (포트 5553)
├── Log_Server/         # 중앙집중식 로깅 및 모니터링 (포트 5554, 8080)
├── Shared/             # 공통 모델 및 인터페이스
├── Core/               # 네트워크 코어 라이브러리 (Session, Listener, Buffer)
└── PacketGenerator/    # Protocol Buffer 코드 생성 도구
```

### 서비스 간 통신 패턴
- **InGame_Server**: 게임 로직, 플레이어 세션, 8인 실시간 대전 처리 (30fps)
- **Chat_Server**: 모든 채팅 메시지 처리, 하이브리드 TCP/gRPC 지원
- **DB_Server**: 사용자 인증, MySQL + Entity Framework를 통한 데이터 지속성
- **Log_Server**: 모든 서비스의 로그 수집, 웹 대시보드 및 알림 제공

### 핵심 컴포넌트

#### Core 네트워크 라이브러리
- **Session.cs**: SocketAsyncEventArgs를 사용한 비동기 I/O 네트워크 세션 추상 클래스
- **Listener.cs**: 팩토리 패턴을 사용한 TCP 연결 수락기
- **Connector.cs**: 서버 간 아웃바운드 연결 관리자
- **RecvBuffer/SendBuffer**: GC 압박을 줄이는 풀링을 통한 효율적인 버퍼 관리
- **JobQueue.cs**: 직렬화된 패킷 처리를 위한 스레드 안전 작업 큐

#### 서비스 간 통신
모든 마이크로서비스는 protobuf 직렬화를 사용한 gRPC로 통신합니다:
```csharp
// 예시: InGame_Server에서 DB_Server 인증 호출
var authClient = new AuthServiceClient();
bool isValid = await authClient.ValidateUserAsync(userId, token);
```

#### 실시간 8인 대전 시스템 (InGame_Server)
- **BattleRoom**: 최대 8명의 플레이어를 30fps 동기화로 관리
- **PlayerAction**: 타임스탬프 검증을 통한 움직임, 공격, 스킬 처리
- **BattleRoomManager**: 다중 동시 대전방 조정

#### 성능 최적화
- **비동기 I/O**: SocketAsyncEventArgs를 사용한 논블로킹 연산
- **버퍼 풀링**: ThreadLocal SendBufferHelper로 메모리 할당 감소
- **배치 처리**: 네트워크 오버헤드를 최소화하는 연산 그룹화
- **JobQueue**: 모든 서비스에서 스레드 안전 패킷 처리 보장

### 프로토콜 설계

#### 클라이언트 통신
- **TCP 소켓**: InGame_Server(7777), Chat_Server(7778)로 직접 클라이언트 연결
- **커스텀 바이너리 프로토콜**: PacketID + protobuf 페이로드
- **메시지 필터링**: Chat_Server에서 메시지 검증 및 검열 구현

#### 서비스 간 통신
- **gRPC**: 모든 서비스 간 호출은 HTTP/2 gRPC와 protobuf 사용
- **서비스 디스커버리**: 하드코딩된 엔드포인트 (향후: 서비스 디스커버리 구현)
- **회로 차단기 패턴**: 서비스들이 실패 시 폴백 로직으로 우아하게 처리

### 데이터 플로우 예시

#### 사용자 인증 플로우
```
클라이언트 → InGame_Server → DB_Server (gRPC) → MySQL
         ← InGame_Server ← DB_Server ← (인증 결과)
```

#### 채팅 메시지 플로우
```
클라이언트 → Chat_Server (TCP) → Log_Server (로깅)
         → Chat_Server → 다른 클라이언트들로 브로드캐스트
```

#### 대전 동기화 플로우
```
플레이어 액션 → InGame_Server → 검증 → 다른 7명 플레이어에게 브로드캐스트
              → Log_Server (성능 메트릭)
```

### 개발 참고사항

#### Shared 프로젝트 의존성
모든 서비스는 다음을 포함하는 Shared 프로젝트를 참조합니다:
- **Models**: ClientInfo, ChatRoom, UserGroup 열거형 및 데이터 구조
- **DTOs**: 중앙집중식 로깅을 위한 LogEntry
- **Contracts**: 서비스 클라이언트 인터페이스 (IAuthServiceClient, IChatServiceClient 등)

#### Entity Framework 통합 (DB_Server)
- **Code First**: C# 엔티티로 정의된 데이터베이스 스키마
- **MySQL 프로바이더**: Pomelo.EntityFrameworkCore.MySql 사용
- **마이그레이션**: `dotnet ef migrations add` 및 `dotnet ef database update` 사용
- **연결 문자열**: appsettings.json에서 설정

#### Protocol Buffer 사용
- **Proto 파일**: 각 서비스의 Protos/ 디렉토리에 정의
- **코드 생성**: 빌드 과정에서 자동 생성
- **서비스 정의**: .proto 파일에서 gRPC 서비스 정의
- **버전 관리**: proto 변경 시 하위 호환성 처리

#### 테스트
- **DumyClient**: TCP/gRPC 연결 테스트를 위한 별도 디렉토리
- **단위 테스트**: 각 서비스마다 자체 테스트 프로젝트 필요
- **통합 테스트**: 서비스 간 통신 테스트

## 서비스별 상세 정보

### InGame_Server
- **하이브리드 아키텍처**: TCP(7777)와 gRPC(5551) 서버 동시 실행
- **대전 시스템**: 30fps 동기화 루프를 가진 8인 실시간 대전
- **세션 관리**: Core/Session에서 파생된 ClientSession을 통한 클라이언트 연결 처리
- **서비스 클라이언트**: gRPC를 통해 DB_Server, Chat_Server, Log_Server 연결

### Chat_Server
- **이중 프로토콜 지원**: 직접 클라이언트용 TCP(7778), 서비스 통신용 gRPC(5552)
- **메시지 처리**: 브로드캐스트 전 채팅 메시지 필터링 및 검증
- **방 기반 채팅**: 로비, 게임방, 야외활동 채팅 타입 지원

### DB_Server
- **인증**: JWT 기반 토큰 검증 및 사용자 세션 관리
- **데이터 지속성**: 사용자 프로필, 게임 통계, 세션 데이터
- **gRPC 전용**: 직접 클라이언트 연결 없음, 서비스 간 통신만
- **Entity Framework**: 자동 마이그레이션을 사용한 Code First 접근

### Log_Server
- **중앙집중식 로깅**: gRPC를 통해 모든 서비스의 로그 수집
- **웹 대시보드**: 로그 시각화를 위한 포트 8080 HTTP 서버
- **배치 처리**: 효율적인 로그 저장 및 조회
- **알림**: 로그 패턴 및 임계값 기반 설정 가능한 알림

## 중요 파일들
- **MICROSERVICES_ARCHITECTURE.md**: 포괄적인 아키텍처 문서
- 각 서비스마다 자세한 매뉴얼 (예: DB_SERVER_MANUAL.md)
- **Shared/**: 모든 서비스에서 사용하는 공통 데이터 모델 및 서비스 계약
- **Core/**: TCP 서비스들이 공유하는 저수준 네트워킹 라이브러리