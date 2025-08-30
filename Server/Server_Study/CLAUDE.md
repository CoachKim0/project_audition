# CLAUDE.md

이 파일은 Claude Code (claude.ai/code)가 이 저장소에서 코드 작업을 할 때 필요한 가이드를 제공합니다.

## 개발 명령어

### 솔루션 빌드
```bash
# 전체 솔루션 빌드
dotnet build

# 특정 프로젝트 빌드
dotnet build Server/Server.csproj
dotnet build Core/Core.csproj
```

### 서버 실행
```bash
# 메인 서버 실행 (TCP 소켓 서버 포트 7777, gRPC 서버 포트 5554 동시 시작)
cd Server
dotnet run

# 특정 프로젝트 실행
dotnet run --project Server/Server.csproj
dotnet run --project PacketGenerator/PacketGenerator.csproj
dotnet run --project gRPC_Multi/gRPC_Multi.csproj
```

### 클린 빌드
```bash
dotnet clean
dotnet build
```

## 아키텍처 개요

TCP 소켓과 gRPC를 사용한 고성능 네트워크 통신을 구현하는 C# .NET 8.0 MMO 서버 학습 프로젝트입니다.

### 프로젝트 구조
- **Core**: Session, Listener, Connector, 버퍼 관리를 포함한 네트워크 코어 라이브러리
- **Server**: TCP 소켓(포트 7777)과 gRPC(포트 5554)를 지원하는 메인 게임 서버
- **PacketGenerator**: Protocol Buffer 코드 생성 도구
- **GrpcServer**: 독립적인 gRPC 서버 구현
- **gRPC_Multi**: 추가적인 gRPC 서버 변형
- **DumyClient**: 클라이언트 테스트 애플리케이션 (별도 디렉토리)

### 핵심 컴포넌트

#### 네트워크 레이어 (Core 프로젝트)
- **Session.cs**: 비동기 I/O를 사용하는 네트워크 세션 추상 기본 클래스
- **Listener.cs**: 팩토리 패턴을 사용하여 TCP 클라이언트 연결 수락
- **Connector.cs**: 다른 서버로의 아웃바운드 연결 설정
- **RecvBuffer.cs / SendBuffer.cs**: 풀링을 사용한 효율적인 버퍼 관리
- **JobQueue.cs**: 패킷 처리를 위한 스레드 안전 작업 큐

#### 게임 서버 (Server 프로젝트)
- **Program.cs**: 메인 진입점, TCP와 gRPC 서버 초기화
- **ClientSession.cs**: protobuf 패킷 처리를 포함한 구체적인 TCP 세션 구현
- **GameRoom.cs**: 방 기반 멀티플레이어 시스템
- **Modules/**: 기능 모듈 (Auth, Chat, Room, Ping 등)
- **Services/**: gRPC 서비스 구현 및 브로드캐스트 서비스

### 통신 프로토콜

#### TCP 소켓 통신 (포트 7777)
- protobuf 직렬화를 사용하는 커스텀 바이너리 프로토콜
- PacketID 헤더 다음에 protobuf 페이로드
- protobuf와 JSON 패킷 형식 모두 지원
- PlayerInfoReq, C_Chat, S_Chat 패킷 처리

#### gRPC 통신 (포트 5554)
- HTTP/2 기반 gRPC 서비스
- GameGrpcService와 ChatServiceImpl 엔드포인트
- 서버 간 통신 및 클라이언트 API에 사용

### 사용된 디자인 패턴
- **팩토리 패턴**: Listener/Connector에서 세션 생성
- **템플릿 메소드**: 오버라이드 가능한 메소드를 가진 추상 Session 클래스
- **객체 풀**: ThreadLocal 스토리지를 사용한 SendBufferHelper
- **옵저버 패턴**: SocketAsyncEventArgs 이벤트 처리
- **모듈 패턴**: Modules/ 디렉토리에서 기능 분리

### 개발 참고사항
- 모든 프로젝트는 .NET 8.0을 타겟팅
- 직렬화에 Protocol Buffers 사용
- SocketAsyncEventArgs를 사용한 비동기 I/O 구현
- JobQueue를 통한 스레드 안전 패킷 처리 보장
- UserManager가 사용자 인증 및 방 관리 처리
- BroadcastService가 메시지 배포 처리

### 성능 기능
- 논블로킹 비동기 I/O 연산
- GC 압박을 줄이는 버퍼 풀링
- 직렬화된 패킷 처리를 위한 JobQueue
- 스레드 안전 세션 관리
- 연결 풀링 및 재사용

### 테스트
인접 디렉토리의 DumyClient 프로젝트가 TCP와 gRPC 연결 모두에 대한 클라이언트 테스트 기능을 제공합니다.