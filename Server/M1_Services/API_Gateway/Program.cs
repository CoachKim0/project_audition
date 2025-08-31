using Microsoft.AspNetCore.Server.Kestrel.Core;
using API_Gateway.Services;
using Grpc.Net.Client;

var builder = WebApplication.CreateBuilder(args);

// gRPC 서비스 추가
builder.Services.AddGrpc();

// 마이크로서비스 클라이언트들은 각 서비스 구현에서 직접 생성
// TODO: 향후 정식 gRPC 클라이언트 연동 시 여기에 추가

// API Gateway 서비스 등록
builder.Services.AddScoped<GatewayServiceImpl>();

// Kestrel 서버 설정 - API Gateway 포트 5550
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(5550, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

var app = builder.Build();

// gRPC 서비스 매핑
app.MapGrpcService<GatewayServiceImpl>();

Console.WriteLine("API Gateway 시작됨 (gRPC 포트: 5550)");
Console.WriteLine("연결 대상 서비스들:");
Console.WriteLine("  - Auth_Server: http://localhost:5555");
Console.WriteLine("  - InGame_Server: http://localhost:5551");
Console.WriteLine("  - Chat_Server: http://localhost:5552");
Console.WriteLine("  - DB_Server: http://localhost:5553");
Console.WriteLine("  - Log_Server: http://localhost:5554");
Console.WriteLine("API Gateway 실행 중...");

await app.RunAsync();