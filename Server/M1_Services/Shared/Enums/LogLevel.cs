namespace Shared.Enums;

public enum LogLevel
{
    DEBUG = 0,
    INFO = 1,
    WARN = 2,
    ERROR = 3,
    CRITICAL = 4
}

public enum ServiceType
{
    InGame_Server,
    Chat_Server,
    DB_Server,
    Log_Server
}