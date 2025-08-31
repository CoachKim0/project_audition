using Shared.DTOs;

namespace Shared.Contracts;

public interface ILogService
{
    Task LogAsync(LogEntry logEntry);
    Task LogBatchAsync(IEnumerable<LogEntry> logEntries);
    Task<IEnumerable<LogEntry>> GetLogsAsync(DateTime from, DateTime to, string? serviceFilter = null);
}