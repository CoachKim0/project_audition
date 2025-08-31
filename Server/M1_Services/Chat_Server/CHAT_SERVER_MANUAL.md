# Chat_Server 매뉴얼

> 하이브리드 채팅 서비스 - TCP + gRPC 동시 지원

## 📋 목차
- [서비스 개요](#서비스-개요)
- [하이브리드 아키텍처](#하이브리드-아키텍처)
- [기술 스택](#기술-스택)
- [채팅 타입별 가이드](#채팅-타입별-가이드)
- [API 엔드포인트](#api-엔드포인트)
- [설정 및 실행](#설정-및-실행)
- [개발 가이드](#개발-가이드)
- [성능 최적화](#성능-최적화)
- [트러블슈팅](#트러블슈팅)

---

## 🎯 서비스 개요

### 역할
- **실시간 채팅 서비스 제공**
- **로비, 게임방, 야외활동 채팅 지원**
- **메시지 필터링 및 검열**
- **브로드캐스트 및 멀티캐스트**
- **채팅방 관리**
- **스팸 및 어뷰징 방지**

### 서비스 포트
- **TCP**: 7778 (직접 클라이언트 연결)
- **gRPC**: 5552 (서비스 간 통신)

### 특징
- **하이브리드 프로토콜**: TCP와 gRPC 동시 지원
- **고성능 실시간 통신**: TCP 직접 연결로 지연 최소화
- **확장 가능한 구조**: 서비스 간 gRPC 통신
- **메시지 필터링**: 욕설, 스팸 자동 차단

---

## 🏗️ 하이브리드 아키텍처

### 통신 방식 선택 가이드

#### Option 1: gRPC 중계 방식 (권장)
```
[Client] → [InGame_Server] → [Chat_Server(gRPC)] → [브로드캐스트]
```
**장점:**
- 서비스 간 일관성 유지
- 중앙 집중식 로깅
- 보안 강화 (토큰 검증)

**단점:**
- 한 단계 더 거치는 지연
- InGame_Server 부하 증가

#### Option 2: TCP 직접 연결 (고성능)
```
[Chat Client] → [Chat_Server(TCP:7778)] → [직접 브로드캐스트]
```
**장점:**
- 최소 지연 (Low Latency)
- InGame_Server 부하 분산
- 직접적인 채팅 경험

**단점:**
- 별도 클라이언트 구현 필요
- 보안 관리 복잡도 증가

### 하이브리드 구조 장점
```csharp
// 상황에 따른 최적 선택
if (requireLowLatency && isFrequentChat)
{
    UseTcpDirectConnection();  // 실시간 게임 채팅
}
else 
{
    UseGrpcRelay();           // 일반 로비 채팅
}
```

---

## 🛠️ 기술 스택

### 네트워크 통신
```xml
<PackageReference Include="Grpc.AspNetCore" Version="2.62.0" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
```

### 사용 라이브러리
- **ServerCore**: TCP 소켓 통신 (기존 Core 라이브러리)
- **ASP.NET Core**: gRPC 호스팅
- **Shared**: 공통 모델 및 계약

### 메시지 처리
- **비동기 I/O**: SocketAsyncEventArgs 기반
- **메모리 풀링**: 버퍼 재사용으로 GC 압박 최소화
- **스레드 안전**: ConcurrentDictionary를 통한 세션 관리

---

## 💬 채팅 타입별 가이드

### 1. 로비 채팅 (Lobby Chat)
**특징:**
- 접속한 모든 사용자 참여
- 일반적인 대화
- 적당한 메시지 필터링

**구현 위치:**
```
Chat_Server/Modules/Lobby/Chat/LobbyChat.cs
```

**사용 예시:**
```csharp
// 로비 채팅 브로드캐스트
await _chatService.BroadcastToLobbyAsync("전체공지", "시스템 점검 예정");
```

### 2. 게임방 채팅 (GameRoom Chat)
**특징:**
- 특정 게임방 참여자만 참여
- 게임 관련 소통
- 실시간 전략 논의

**구현 위치:**
```
Chat_Server/Modules/GameRoom/Chat/ (새로 추가)
```

**사용 예시:**
```csharp
// 게임방 내 채팅
await _chatService.SendToGameRoomAsync(roomId, userId, "좋은 게임이었습니다!");
```

### 3. 야외활동 채팅 (OutdoorActivity Chat)
**특징:**
- 야외활동 참여자 전용
- 위치 기반 채팅
- 이벤트 관련 소통

**구현 위치:**
```
Chat_Server/Modules/OutdoorActivity/Chat/OutdoorChat.cs
```

### 4. 개인 메시지 (Private Message)
**특징:**
- 1:1 개인 대화
- 높은 보안 수준
- 메시지 암호화 (추후 구현)

---

## 🔌 API 엔드포인트

### gRPC 서비스 정의

#### 1. 채팅 메시지 전송
```protobuf
service ChatService {
  rpc SendMessage(SendMessageRequest) returns (SendMessageResponse);
  rpc BroadcastToRoom(BroadcastRequest) returns (BroadcastResponse);
  rpc GetChatHistory(GetChatHistoryRequest) returns (GetChatHistoryResponse);
  rpc JoinRoom(JoinRoomRequest) returns (JoinRoomResponse);
  rpc LeaveRoom(LeaveRoomRequest) returns (LeaveRoomResponse);
}

message SendMessageRequest {
  string room_id = 1;
  string user_id = 2;
  string message = 3;
  string message_type = 4; // "lobby", "game", "outdoor", "private"
}

message SendMessageResponse {
  bool success = 1;
  string message = 2;
  int64 timestamp = 3;
}
```

#### 2. 채팅방 관리
```protobuf
message JoinRoomRequest {
  string room_id = 1;
  string user_id = 2;
  string room_type = 3;
}

message BroadcastRequest {
  string room_id = 1;
  string message = 2;
  string sender_id = 3; // 시스템 메시지의 경우 "SYSTEM"
}
```

### TCP 프로토콜 정의

#### 메시지 구조
```csharp
public class ChatPacket
{
    public ChatPacketType Type { get; set; }
    public string RoomId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object>? Metadata { get; set; }
}

public enum ChatPacketType
{
    ChatMessage = 1,
    JoinRoom = 2,
    LeaveRoom = 3,
    SystemNotice = 4,
    PrivateMessage = 5
}
```

---

## ⚙️ 설정 및 실행

### 1. 서비스 구성 파일

#### appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "ChatServer": "Debug",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ChatSettings": {
    "MaxRoomSize": 100,
    "MaxMessageLength": 500,
    "MessageRateLimit": 10,
    "EnableMessageFilter": true,
    "FilterWords": ["욕설1", "스팸키워드"]
  },
  "NetworkSettings": {
    "TcpPort": 7778,
    "GrpcPort": 5552,
    "MaxConnections": 1000,
    "HeartbeatInterval": 30
  }
}
```

### 2. 서비스 실행

#### 개발 환경 실행
```bash
cd Chat_Server
dotnet run

# 출력 확인사항:
# Chat Server gRPC 시작됨 (포트: 5552)
# Chat Server TCP 시작됨 (포트: 7778)
# Chat Server 실행 중...
```

#### Docker 실행
```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY . .
EXPOSE 5552 7778
ENTRYPOINT ["dotnet", "Chat_Server.dll"]
```

```bash
docker build -t chat-server .
docker run -p 5552:5552 -p 7778:7778 chat-server
```

---

## 💻 개발 가이드

### 1. 새로운 채팅 타입 추가

#### Step 1: 채팅 타입 정의
```csharp
// Shared/Models/ChatRoom.cs에 추가
public enum ChatRoomType
{
    Lobby,
    GameRoom,
    OutdoorActivity,
    Private,
    Guild,        // 새로 추가
    Tournament    // 새로 추가
}
```

#### Step 2: 채팅 핸들러 구현
```csharp
// Chat_Server/Modules/Guild/GuildChat.cs
namespace ChatServer.Modules.Guild;

public class GuildChat : IChatModule
{
    public async Task<bool> SendMessageAsync(string guildId, string userId, string message)
    {
        // 길드 멤버 검증
        if (!await ValidateGuildMemberAsync(guildId, userId))
            return false;

        // 길드 전용 메시지 필터링
        var filteredMessage = await ApplyGuildFilterAsync(message);

        // 길드 멤버들에게 브로드캐스트
        await BroadcastToGuildMembersAsync(guildId, userId, filteredMessage);
        
        return true;
    }

    private async Task BroadcastToGuildMembersAsync(string guildId, string userId, string message)
    {
        var guildMembers = await GetGuildMembersAsync(guildId);
        
        var chatPacket = new ChatPacket
        {
            Type = ChatPacketType.GuildMessage,
            RoomId = guildId,
            UserId = userId,
            Message = message,
            Timestamp = DateTime.UtcNow
        };

        await Parallel.ForEachAsync(guildMembers, async (member, ct) =>
        {
            await member.Session?.SendAsync(chatPacket, ct);
        });
    }
}
```

### 2. 메시지 필터링 시스템

#### 필터 인터페이스
```csharp
// Chat_Server/Services/IMessageFilter.cs
public interface IMessageFilter
{
    Task<string> FilterMessageAsync(string message, string userId, string roomId);
    Task<bool> IsMessageAllowedAsync(string message, string userId);
    Task<FilterResult> AnalyzeMessageAsync(string message);
}

public class FilterResult
{
    public bool IsAllowed { get; set; }
    public string FilteredMessage { get; set; } = string.Empty;
    public List<string> ViolationReasons { get; set; } = new();
    public FilterSeverity Severity { get; set; }
}

public enum FilterSeverity
{
    None,
    Warning,
    Block,
    Ban
}
```

#### 필터 구현체
```csharp
// Chat_Server/Services/MessageFilterService.cs
public class MessageFilterService : IMessageFilter
{
    private readonly IConfiguration _config;
    private readonly ILogger<MessageFilterService> _logger;
    private readonly HashSet<string> _bannedWords;

    public MessageFilterService(IConfiguration config, ILogger<MessageFilterService> logger)
    {
        _config = config;
        _logger = logger;
        _bannedWords = new HashSet<string>(_config.GetSection("ChatSettings:FilterWords").Get<string[]>() ?? Array.Empty<string>());
    }

    public async Task<string> FilterMessageAsync(string message, string userId, string roomId)
    {
        // 욕설 필터링
        var filteredMessage = FilterProfanity(message);
        
        // 스팸 검사
        if (await IsSpamAsync(message, userId))
        {
            _logger.LogWarning("스팸 메시지 차단: UserId={UserId}, Message={Message}", userId, message);
            return "[스팸으로 차단된 메시지]";
        }

        // URL 필터링
        filteredMessage = FilterUrls(filteredMessage);

        return filteredMessage;
    }

    private string FilterProfanity(string message)
    {
        foreach (var bannedWord in _bannedWords)
        {
            message = message.Replace(bannedWord, new string('*', bannedWord.Length), StringComparison.OrdinalIgnoreCase);
        }
        return message;
    }

    private async Task<bool> IsSpamAsync(string message, string userId)
    {
        // Redis를 통한 사용자별 메시지 빈도 체크 (추후 구현)
        // 현재는 단순한 길이 체크
        return message.Length > 500 || message.Count(c => c == '!') > 10;
    }
}
```

### 3. TCP 클라이언트 세션 관리

#### 채팅 세션 클래스
```csharp
// Chat_Server/Session/ChatClientSession.cs
public class ChatClientSession : Session
{
    public string UserId { get; set; } = string.Empty;
    public string CurrentRoomId { get; set; } = string.Empty;
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    public ChatClientState State { get; set; } = ChatClientState.Connected;

    private readonly IMessageFilter _messageFilter;
    private readonly ILogger<ChatClientSession> _logger;

    public ChatClientSession(IMessageFilter messageFilter, ILogger<ChatClientSession> logger)
    {
        _messageFilter = messageFilter;
        _logger = logger;
    }

    public override void OnConnected(EndPoint endPoint)
    {
        _logger.LogInformation("채팅 클라이언트 연결: {EndPoint}", endPoint);
        State = ChatClientState.Connected;
        LastActivity = DateTime.UtcNow;
    }

    public override void OnDisconnected(EndPoint endPoint)
    {
        _logger.LogInformation("채팅 클라이언트 연결 해제: {EndPoint}, UserId: {UserId}", endPoint, UserId);
        
        // 현재 채팅방에서 나가기 처리
        if (!string.IsNullOrEmpty(CurrentRoomId))
        {
            LeaveRoom(CurrentRoomId);
        }

        State = ChatClientState.Disconnected;
    }

    public override int OnRecv(ArraySegment<byte> buffer)
    {
        try
        {
            // 패킷 파싱 및 처리
            var packet = ParseChatPacket(buffer);
            ProcessChatPacket(packet);
            
            LastActivity = DateTime.UtcNow;
            return buffer.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "채팅 패킷 처리 중 오류 발생");
            return 0;
        }
    }

    private async void ProcessChatPacket(ChatPacket packet)
    {
        switch (packet.Type)
        {
            case ChatPacketType.ChatMessage:
                await HandleChatMessage(packet);
                break;
            case ChatPacketType.JoinRoom:
                await HandleJoinRoom(packet);
                break;
            case ChatPacketType.LeaveRoom:
                HandleLeaveRoom(packet);
                break;
        }
    }

    private async Task HandleChatMessage(ChatPacket packet)
    {
        // 메시지 필터링
        var filteredMessage = await _messageFilter.FilterMessageAsync(packet.Message, packet.UserId, packet.RoomId);
        
        // 브로드캐스트
        await BroadcastToRoom(packet.RoomId, new ChatPacket
        {
            Type = ChatPacketType.ChatMessage,
            RoomId = packet.RoomId,
            UserId = packet.UserId,
            Message = filteredMessage,
            Timestamp = DateTime.UtcNow
        });
    }
}

public enum ChatClientState
{
    Connected,
    Authenticated,
    InRoom,
    Disconnected
}
```

---

## 🚀 성능 최적화

### 1. 연결 풀링 및 세션 관리

#### 효율적인 세션 관리
```csharp
// Chat_Server/Services/ChatSessionManager.cs
public class ChatSessionManager
{
    private readonly ConcurrentDictionary<string, ChatClientSession> _sessions = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _roomSessions = new();
    private readonly Timer _cleanupTimer;

    public ChatSessionManager()
    {
        // 5분마다 비활성 세션 정리
        _cleanupTimer = new Timer(CleanupInactiveSessions, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public void AddSession(string sessionId, ChatClientSession session)
    {
        _sessions.TryAdd(sessionId, session);
    }

    public void RemoveSession(string sessionId)
    {
        if (_sessions.TryRemove(sessionId, out var session))
        {
            // 모든 채팅방에서 제거
            RemoveFromAllRooms(sessionId);
        }
    }

    public async Task BroadcastToRoomAsync(string roomId, ChatPacket packet)
    {
        if (!_roomSessions.TryGetValue(roomId, out var sessionIds))
            return;

        var tasks = sessionIds.Select(async sessionId =>
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                await session.SendAsync(packet);
            }
        });

        await Task.WhenAll(tasks);
    }

    private void CleanupInactiveSessions(object? state)
    {
        var cutoffTime = DateTime.UtcNow.AddMinutes(-10);
        var inactiveSessions = _sessions.Values
            .Where(s => s.LastActivity < cutoffTime)
            .Select(s => s.SessionId)
            .ToList();

        foreach (var sessionId in inactiveSessions)
        {
            RemoveSession(sessionId);
        }
    }
}
```

### 2. 메시지 배치 처리

#### 메시지 큐를 통한 배치 전송
```csharp
// Chat_Server/Services/MessageBatchProcessor.cs
public class MessageBatchProcessor
{
    private readonly Channel<ChatPacket> _messageQueue;
    private readonly Timer _batchTimer;
    private readonly List<ChatPacket> _currentBatch = new();
    private readonly object _batchLock = new object();

    public MessageBatchProcessor()
    {
        var options = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };

        _messageQueue = Channel.CreateBounded<ChatPacket>(options);

        // 100ms마다 배치 처리
        _batchTimer = new Timer(ProcessBatch, null, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100));

        // 백그라운드에서 메시지 처리
        _ = Task.Run(ProcessMessagesAsync);
    }

    public async Task QueueMessageAsync(ChatPacket packet)
    {
        await _messageQueue.Writer.WriteAsync(packet);
    }

    private async Task ProcessMessagesAsync()
    {
        await foreach (var packet in _messageQueue.Reader.ReadAllAsync())
        {
            lock (_batchLock)
            {
                _currentBatch.Add(packet);
            }
        }
    }

    private async void ProcessBatch(object? state)
    {
        List<ChatPacket> batch;
        lock (_batchLock)
        {
            if (_currentBatch.Count == 0) return;
            
            batch = new List<ChatPacket>(_currentBatch);
            _currentBatch.Clear();
        }

        // 방별로 그룹화하여 처리
        var roomGroups = batch.GroupBy(p => p.RoomId);
        
        await Parallel.ForEachAsync(roomGroups, async (group, ct) =>
        {
            await BroadcastBatchToRoom(group.Key, group.ToList());
        });
    }
}
```

### 3. 메모리 최적화

#### 객체 풀링 사용
```csharp
// Chat_Server/Pools/ChatPacketPool.cs
public class ChatPacketPool
{
    private static readonly ObjectPool<ChatPacket> _pool = new DefaultObjectPool<ChatPacket>(new DefaultPooledObjectPolicy<ChatPacket>());

    public static ChatPacket Get()
    {
        var packet = _pool.Get();
        // 초기화
        packet.Reset();
        return packet;
    }

    public static void Return(ChatPacket packet)
    {
        _pool.Return(packet);
    }
}

public static class ChatPacketExtensions
{
    public static void Reset(this ChatPacket packet)
    {
        packet.Type = ChatPacketType.ChatMessage;
        packet.RoomId = string.Empty;
        packet.UserId = string.Empty;
        packet.Message = string.Empty;
        packet.Timestamp = DateTime.UtcNow;
        packet.Metadata?.Clear();
    }
}
```

---

## 🔍 트러블슈팅

### 1. TCP 연결 문제

#### 포트 이미 사용 중
```
SocketException: Only one usage of each socket address (protocol/network address/port) is normally permitted
```

**해결 방법:**
```bash
# 포트 사용 프로세스 확인
netstat -ano | findstr :7778

# 프로세스 종료
taskkill /PID [PID] /F

# 또는 다른 포트 사용
"NetworkSettings": {
  "TcpPort": 7779  // 포트 변경
}
```

#### 연결 끊김 문제
```csharp
// 하트비트 구현
public class HeartbeatService
{
    private readonly Timer _heartbeatTimer;
    
    public HeartbeatService()
    {
        _heartbeatTimer = new Timer(SendHeartbeat, null, 
            TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }
    
    private async void SendHeartbeat(object? state)
    {
        var heartbeatPacket = new ChatPacket
        {
            Type = ChatPacketType.Heartbeat,
            Timestamp = DateTime.UtcNow
        };
        
        await BroadcastToAllAsync(heartbeatPacket);
    }
}
```

### 2. gRPC 통신 오류

#### 서비스 등록 확인
```csharp
// Program.cs에서 확인
app.MapGrpcService<ChatGrpcService>();

// proto 파일 컴파일 확인
dotnet build
```

#### 연결 시간 초과
```csharp
// 클라이언트 옵션 설정
var channel = GrpcChannel.ForAddress("https://localhost:5552", new GrpcChannelOptions
{
    MaxReceiveMessageSize = 4 * 1024 * 1024, // 4MB
    MaxSendMessageSize = 4 * 1024 * 1024,
    KeepAliveTime = TimeSpan.FromSeconds(30),
    KeepAliveTimeout = TimeSpan.FromSeconds(5)
});
```

### 3. 메시지 손실 문제

#### 메시지 전송 확인
```csharp
public async Task<bool> SendMessageWithConfirmation(ChatClientSession session, ChatPacket packet)
{
    try
    {
        await session.SendAsync(packet);
        
        // ACK 대기 (선택적)
        var ackReceived = await WaitForAcknowledgment(session.SessionId, packet.Id, TimeSpan.FromSeconds(5));
        
        return ackReceived;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "메시지 전송 실패: SessionId={SessionId}", session.SessionId);
        return false;
    }
}
```

#### 메시지 재전송 로직
```csharp
public class ReliableMessageService
{
    private readonly ConcurrentDictionary<string, PendingMessage> _pendingMessages = new();
    private readonly Timer _retryTimer;

    public ReliableMessageService()
    {
        _retryTimer = new Timer(RetryFailedMessages, null, 
            TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
    }

    public async Task SendReliableMessage(string sessionId, ChatPacket packet)
    {
        var messageId = Guid.NewGuid().ToString();
        packet.Id = messageId;

        var pendingMessage = new PendingMessage
        {
            Id = messageId,
            SessionId = sessionId,
            Packet = packet,
            Timestamp = DateTime.UtcNow,
            RetryCount = 0
        };

        _pendingMessages.TryAdd(messageId, pendingMessage);
        await SendMessageAsync(sessionId, packet);
    }

    public void ConfirmMessage(string messageId)
    {
        _pendingMessages.TryRemove(messageId, out _);
    }

    private async void RetryFailedMessages(object? state)
    {
        var expiredMessages = _pendingMessages.Values
            .Where(m => DateTime.UtcNow - m.Timestamp > TimeSpan.FromSeconds(30) && m.RetryCount < 3)
            .ToList();

        foreach (var message in expiredMessages)
        {
            message.RetryCount++;
            await SendMessageAsync(message.SessionId, message.Packet);
            
            if (message.RetryCount >= 3)
            {
                _pendingMessages.TryRemove(message.Id, out _);
            }
        }
    }
}
```

---

## 📊 모니터링 및 분석

### 1. 채팅 통계 수집
```csharp
// Chat_Server/Services/ChatAnalyticsService.cs
public class ChatAnalyticsService
{
    private readonly ILogger<ChatAnalyticsService> _logger;
    private readonly ConcurrentDictionary<string, ChatRoomStats> _roomStats = new();

    public void RecordMessage(string roomId, string userId, int messageLength)
    {
        var stats = _roomStats.GetOrAdd(roomId, _ => new ChatRoomStats { RoomId = roomId });
        
        Interlocked.Increment(ref stats.MessageCount);
        Interlocked.Add(ref stats.TotalCharacters, messageLength);
        stats.ActiveUsers.TryAdd(userId, DateTime.UtcNow);
    }

    public ChatRoomStats GetRoomStats(string roomId)
    {
        return _roomStats.GetValueOrDefault(roomId, new ChatRoomStats { RoomId = roomId });
    }

    public async Task GenerateHourlyReport()
    {
        var report = new ChatAnalyticsReport
        {
            Timestamp = DateTime.UtcNow,
            TotalRooms = _roomStats.Count,
            TotalMessages = _roomStats.Values.Sum(s => s.MessageCount),
            TotalActiveUsers = _roomStats.Values.SelectMany(s => s.ActiveUsers.Keys).Distinct().Count()
        };

        _logger.LogInformation("채팅 통계 리포트: {@Report}", report);
    }
}

public class ChatRoomStats
{
    public string RoomId { get; set; } = string.Empty;
    public long MessageCount { get; set; }
    public long TotalCharacters { get; set; }
    public ConcurrentDictionary<string, DateTime> ActiveUsers { get; set; } = new();
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
}
```

### 2. 실시간 대시보드 API
```csharp
// Chat_Server/Controllers/ChatStatsController.cs
[ApiController]
[Route("api/[controller]")]
public class ChatStatsController : ControllerBase
{
    private readonly ChatAnalyticsService _analyticsService;
    private readonly ChatSessionManager _sessionManager;

    [HttpGet("rooms")]
    public IActionResult GetRoomStats()
    {
        var stats = _analyticsService.GetAllRoomStats();
        return Ok(stats);
    }

    [HttpGet("connections")]
    public IActionResult GetConnectionStats()
    {
        var stats = new
        {
            TotalConnections = _sessionManager.GetTotalConnections(),
            ActiveRooms = _sessionManager.GetActiveRoomCount(),
            MessagesPerMinute = _analyticsService.GetMessagesPerMinute()
        };
        return Ok(stats);
    }

    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });
    }
}
```

---

*이 매뉴얼은 Chat_Server의 완전한 가이드입니다. 하이브리드 아키텍처를 활용하여 상황에 맞는 최적의 채팅 경험을 제공할 수 있습니다.*