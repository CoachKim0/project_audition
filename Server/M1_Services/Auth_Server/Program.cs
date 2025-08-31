using Auth_Server.Services;
using Auth_Server.Utils;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// gRPC 서비스만 추가
builder.Services.AddGrpc();

// JWT 서비스 추가
builder.Services.AddSingleton<JwtTokenService>();

// Kestrel 서버 설정 - gRPC 전용 포트 5555
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(5555, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

var app = builder.Build();

// gRPC 서비스만 매핑
app.MapGrpcService<AuthServiceImpl>();

Console.WriteLine("Auth_Server 시작됨 (gRPC 포트: 5555)");
Console.WriteLine("Auth_Server 실행 중...");

await app.RunAsync();
