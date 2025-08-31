# Log_Server 매뉴얼

> 통합 로깅 및 모니터링 서비스 - 중앙집중식 로그 관리

## 📋 목차
- [서비스 개요](#서비스-개요)
- [로그 수집 아키텍처](#로그-수집-아키텍처)
- [기술 스택](#기술-스택)
- [로그 레벨 및 카테고리](#로그-레벨-및-카테고리)
- [API 엔드포인트](#api-엔드포인트)
- [웹 대시보드](#웹-대시보드)
- [알림 시스템](#알림-시스템)
- [설정 및 실행](#설정-및-실행)
- [개발 가이드](#개발-가이드)
- [성능 최적화](#성능-최적화)
- [트러블슈팅](#트러블슈팅)

---

## 🎯 서비스 개요

### 역할
- **모든 서비스의 로그 중앙 수집**
- **실시간 로그 분석 및 필터링**
- **성능 메트릭 수집 및 모니터링**
- **자동 알림 및 경고 시스템**
- **웹 기반 대시보드 제공**
- **로그 검색 및 분석 도구**
- **보안 이벤트 탐지 및 알림**

### 서비스 포트
- **gRPC**: 5554 (로그 수집)
- **HTTP**: 8080 (웹 대시보드 및 API)
- **TCP**: 7779 (고속 로그 스트리밍, 선택적)

### 의존성
- 모든 마이크로서비스 (로그 소스)
- 파일 시스템 (로그 저장)
- Serilog (구조화 로깅)

---

## 🏗️ 로그 수집 아키텍처

### 수집 플로우
```
[InGame_Server] ──┐
[Chat_Server]   ──┼──→ [Log_Server] ──→ [파일 저장] ──→ [분석] ──→ [알림]
[DB_Server]     ──┘                 ──→ [대시보드]
```

### 로그 전송 방식

#### 1. 비동기 배치 전송 (권장)
```csharp
// 각 서비스에서 로그 배치 전송
var logBatch = new List<LogEntry>();
logBatch.Add(new LogEntry { Level = LogLevel.INFO, Message = "게임 시작" });
logBatch.Add(new LogEntry { Level = LogLevel.DEBUG, Message = "플레이어 입장" });

await _logClient.LogBatchAsync(logBatch);  // 100개씩 배치 전송
```

#### 2. 즉시 전송 (중요한 로그)
```csharp
// 긴급 로그는 즉시 전송
await _logClient.LogAsync(LogLevel.ERROR, "서버 크래시 발생", "Critical", new Dictionary<string, object>
{
    ["ErrorCode"] = "SRV_001",
    ["StackTrace"] = ex.StackTrace
});
```

### 로그 저장 전략

#### 파일 기반 저장 (기본)
```
logs/
├── InGame_Server_2024-01-15.log
├── Chat_Server_2024-01-15.log
├── DB_Server_2024-01-15.log
├── Critical_2024-01-15.log      // 중요 로그만
└── Performance_2024-01-15.log   // 성능 로그만
```

#### 로그 보존 정책
```csharp
public static readonly Dictionary<LogLevel, TimeSpan> RetentionPeriods = new()
{
    { LogLevel.DEBUG, TimeSpan.FromDays(7) },      // 7일
    { LogLevel.INFO, TimeSpan.FromDays(30) },      // 30일  
    { LogLevel.WARN, TimeSpan.FromDays(90) },      // 90일
    { LogLevel.ERROR, TimeSpan.FromDays(365) },    // 1년
    { LogLevel.CRITICAL, TimeSpan.FromDays(1095) } // 3년
};

// 카테고리별 특별 보존 정책
public static readonly Dictionary<string, TimeSpan> CategoryRetention = new()
{
    { "GamePlay", TimeSpan.FromDays(180) },        // 게임 플레이 로그
    { "Chat", TimeSpan.FromDays(365) },            // 채팅 로그 (규정상 1년)
    { "Auth", TimeSpan.FromDays(1095) },           // 인증 로그 (보안상 3년)
    { "Performance", TimeSpan.FromDays(90) }       // 성능 로그
};
```

---

## 🛠️ 기술 스택

### 로깅 프레임워크
```xml
<PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
<PackageReference Include="Grpc.AspNetCore" Version="2.62.0" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
```

### 분석 도구
- **Serilog**: 구조화 로깅 및 파일 저장
- **gRPC**: 고성능 로그 수집 
- **ASP.NET Core**: 웹 API 및 대시보드
- **SignalR**: 실시간 로그 스트리밍 (추후 구현)

---

## 📊 로그 레벨 및 카테고리

### 로그 레벨 정의

#### DEBUG (레벨 0)
```csharp
// 개발 디버깅용 상세 정보
await _logClient.LogAsync("DEBUG", "플레이어 위치 업데이트", "GamePlay", new Dictionary<string, object>
{
    ["UserId"] = userId,
    ["Position"] = new { X = 100, Y = 200, Z = 50 },
    ["Timestamp"] = DateTime.UtcNow
});
```

#### INFO (레벨 1)
```csharp
// 일반적인 시스템 동작 정보
await _logClient.LogAsync("INFO", "게임 세션 시작", "GamePlay", new Dictionary<string, object>
{
    ["RoomId"] = roomId,
    ["PlayerCount"] = 8,
    ["GameMode"] = "BattleRoyale"
});
```

#### WARN (레벨 2)
```csharp
// 주의가 필요한 상황
await _logClient.LogAsync("WARN", "높은 지연시간 감지", "Performance", new Dictionary<string, object>
{
    ["Latency"] = 150,
    ["Threshold"] = 100,
    ["ServerId"] = "InGame-01"
});
```

#### ERROR (레벨 3)
```csharp
// 오류 상황
await _logClient.LogAsync("ERROR", "데이터베이스 연결 실패", "Database", new Dictionary<string, object>
{
    ["ErrorCode"] = "DB_CONNECTION_FAILED",
    ["RetryCount"] = 3,
    ["Exception"] = ex.Message
});
```

#### CRITICAL (레벨 4)
```csharp
// 즉시 대응이 필요한 심각한 상황
await _logClient.LogAsync("CRITICAL", "서버 메모리 부족", "System", new Dictionary<string, object>
{
    ["MemoryUsage"] = "95%",
    ["AvailableMemory"] = "512MB",
    ["ProcessCount"] = 25
});
```

### 로그 카테고리

#### GamePlay 카테고리
```csharp
// 게임 플레이 관련 모든 로그
- 게임 시작/종료
- 플레이어 액션
- 스코어 변경  
- 아이템 사용
- 치팅 의심 행동
```

#### Chat 카테고리
```csharp
// 채팅 관련 로그
- 메시지 전송/수신
- 채팅방 입장/퇴장
- 스팸 감지
- 욕설 필터링
- 사용자 신고
```

#### Performance 카테고리
```csharp
// 성능 관련 로그
- CPU/메모리 사용률
- 네트워크 지연시간
- 데이터베이스 쿼리 시간
- 동시 접속자 수
- 처리량 통계
```

#### Security 카테고리
```csharp
// 보안 관련 로그
- 로그인 실패 시도
- 비인가 접근
- SQL 인젝션 시도
- 의심스러운 패킷
- 계정 잠금
```

---

## 🔌 API 엔드포인트

### gRPC 로그 수집 서비스

#### 1. 단일 로그 전송
```protobuf
service LogService {
  rpc LogEntry(LogRequest) returns (LogResponse);
  rpc LogBatch(LogBatchRequest) returns (LogResponse);
  rpc GetLogs(GetLogsRequest) returns (GetLogsResponse);
}

message LogRequest {
  string id = 1;
  int64 timestamp = 2;
  string service_name = 3;
  int32 level = 4;
  string category = 5;
  string message = 6;
  string user_id = 7;
  string session_id = 8;
  map<string, string> data = 9;
  string trace_id = 10;
}
```

#### 2. 배치 로그 전송
```protobuf
message LogBatchRequest {
  repeated LogRequest logs = 1;
}

message LogResponse {
  bool success = 1;
  string message = 2;
}
```

#### 3. 로그 조회
```protobuf
message GetLogsRequest {
  int64 from_timestamp = 1;
  int64 to_timestamp = 2;
  string service_filter = 3;
  int32 level_filter = 4;
  int32 limit = 5;
}

message GetLogsResponse {
  repeated LogRequest logs = 1;
  int32 total_count = 2;
}
```

### REST API (웹 대시보드용)

#### 로그 조회 API
```csharp
// GET /api/logs
[HttpGet]
public async Task<IActionResult> GetLogs(
    [FromQuery] DateTime from,
    [FromQuery] DateTime to,
    [FromQuery] string? service = null,
    [FromQuery] string? level = null,
    [FromQuery] int page = 1,
    [FromQuery] int size = 50)
{
    var logs = await _logStorageService.GetLogsAsync(from, to, service);
    var filteredLogs = ApplyFilters(logs, level);
    var pagedResult = ApplyPaging(filteredLogs, page, size);
    
    return Ok(new
    {
        Data = pagedResult,
        Total = filteredLogs.Count(),
        Page = page,
        Size = size
    });
}
```

#### 통계 API
```csharp
// GET /api/stats/summary
[HttpGet("summary")]
public async Task<IActionResult> GetLogSummary([FromQuery] DateTime date)
{
    var stats = await _analyticsService.GetDailySummaryAsync(date);
    return Ok(stats);
}

// GET /api/stats/services
[HttpGet("services")]
public async Task<IActionResult> GetServiceStats()
{
    var stats = await _analyticsService.GetServiceStatsAsync();
    return Ok(stats);
}
```

#### 알림 관리 API
```csharp
// GET /api/alerts
[HttpGet]
public async Task<IActionResult> GetActiveAlerts()
{
    var alerts = await _alertService.GetActiveAlertsAsync();
    return Ok(alerts);
}

// POST /api/alerts/acknowledge/{alertId}
[HttpPost("acknowledge/{alertId}")]
public async Task<IActionResult> AcknowledgeAlert(string alertId)
{
    await _alertService.AcknowledgeAlertAsync(alertId);
    return Ok();
}
```

---

## 🖥️ 웹 대시보드

### 대시보드 구성 (http://localhost:8080)

#### 1. 메인 대시보드 (`/`)
```html
<!DOCTYPE html>
<html>
<head>
    <title>Log Server Dashboard</title>
    <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
</head>
<body>
    <div class="dashboard">
        <div class="stats-row">
            <div class="stat-card">
                <h3>총 로그 수</h3>
                <span id="total-logs">-</span>
            </div>
            <div class="stat-card error">
                <h3>오류 로그</h3>
                <span id="error-logs">-</span>
            </div>
            <div class="stat-card warning">
                <h3>경고 로그</h3>
                <span id="warning-logs">-</span>
            </div>
        </div>
        <div class="chart-container">
            <canvas id="logs-chart"></canvas>
        </div>
        <div class="recent-logs">
            <h3>최근 로그</h3>
            <div id="log-list"></div>
        </div>
    </div>
</body>
</html>
```

#### 2. 서비스별 대시보드
```
/ingame     - InGame_Server 전용 대시보드
/chat       - Chat_Server 전용 대시보드  
/database   - DB_Server 전용 대시보드
```

#### 3. 로그 검색 (`/search`)
```html
<div class="search-container">
    <div class="search-filters">
        <select id="service-filter">
            <option value="">모든 서비스</option>
            <option value="InGame_Server">게임 서버</option>
            <option value="Chat_Server">채팅 서버</option>
            <option value="DB_Server">DB 서버</option>
        </select>
        
        <select id="level-filter">
            <option value="">모든 레벨</option>
            <option value="DEBUG">DEBUG</option>
            <option value="INFO">INFO</option>
            <option value="WARN">WARN</option>
            <option value="ERROR">ERROR</option>
            <option value="CRITICAL">CRITICAL</option>
        </select>
        
        <input type="text" id="keyword-filter" placeholder="키워드 검색">
        <button onclick="searchLogs()">검색</button>
    </div>
    
    <div id="search-results"></div>
</div>
```

#### 4. 실시간 로그 스트림 (`/live`)
```javascript
// SignalR을 통한 실시간 로그 스트리밍
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/logHub")
    .build();

connection.start().then(function () {
    console.log("실시간 로그 스트림 연결됨");
});

connection.on("NewLogEntry", function (logEntry) {
    appendLogToView(logEntry);
});

function appendLogToView(logEntry) {
    const logElement = document.createElement("div");
    logElement.className = `log-entry ${logEntry.level.toLowerCase()}`;
    logElement.innerHTML = `
        <span class="timestamp">${formatTimestamp(logEntry.timestamp)}</span>
        <span class="service">${logEntry.serviceName}</span>
        <span class="level">${logEntry.level}</span>
        <span class="message">${logEntry.message}</span>
    `;
    document.getElementById("live-logs").appendChild(logElement);
}
```

---

## 🚨 알림 시스템

### 알림 규칙 정의

#### 성능 알림
```csharp
public static class PerformanceAlerts
{
    public static readonly AlertRule HighCpuUsage = new AlertRule
    {
        Name = "High CPU Usage",
        Condition = log => log.Category == "Performance" && 
                          log.Data.ContainsKey("CpuUsage") &&
                          double.Parse(log.Data["CpuUsage"].ToString()) > 80.0,
        Severity = AlertSeverity.Warning,
        Cooldown = TimeSpan.FromMinutes(5)
    };

    public static readonly AlertRule HighMemoryUsage = new AlertRule
    {
        Name = "High Memory Usage", 
        Condition = log => log.Category == "Performance" &&
                          log.Data.ContainsKey("MemoryUsage") &&
                          double.Parse(log.Data["MemoryUsage"].ToString()) > 90.0,
        Severity = AlertSeverity.Critical,
        Cooldown = TimeSpan.FromMinutes(3)
    };
}
```

#### 보안 알림
```csharp
public static class SecurityAlerts
{
    public static readonly AlertRule FailedLoginSpike = new AlertRule
    {
        Name = "Failed Login Attempts Spike",
        Condition = log => log.Category == "Auth" && 
                          log.Level == LogLevel.WARN &&
                          log.Message.Contains("로그인 실패"),
        Threshold = 10, // 5분 내 10회
        TimeWindow = TimeSpan.FromMinutes(5),
        Severity = AlertSeverity.Warning
    };

    public static readonly AlertRule SqlInjectionAttempt = new AlertRule
    {
        Name = "SQL Injection Attempt",
        Condition = log => log.Category == "Security" &&
                          (log.Message.Contains("SQL injection") || 
                           log.Data.ContainsKey("SqlInjection")),
        Severity = AlertSeverity.Critical,
        Cooldown = TimeSpan.FromSeconds(0) // 즉시 알림
    };
}
```

### 알림 채널 구성

#### 이메일 알림
```csharp
public class EmailAlertChannel : IAlertChannel
{
    public async Task SendAlertAsync(Alert alert)
    {
        var subject = $"[{alert.Severity}] {alert.RuleName}";
        var body = $@"
알림 시간: {alert.Timestamp:yyyy-MM-dd HH:mm:ss}
서비스: {alert.ServiceName}
메시지: {alert.Message}
상세 정보: {JsonSerializer.Serialize(alert.Data)}
";

        await _emailService.SendAsync("admin@company.com", subject, body);
    }
}
```

#### Slack/Discord 웹훅
```csharp
public class SlackAlertChannel : IAlertChannel
{
    public async Task SendAlertAsync(Alert alert)
    {
        var payload = new
        {
            text = $"🚨 **{alert.Severity} Alert**",
            attachments = new[]
            {
                new
                {
                    color = GetColorBySeverity(alert.Severity),
                    fields = new[]
                    {
                        new { title = "Service", value = alert.ServiceName, @short = true },
                        new { title = "Message", value = alert.Message, @short = false }
                    },
                    ts = new DateTimeOffset(alert.Timestamp).ToUnixTimeSeconds()
                }
            }
        };

        await _httpClient.PostAsJsonAsync(_webhookUrl, payload);
    }
}
```

### 알림 흐름도
```
로그 수신 → 규칙 매칭 → 임계값 체크 → 쿨다운 확인 → 알림 전송 → 알림 기록
```

---

## ⚙️ 설정 및 실행

### 설정 파일

#### appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "LogServer": "Debug",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "LogSettings": {
    "LogDirectory": "logs",
    "MaxFileSizeMB": 100,
    "MaxRetentionDays": 365,
    "EnableCompression": true,
    "BatchSize": 100,
    "BatchTimeoutMs": 5000
  },
  "AlertSettings": {
    "EnableAlerts": true,
    "EmailEnabled": true,
    "SlackEnabled": false,
    "SmtpServer": "smtp.company.com",
    "SmtpPort": 587,
    "EmailFrom": "logserver@company.com",
    "SlackWebhookUrl": ""
  },
  "DashboardSettings": {
    "RefreshIntervalMs": 5000,
    "MaxLogsPerPage": 50,
    "EnableRealTimeStream": true
  }
}
```

### Docker 설정

#### Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY . .

# 로그 디렉토리 생성
RUN mkdir -p /app/logs

EXPOSE 5554 8080

# 볼륨 마운트 포인트
VOLUME ["/app/logs"]

ENTRYPOINT ["dotnet", "Log_Server.dll"]
```

#### docker-compose.yml
```yaml
version: '3.8'
services:
  log-server:
    build: .
    ports:
      - "5554:5554"  # gRPC
      - "8080:8080"  # HTTP
    volumes:
      - ./logs:/app/logs
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    restart: unless-stopped
```

### 서비스 실행

#### 개발 환경
```bash
cd Log_Server
dotnet run

# 출력 확인:
# Log Server 시작됨:
# - gRPC 포트: 5554
# - HTTP API 포트: 8080
```

#### 운영 환경
```bash
dotnet publish -c Release -o ./publish
cd publish
dotnet Log_Server.dll
```

---

## 💻 개발 가이드

### 1. 새로운 알림 규칙 추가

#### Step 1: 알림 규칙 정의
```csharp
// Log_Server/AlertRules/GameAlerts.cs
public static class GameAlerts
{
    public static readonly AlertRule PlayerCountSpike = new AlertRule
    {
        Name = "Sudden Player Count Increase",
        Condition = log => log.Category == "GamePlay" &&
                          log.Data.ContainsKey("PlayerCount") &&
                          int.Parse(log.Data["PlayerCount"].ToString()) > 1000,
        Severity = AlertSeverity.Info,
        Description = "게임 인기 급증으로 서버 확장 검토 필요"
    };
}
```

#### Step 2: 알림 서비스에 등록
```csharp
// Log_Server/Services/AlertService.cs
public class AlertService
{
    private readonly List<AlertRule> _rules = new()
    {
        PerformanceAlerts.HighCpuUsage,
        SecurityAlerts.FailedLoginSpike,
        GameAlerts.PlayerCountSpike  // 새로 추가
    };
}
```

### 2. 커스텀 로그 분석기 추가

#### 분석기 인터페이스
```csharp
// Log_Server/Analysis/ILogAnalyzer.cs
public interface ILogAnalyzer
{
    Task<AnalysisResult> AnalyzeAsync(LogEntry logEntry);
    Task<AnalysisResult> AnalyzeBatchAsync(IEnumerable<LogEntry> logs);
    string AnalyzerName { get; }
}

public class AnalysisResult
{
    public string AnalyzerName { get; set; } = string.Empty;
    public Dictionary<string, object> Metrics { get; set; } = new();
    public List<string> Insights { get; set; } = new();
    public AnalysisSeverity Severity { get; set; } = AnalysisSeverity.Normal;
}
```

#### 게임 성능 분석기 구현
```csharp
// Log_Server/Analysis/GamePerformanceAnalyzer.cs
public class GamePerformanceAnalyzer : ILogAnalyzer
{
    public string AnalyzerName => "Game Performance Analyzer";

    public async Task<AnalysisResult> AnalyzeAsync(LogEntry logEntry)
    {
        var result = new AnalysisResult { AnalyzerName = AnalyzerName };

        if (logEntry.Category == "Performance" && logEntry.Data.ContainsKey("FPS"))
        {
            var fps = double.Parse(logEntry.Data["FPS"].ToString());
            
            result.Metrics["AverageFPS"] = fps;
            
            if (fps < 30)
            {
                result.Insights.Add("낮은 FPS 감지 - 게임 성능 최적화 필요");
                result.Severity = AnalysisSeverity.Warning;
            }
            else if (fps > 60)
            {
                result.Insights.Add("우수한 게임 성능 유지");
                result.Severity = AnalysisSeverity.Good;
            }
        }

        return await Task.FromResult(result);
    }

    public async Task<AnalysisResult> AnalyzeBatchAsync(IEnumerable<LogEntry> logs)
    {
        var performanceLogs = logs.Where(l => l.Category == "Performance");
        var result = new AnalysisResult { AnalyzerName = AnalyzerName };

        if (performanceLogs.Any())
        {
            var avgFps = performanceLogs
                .Where(l => l.Data.ContainsKey("FPS"))
                .Average(l => double.Parse(l.Data["FPS"].ToString()));

            result.Metrics["BatchAverageFPS"] = avgFps;
            result.Insights.Add($"배치 평균 FPS: {avgFps:F1}");
        }

        return await Task.FromResult(result);
    }
}
```

### 3. 대시보드 위젯 추가

#### 새로운 차트 위젯
```javascript
// Log_Server/wwwroot/js/widgets/performance-widget.js
class PerformanceWidget {
    constructor(containerId) {
        this.containerId = containerId;
        this.chart = null;
        this.init();
    }

    init() {
        const ctx = document.getElementById(this.containerId).getContext('2d');
        this.chart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: [],
                datasets: [{
                    label: 'CPU Usage (%)',
                    data: [],
                    borderColor: 'rgb(255, 99, 132)',
                    tension: 0.1
                }, {
                    label: 'Memory Usage (%)',
                    data: [],
                    borderColor: 'rgb(54, 162, 235)',
                    tension: 0.1
                }]
            },
            options: {
                responsive: true,
                scales: {
                    y: {
                        beginAtZero: true,
                        max: 100
                    }
                },
                plugins: {
                    title: {
                        display: true,
                        text: '시스템 성능 모니터링'
                    }
                }
            }
        });

        this.startRealTimeUpdate();
    }

    startRealTimeUpdate() {
        setInterval(async () => {
            const data = await this.fetchPerformanceData();
            this.updateChart(data);
        }, 5000);
    }

    async fetchPerformanceData() {
        const response = await fetch('/api/stats/performance');
        return await response.json();
    }

    updateChart(data) {
        const now = new Date().toLocaleTimeString();
        
        this.chart.data.labels.push(now);
        this.chart.data.datasets[0].data.push(data.cpuUsage);
        this.chart.data.datasets[1].data.push(data.memoryUsage);

        // 최대 20개 데이터 포인트 유지
        if (this.chart.data.labels.length > 20) {
            this.chart.data.labels.shift();
            this.chart.data.datasets.forEach(dataset => dataset.data.shift());
        }

        this.chart.update();
    }
}
```

---

## 🚀 성능 최적화

### 1. 로그 배치 처리 최적화

#### 효율적인 배치 처리
```csharp
// Log_Server/Services/LogBatchProcessor.cs
public class LogBatchProcessor
{
    private readonly Channel<LogEntry> _logChannel;
    private readonly ILogStorageService _storageService;
    private readonly Timer _batchTimer;
    private readonly List<LogEntry> _currentBatch = new();
    private readonly SemaphoreSlim _batchSemaphore = new(1, 1);

    public LogBatchProcessor(ILogStorageService storageService)
    {
        _storageService = storageService;
        
        var options = new BoundedChannelOptions(10000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };
        
        _logChannel = Channel.CreateBounded<LogEntry>(options);
        
        // 5초마다 또는 100개 쌓이면 배치 처리
        _batchTimer = new Timer(ProcessBatchIfReady, null, 
            TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
            
        _ = Task.Run(ProcessLogsAsync);
    }

    public async Task EnqueueLogAsync(LogEntry logEntry)
    {
        await _logChannel.Writer.WriteAsync(logEntry);
    }

    private async Task ProcessLogsAsync()
    {
        await foreach (var logEntry in _logChannel.Reader.ReadAllAsync())
        {
            await _batchSemaphore.WaitAsync();
            try
            {
                _currentBatch.Add(logEntry);
                
                // 배치 크기 도달시 즉시 처리
                if (_currentBatch.Count >= 100)
                {
                    await ProcessCurrentBatch();
                }
            }
            finally
            {
                _batchSemaphore.Release();
            }
        }
    }

    private async void ProcessBatchIfReady(object? state)
    {
        await _batchSemaphore.WaitAsync();
        try
        {
            if (_currentBatch.Count > 0)
            {
                await ProcessCurrentBatch();
            }
        }
        finally
        {
            _batchSemaphore.Release();
        }
    }

    private async Task ProcessCurrentBatch()
    {
        if (_currentBatch.Count == 0) return;

        var batch = new List<LogEntry>(_currentBatch);
        _currentBatch.Clear();

        // 파일별로 그룹화하여 처리
        var fileGroups = batch.GroupBy(log => GetLogFileName(log.Timestamp, log.ServiceName.ToString()));
        
        await Parallel.ForEachAsync(fileGroups, async (group, ct) =>
        {
            await _storageService.StoreBatchAsync(group.ToList());
        });
    }
}
```

### 2. 메모리 사용량 최적화

#### 객체 풀링 및 재사용
```csharp
// Log_Server/Pools/LogEntryPool.cs
public class LogEntryPool
{
    private static readonly ObjectPool<LogEntry> _pool = 
        new DefaultObjectPool<LogEntry>(new LogEntryPoolPolicy());

    public static LogEntry Get()
    {
        return _pool.Get();
    }

    public static void Return(LogEntry logEntry)
    {
        logEntry.Reset();
        _pool.Return(logEntry);
    }
}

public class LogEntryPoolPolicy : PooledObjectPolicy<LogEntry>
{
    public override LogEntry Create() => new LogEntry();

    public override bool Return(LogEntry obj)
    {
        obj.Reset();
        return true;
    }
}
```

### 3. 파일 I/O 최적화

#### 비동기 파일 쓰기
```csharp
// Log_Server/Storage/AsyncFileLogStorage.cs
public class AsyncFileLogStorage : ILogStorageService
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks = new();
    private readonly ILogger<AsyncFileLogStorage> _logger;

    public async Task StoreBatchAsync(IEnumerable<LogEntry> logEntries)
    {
        var fileGroups = logEntries.GroupBy(log => GetLogFileName(log));
        
        await Parallel.ForEachAsync(fileGroups, new ParallelOptions 
        { 
            MaxDegreeOfParallelism = Environment.ProcessorCount 
        }, async (group, ct) =>
        {
            await WriteToFileAsync(group.Key, group.ToList());
        });
    }

    private async Task WriteToFileAsync(string fileName, List<LogEntry> logs)
    {
        var fileLock = _fileLocks.GetOrAdd(fileName, _ => new SemaphoreSlim(1, 1));
        
        await fileLock.WaitAsync();
        try
        {
            using var stream = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.Read, 
                bufferSize: 65536, useAsync: true);
            using var writer = new StreamWriter(stream, Encoding.UTF8);

            foreach (var log in logs)
            {
                var logLine = JsonSerializer.Serialize(log);
                await writer.WriteLineAsync(logLine);
            }
            
            await writer.FlushAsync();
        }
        finally
        {
            fileLock.Release();
        }
    }
}
```

---

## 🔍 트러블슈팅

### 1. 로그 손실 문제

#### 원인 및 해결
```csharp
// 로그 손실 방지를 위한 안전장치
public class ReliableLogCollectionService : ILogCollectionService
{
    private readonly ILogStorageService _storage;
    private readonly ILogger<ReliableLogCollectionService> _logger;
    private readonly ConcurrentQueue<LogEntry> _failedLogs = new();

    public async Task ProcessLogAsync(LogEntry logEntry)
    {
        try
        {
            await _storage.StoreLogAsync(logEntry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "로그 저장 실패, 재시도 큐에 추가: {LogId}", logEntry.Id);
            _failedLogs.Enqueue(logEntry);
        }
    }

    // 주기적으로 실패한 로그 재처리
    private async Task RetryFailedLogs()
    {
        while (_failedLogs.TryDequeue(out var failedLog))
        {
            try
            {
                await _storage.StoreLogAsync(failedLog);
                _logger.LogInformation("실패한 로그 재처리 성공: {LogId}", failedLog.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "로그 재처리 실패: {LogId}", failedLog.Id);
                
                // 3회 이상 실패시 데드레터 큐로 이동
                if (failedLog.RetryCount >= 3)
                {
                    await MoveToDeadLetterQueue(failedLog);
                }
                else
                {
                    failedLog.RetryCount++;
                    _failedLogs.Enqueue(failedLog);
                }
            }
        }
    }
}
```

### 2. 파일 시스템 문제

#### 디스크 공간 부족
```csharp
public class DiskSpaceMonitor
{
    private readonly ILogger<DiskSpaceMonitor> _logger;
    private readonly Timer _checkTimer;

    public DiskSpaceMonitor(ILogger<DiskSpaceMonitor> logger)
    {
        _logger = logger;
        _checkTimer = new Timer(CheckDiskSpace, null, 
            TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    private async void CheckDiskSpace(object? state)
    {
        var drive = new DriveInfo(Path.GetPathRoot(Environment.CurrentDirectory));
        var freeSpaceGB = drive.AvailableFreeSpace / (1024 * 1024 * 1024);
        
        if (freeSpaceGB < 5) // 5GB 이하
        {
            _logger.LogCritical("디스크 공간 부족: {FreeSpace}GB 남음", freeSpaceGB);
            
            // 오래된 로그 파일 정리
            await CleanupOldLogs();
            
            // 알림 전송
            await SendDiskSpaceAlert(freeSpaceGB);
        }
        else if (freeSpaceGB < 10) // 10GB 이하
        {
            _logger.LogWarning("디스크 공간 경고: {FreeSpace}GB 남음", freeSpaceGB);
        }
    }

    private async Task CleanupOldLogs()
    {
        var logDirectory = "logs";
        var cutoffDate = DateTime.Now.AddDays(-30); // 30일 이전 파일 삭제

        var oldFiles = Directory.GetFiles(logDirectory)
            .Where(file => File.GetCreationTime(file) < cutoffDate)
            .OrderBy(file => File.GetCreationTime(file))
            .Take(10); // 한 번에 최대 10개 파일 삭제

        foreach (var file in oldFiles)
        {
            try
            {
                File.Delete(file);
                _logger.LogInformation("오래된 로그 파일 삭제: {FileName}", Path.GetFileName(file));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "로그 파일 삭제 실패: {FileName}", Path.GetFileName(file));
            }
        }
    }
}
```

### 3. 성능 문제

#### 대용량 로그 처리 최적화
```csharp
public class HighVolumeLogProcessor
{
    private readonly Channel<LogEntry> _logChannel;
    private readonly SemaphoreSlim _processingLimiter;

    public HighVolumeLogProcessor()
    {
        // 대용량 처리를 위한 큰 버퍼 설정
        var options = new BoundedChannelOptions(100000)
        {
            FullMode = BoundedChannelFullMode.DropOldest, // 오래된 로그 드롭
            SingleReader = false,
            SingleWriter = false
        };

        _logChannel = Channel.CreateBounded<LogEntry>(options);
        _processingLimiter = new SemaphoreSlim(Environment.ProcessorCount * 2);

        // 멀티플 컨슈머 시작
        for (int i = 0; i < Environment.ProcessorCount; i++)
        {
            _ = Task.Run(() => ProcessLogsAsync($"Consumer-{i}"));
        }
    }

    private async Task ProcessLogsAsync(string consumerName)
    {
        await foreach (var log in _logChannel.Reader.ReadAllAsync())
        {
            await _processingLimiter.WaitAsync();
            try
            {
                await ProcessSingleLogAsync(log, consumerName);
            }
            finally
            {
                _processingLimiter.Release();
            }
        }
    }
}
```

---

*이 매뉴얼은 Log_Server의 완전한 가이드입니다. 중앙집중식 로깅을 통해 전체 시스템의 관찰가능성을 크게 향상시킬 수 있습니다.*