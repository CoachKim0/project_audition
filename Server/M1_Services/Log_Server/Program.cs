using Microsoft.AspNetCore.Server.Kestrel.Core;
using Log_Server.Services;
using Serilog;
using Shared.Services;

var builder = WebApplication.CreateBuilder(args);

// Serilog 설정
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log-server-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// gRPC 서비스 추가
builder.Services.AddGrpc();

// 로그 서비스들 등록
builder.Services.AddSingleton<ILogStorageService, FileLogStorageService>();
builder.Services.AddSingleton<ILogCollectionService, LogCollectionService>();

// Auth_Server 클라이언트 등록
builder.Services.AddSingleton<AuthServiceClient>();

// Kestrel 서버 설정
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    // gRPC용 포트 5554
    serverOptions.ListenAnyIP(5554, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
    
    // HTTP API용 포트 8080
    serverOptions.ListenAnyIP(8080, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1;
    });
});

// API 컨트롤러 추가 (대시보드용)
builder.Services.AddControllers();

var app = builder.Build();

// gRPC 서비스 매핑
app.MapGrpcService<LogGrpcService>();

// API 컨트롤러 매핑
app.MapControllers();

// 정적 파일 서빙 (대시보드)
app.UseStaticFiles();

Console.WriteLine("Log Server 시작됨:");
Console.WriteLine("- gRPC 포트: 5554");
Console.WriteLine("- HTTP API 포트: 8080");

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Log Server 시작 실패");
}
finally
{
    Log.CloseAndFlush();
}
