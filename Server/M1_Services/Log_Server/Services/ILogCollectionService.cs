using Shared.DTOs;

namespace Log_Server.Services;

public interface ILogCollectionService
{
    Task ProcessLogAsync(LogEntry logEntry);
    Task ProcessBatchAsync(IEnumerable<LogEntry> logEntries);
    Task<bool> ShouldAlert(LogEntry logEntry);
}