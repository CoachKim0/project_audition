using Microsoft.AspNetCore.Server.Kestrel.Core;
using Chat_Server.Modules.Common.ChatBase;
var builder = WebApplication.CreateBuilder(args);

// gRPC 서비스 추가
builder.Services.AddGrpc();

// Kestrel 서버 설정 - gRPC용 포트 5552
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(5552, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

var app = builder.Build();

// gRPC 서비스 매핑
app.MapGrpcService<ChatServiceImpl>();

Console.WriteLine("Chat Server 시작됨 (gRPC 포트: 5552)");
Console.WriteLine("Chat Server 실행 중...");

await app.RunAsync();
