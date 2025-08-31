using Grpc.Core;
using Log_Server.Grpc;
using Shared.DTOs;
using Shared.Enums;
using Log_Server.Services;

namespace Log_Server.Services;

public class LogGrpcService : LogService.LogServiceBase
{
    private readonly ILogCollectionService _logCollectionService;
    private readonly ILogStorageService _logStorageService;
    private readonly ILogger<LogGrpcService> _logger;

    public LogGrpcService(
        ILogCollectionService logCollectionService,
        ILogStorageService logStorageService,
        ILogger<LogGrpcService> logger)
    {
        _logCollectionService = logCollectionService;
        _logStorageService = logStorageService;
        _logger = logger;
    }

    public override async Task<LogResponse> LogEntry(LogRequest request, ServerCallContext context)
    {
        try
        {
            var logEntry = ConvertToLogEntry(request);
            await _logCollectionService.ProcessLogAsync(logEntry);

            return new LogResponse
            {
                Success = true,
                Message = "Log entry processed successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process log entry");
            
            return new LogResponse
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public override async Task<LogResponse> LogBatch(LogBatchRequest request, ServerCallContext context)
    {
        try
        {
            var logEntries = request.Logs.Select(ConvertToLogEntry).ToList();
            await _logCollectionService.ProcessBatchAsync(logEntries);

            return new LogResponse
            {
                Success = true,
                Message = $"Processed {logEntries.Count} log entries"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process log batch");
            
            return new LogResponse
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public override async Task<GetLogsResponse> GetLogs(GetLogsRequest request, ServerCallContext context)
    {
        try
        {
            var fromTime = DateTimeOffset.FromUnixTimeMilliseconds(request.FromTimestamp).DateTime;
            var toTime = DateTimeOffset.FromUnixTimeMilliseconds(request.ToTimestamp).DateTime;
            
            var logs = await _logStorageService.GetLogsAsync(fromTime, toTime, request.ServiceFilter);
            
            var response = new GetLogsResponse
            {
                TotalCount = logs.Count()
            };

            foreach (var log in logs)
            {
                response.Logs.Add(ConvertToLogRequest(log));
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get logs");
            return new GetLogsResponse { TotalCount = 0 };
        }
    }

    private static LogEntry ConvertToLogEntry(LogRequest request)
    {
        return new LogEntry
        {
            Id = request.Id,
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(request.Timestamp).DateTime,
            ServiceName = Enum.Parse<ServiceType>(request.ServiceName),
            Level = (Shared.Enums.LogLevel)request.Level,
            Category = request.Category,
            Message = request.Message,
            UserId = string.IsNullOrEmpty(request.UserId) ? null : request.UserId,
            SessionId = string.IsNullOrEmpty(request.SessionId) ? null : request.SessionId,
            Data = request.Data.ToDictionary(kv => kv.Key, kv => (object)kv.Value),
            TraceId = string.IsNullOrEmpty(request.TraceId) ? null : request.TraceId
        };
    }

    private static LogRequest ConvertToLogRequest(LogEntry logEntry)
    {
        var request = new LogRequest
        {
            Id = logEntry.Id,
            Timestamp = new DateTimeOffset(logEntry.Timestamp).ToUnixTimeMilliseconds(),
            ServiceName = logEntry.ServiceName.ToString(),
            Level = (int)logEntry.Level,
            Category = logEntry.Category,
            Message = logEntry.Message,
            UserId = logEntry.UserId ?? "",
            SessionId = logEntry.SessionId ?? "",
            TraceId = logEntry.TraceId ?? ""
        };

        if (logEntry.Data != null)
        {
            foreach (var kv in logEntry.Data)
            {
                request.Data[kv.Key] = kv.Value?.ToString() ?? "";
            }
        }

        return request;
    }
}