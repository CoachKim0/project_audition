using Shared.DTOs;

namespace Log_Server.Services;

public interface ILogStorageService
{
    Task StoreLogAsync(LogEntry logEntry);
    Task StoreBatchAsync(IEnumerable<LogEntry> logEntries);
    Task<IEnumerable<LogEntry>> GetLogsAsync(DateTime from, DateTime to, string? serviceFilter = null);
    Task<long> GetLogCountAsync(DateTime from, DateTime to);
}