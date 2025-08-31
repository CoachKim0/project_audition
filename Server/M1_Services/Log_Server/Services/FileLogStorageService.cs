using Shared.DTOs;
using System.Text.Json;
using Log_Server.Services;

namespace Log_Server.Services;

public class FileLogStorageService : ILogStorageService
{
    private readonly string _logDirectory = "logs";
    private readonly object _lockObject = new object();

    public FileLogStorageService()
    {
        Directory.CreateDirectory(_logDirectory);
    }

    public async Task StoreLogAsync(LogEntry logEntry)
    {
        var fileName = GetLogFileName(logEntry.Timestamp, logEntry.ServiceName.ToString());
        var logLine = JsonSerializer.Serialize(logEntry) + Environment.NewLine;
        
        lock (_lockObject)
        {
            File.AppendAllText(fileName, logLine);
        }
        
        await Task.CompletedTask;
    }

    public async Task StoreBatchAsync(IEnumerable<LogEntry> logEntries)
    {
        var groupedLogs = logEntries.GroupBy(log => new 
        { 
            Date = log.Timestamp.Date, 
            Service = log.ServiceName.ToString() 
        });

        foreach (var group in groupedLogs)
        {
            var fileName = GetLogFileName(group.Key.Date, group.Key.Service);
            var logLines = group.Select(log => JsonSerializer.Serialize(log) + Environment.NewLine);
            
            lock (_lockObject)
            {
                File.AppendAllLines(fileName, logLines);
            }
        }
        
        await Task.CompletedTask;
    }

    public async Task<IEnumerable<LogEntry>> GetLogsAsync(DateTime from, DateTime to, string? serviceFilter = null)
    {
        var logs = new List<LogEntry>();
        
        for (var date = from.Date; date <= to.Date; date = date.AddDays(1))
        {
            var files = Directory.GetFiles(_logDirectory, $"*{date:yyyy-MM-dd}*.log");
            
            foreach (var file in files)
            {
                if (!string.IsNullOrEmpty(serviceFilter) && !file.Contains(serviceFilter))
                    continue;

                var lines = await File.ReadAllLinesAsync(file);
                
                foreach (var line in lines)
                {
                    try
                    {
                        var logEntry = JsonSerializer.Deserialize<LogEntry>(line);
                        if (logEntry != null && logEntry.Timestamp >= from && logEntry.Timestamp <= to)
                        {
                            logs.Add(logEntry);
                        }
                    }
                    catch
                    {
                        // 파싱 실패한 라인은 무시
                    }
                }
            }
        }
        
        return logs.OrderBy(l => l.Timestamp);
    }

    public async Task<long> GetLogCountAsync(DateTime from, DateTime to)
    {
        var logs = await GetLogsAsync(from, to);
        return logs.Count();
    }

    private string GetLogFileName(DateTime date, string serviceName)
    {
        return Path.Combine(_logDirectory, $"{serviceName}_{date:yyyy-MM-dd}.log");
    }
}