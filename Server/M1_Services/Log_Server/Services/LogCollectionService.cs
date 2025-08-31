using Shared.DTOs;
using Shared.Enums;
using Log_Server.Services;

namespace Log_Server.Services;

public class LogCollectionService : ILogCollectionService
{
    private readonly ILogStorageService _storageService;
    private readonly ILogger<LogCollectionService> _logger;

    public LogCollectionService(ILogStorageService storageService, ILogger<LogCollectionService> logger)
    {
        _storageService = storageService;
        _logger = logger;
    }

    public async Task ProcessLogAsync(LogEntry logEntry)
    {
        try
        {
            // 로그 유효성 검사
            if (string.IsNullOrEmpty(logEntry.Message))
            {
                _logger.LogWarning("Empty log message received from {ServiceName}", logEntry.ServiceName);
                return;
            }

            // TraceId가 없으면 생성
            if (string.IsNullOrEmpty(logEntry.TraceId))
            {
                logEntry.TraceId = Guid.NewGuid().ToString("N")[..8];
            }

            // 저장
            await _storageService.StoreLogAsync(logEntry);

            // 알림 체크
            if (await ShouldAlert(logEntry))
            {
                await TriggerAlert(logEntry);
            }

            _logger.LogDebug("Processed log from {ServiceName}: {Message}", logEntry.ServiceName, logEntry.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process log entry from {ServiceName}", logEntry.ServiceName);
        }
    }

    public async Task ProcessBatchAsync(IEnumerable<LogEntry> logEntries)
    {
        try
        {
            var validLogs = new List<LogEntry>();

            foreach (var logEntry in logEntries)
            {
                if (!string.IsNullOrEmpty(logEntry.Message))
                {
                    if (string.IsNullOrEmpty(logEntry.TraceId))
                    {
                        logEntry.TraceId = Guid.NewGuid().ToString("N")[..8];
                    }
                    validLogs.Add(logEntry);

                    // 개별 알림 체크
                    if (await ShouldAlert(logEntry))
                    {
                        await TriggerAlert(logEntry);
                    }
                }
            }

            if (validLogs.Any())
            {
                await _storageService.StoreBatchAsync(validLogs);
                _logger.LogInformation("Processed batch of {Count} logs", validLogs.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process log batch");
        }
    }

    public async Task<bool> ShouldAlert(LogEntry logEntry)
    {
        await Task.CompletedTask;

        // 알림 규칙들
        return logEntry.Level switch
        {
            Shared.Enums.LogLevel.ERROR => true,
            Shared.Enums.LogLevel.CRITICAL => true,
            Shared.Enums.LogLevel.WARN when logEntry.Category.Contains("Security") => true,
            Shared.Enums.LogLevel.WARN when logEntry.Category.Contains("Performance") => true,
            _ => false
        };
    }

    private async Task TriggerAlert(LogEntry logEntry)
    {
        // 실제 구현에서는 이메일, 슬랙, SMS 등으로 알림 전송
        _logger.LogWarning("ALERT: {Level} from {ServiceName} - {Message}", 
            logEntry.Level, logEntry.ServiceName, logEntry.Message);
        
        await Task.CompletedTask;
    }
}