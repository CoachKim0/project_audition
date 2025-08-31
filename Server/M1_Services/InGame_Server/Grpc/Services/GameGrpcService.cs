using Grpc.Core;
using InGame_Server.Grpc;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace InGame_Server.Grpc.Services;

/// <summary>
/// gRPC 게임 서비스 클래스 (단순화됨)
/// </summary>
public class GameGrpcService : GameService.GameServiceBase
{
    private readonly ILogger<GameGrpcService> _logger;
    
    private static readonly ConcurrentDictionary<string, string> _connectedClients = new();

    public GameGrpcService(ILogger<GameGrpcService> logger)
    {
        _logger = logger;
    }

    public override async Task Game(IAsyncStreamReader<GameMessage> requestStream, IServerStreamWriter<GameMessage> responseStream, ServerCallContext context)
    {
        var clientId = context.Peer;
        _logger.LogInformation($"새로운 클라이언트 연결: {clientId}");

        _connectedClients.TryAdd(clientId, "connected");

        try
        {
            await foreach (var request in requestStream.ReadAllAsync())
            {
                _logger.LogInformation($"수신된 메시지: {request.MessageTypeCase} from {request.UserId}");
                
                var response = ProcessGameMessage(request);
                if (response != null)
                {
                    await responseStream.WriteAsync(response);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"클라이언트 {clientId} 처리 중 오류 발생");
        }
        finally
        {
            _connectedClients.TryRemove(clientId, out _);
            _logger.LogInformation($"클라이언트 {clientId} 연결 해제");
        }
    }

    private GameMessage? ProcessGameMessage(GameMessage request)
    {
        var response = new GameMessage
        {
            UserId = request.UserId,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        switch (request.MessageTypeCase)
        {
            case GameMessage.MessageTypeOneofCase.Ping:
                response.Ping = new Ping { SeqNo = request.Ping.SeqNo };
                response.ResultCode = (int)ResultCode.Success;
                response.ResultMessage = "Pong";
                return response;

            case GameMessage.MessageTypeOneofCase.AuthUser:
                response.ResultCode = (int)ResultCode.Success;
                response.ResultMessage = "인증 성공";
                return response;

            default:
                response.ResultCode = (int)ResultCode.Fail;
                response.ResultMessage = "지원되지 않는 메시지 타입입니다";
                return response;
        }
    }

}

