using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Builder;
using InGame_Server.Grpc.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
class Program
{

    static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        builder.Services.AddGrpc();
        
        // Kestrel 서버 설정 - gRPC용 포트 5551
        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.ListenAnyIP(5551, listenOptions =>
            {
                // HTTP/2만 사용
                listenOptions.Protocols = HttpProtocols.Http2;
            });
        });

        var app = builder.Build();
        app.MapGrpcService<GameGrpcService>();

        Console.WriteLine("InGame Server 시작됨 (gRPC 포트: 5551)");
        Console.WriteLine("InGame Server 실행 중...");
        
        await app.RunAsync();
    }
}