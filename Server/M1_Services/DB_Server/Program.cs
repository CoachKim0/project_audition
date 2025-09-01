using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using DbServer.Services;
using DbServer.Data.Context;

var builder = WebApplication.CreateBuilder(args);

// gRPC 서비스 추가
builder.Services.AddGrpc();

// MySQL Entity Framework 설정
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=localhost;Port=3306;Database=M1_DB;Uid=root;Pwd=1111;";

Console.WriteLine($"사용중인 연결 문자열: {connectionString}");

builder.Services.AddDbContext<GameDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.Parse("8.0.41-mysql"))
           .LogTo(Console.WriteLine, LogLevel.Information));

// 서비스 등록
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();


// Kestrel 서버 설정 - gRPC용 포트 5553
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(5553, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

var app = builder.Build();

// gRPC 서비스 매핑
app.MapGrpcService<AuthGrpcService>();
app.MapGrpcService<UserGrpcService>();

// 데이터베이스 초기화 및 마이그레이션3
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("데이터베이스 마이그레이션 시작...");
        await context.Database.EnsureCreatedAsync();
        logger.LogInformation("데이터베이스 초기화 완료");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "데이터베이스 초기화 실패");
        throw;
    }
}

Console.WriteLine("DB Server 시작됨 (gRPC 포트: 5553)");

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"DB Server 시작 실패: {ex.Message}");
}
