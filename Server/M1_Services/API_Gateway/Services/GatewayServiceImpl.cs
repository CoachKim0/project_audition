using Grpc.Core;
using Grpc.Net.Client;
using API_Gateway.Protos;
using DbServer.Grpc;
using System.Text.Json;
using AuthServerProtos = Auth_Server.Protos;

namespace API_Gateway.Services;

/// <summary>
/// API Gateway 서비스 구현 - 모든 마이크로서비스로의 라우팅 처리 (간단한 구현)
/// </summary>
public class GatewayServiceImpl : GatewayService.GatewayServiceBase
{
    private readonly GrpcChannel _gameChannel;
    private readonly GrpcChannel _chatChannel;
    private readonly GrpcChannel _dbChannel;
    private readonly GrpcChannel _logChannel;
    private readonly GrpcChannel _authChannel;

    public GatewayServiceImpl()
    {
        // 각 서비스의 gRPC 채널 생성
        _gameChannel = GrpcChannel.ForAddress("http://localhost:5551");
        _chatChannel = GrpcChannel.ForAddress("http://localhost:5552");
        _dbChannel = GrpcChannel.ForAddress("http://localhost:5553");
        _logChannel = GrpcChannel.ForAddress("http://localhost:5554");
        _authChannel = GrpcChannel.ForAddress("http://localhost:5555"); // Auth_Server 포트
        
    }

    /// <summary>
    /// 로그인 처리 - Auth_Server로 라우팅 (간단한 구현)
    /// </summary>
    public override async Task<API_Gateway.Protos.LoginResponse> Login(API_Gateway.Protos.LoginRequest request, ServerCallContext context)
    {
        try
        {
            Console.WriteLine($"API Gateway: 로그인 요청 수신 - {request.Username}");
            
            // DB_Server로 실제 로그인 처리
            var dbClient = new DbServer.Grpc.AuthService.AuthServiceClient(_dbChannel);
            var dbRequest = new DbServer.Grpc.LoginRequest
            {
                Username = request.Username,
                Password = request.Password
            };
            
            var dbResponse = await dbClient.LoginAsync(dbRequest);
            
            return new API_Gateway.Protos.LoginResponse
            {
                Success = dbResponse.Success,
                Message = dbResponse.Message,
                AccessToken = dbResponse.Token ?? "",
                RefreshToken = dbResponse.Token ?? "", // 임시: 같은 토큰 사용
                UserId = dbResponse.Success ? (int)dbResponse.UserIdx : 0
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login 오류: {ex.Message}");
            return new API_Gateway.Protos.LoginResponse
            {
                Success = false,
                Message = "로그인 처리 중 오류가 발생했습니다."
            };
        }
    }

    /// <summary>
    /// 회원가입 처리 - DB_Server로 실제 저장
    /// </summary>
    public override async Task<API_Gateway.Protos.RegisterResponse> Register(API_Gateway.Protos.RegisterRequest request, ServerCallContext context)
    {
        try
        {
            Console.WriteLine($"API Gateway: 회원가입 요청 수신 - {request.Username}");
            Console.WriteLine($"API Gateway: 받은 AuthKey - '{request.AuthKey}'");
            Console.WriteLine($"API Gateway: PlatformType - {request.PlatformType}");
            Console.WriteLine($"API Gateway: DeviceId - '{request.DeviceId}'");
            
            // 1. Auth_Server에서 AuthKey 검증 먼저 수행
            var authClient = new global::Auth_Server.Protos.AuthService.AuthServiceClient(_authChannel);
            var authKeyRequest = new global::Auth_Server.Protos.ValidateAuthKeyRequest
            {
                AuthKey = request.AuthKey ?? "", // AuthKey 필드가 있다고 가정
                PlatformType = request.PlatformType,
                DeviceId = request.DeviceId ?? ""
            };
            
            Console.WriteLine($"API Gateway: Auth_Server로 보내는 AuthKey - '{authKeyRequest.AuthKey}'");
            
            var authKeyResponse = await authClient.ValidateAuthKeyAsync(authKeyRequest);
            
            if (!authKeyResponse.Success)
            {
                Console.WriteLine($"AuthKey 검증 실패: {authKeyResponse.Message}");
                return new API_Gateway.Protos.RegisterResponse
                {
                    Success = false,
                    Message = $"인증 실패: {authKeyResponse.Message}"
                };
            }
            
            Console.WriteLine("AuthKey 검증 성공, DB_Server로 회원가입 진행");
            
            // 2. AuthKey 검증 성공 후 DB_Server로 실제 회원가입 진행
            var dbClient = new DbServer.Grpc.AuthService.AuthServiceClient(_dbChannel);
            
            var dbRequest = new DbServer.Grpc.RegisterRequest
            {
                Username = request.Username,
                Password = request.Password,
                Email = request.Email,
                Nickname = request.Username // 기본값으로 username 사용
            };
            
            var dbResponse = await dbClient.RegisterAsync(dbRequest);
            
            Console.WriteLine($"DB_Server 응답: Success={dbResponse.Success}, Message={dbResponse.Message}");
            
            return new API_Gateway.Protos.RegisterResponse
            {
                Success = dbResponse.Success,
                Message = dbResponse.Message,
                UserId = dbResponse.Success ? (int)dbResponse.UserIdx : 0
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Register 오류: {ex.Message}");
            return new API_Gateway.Protos.RegisterResponse
            {
                Success = false,
                Message = "회원가입 처리 중 오류가 발생했습니다."
            };
        }
    }

    /// <summary>
    /// 토큰 검증 - Auth_Server로 라우팅 (간단한 구현)
    /// </summary>
    public override async Task<API_Gateway.Protos.ValidateTokenResponse> ValidateToken(API_Gateway.Protos.ValidateTokenRequest request, ServerCallContext context)
    {
        try
        {
            Console.WriteLine($"API Gateway: 토큰 검증 요청 수신");
            
            // DB_Server로 실제 토큰 검증 처리
            var dbClient = new DbServer.Grpc.AuthService.AuthServiceClient(_dbChannel);
            var dbRequest = new DbServer.Grpc.ValidateTokenRequest
            {
                Token = request.Token
            };
            
            var dbResponse = await dbClient.ValidateTokenAsync(dbRequest);
            
            return new API_Gateway.Protos.ValidateTokenResponse
            {
                IsValid = dbResponse.Valid,
                UserId = dbResponse.Valid ? (int)dbResponse.UserIdx : 0,
                Username = dbResponse.Username ?? ""
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ValidateToken 오류: {ex.Message}");
            return new API_Gateway.Protos.ValidateTokenResponse
            {
                IsValid = false,
                UserId = 0,
                Username = ""
            };
        }
    }

    /// <summary>
    /// 게임 참여 - InGame_Server로 라우팅 (간단한 구현)
    /// </summary>
    public override async Task<JoinGameResponse> JoinGame(JoinGameRequest request, ServerCallContext context)
    {
        try
        {
            Console.WriteLine($"API Gateway: 게임 참여 요청 수신");
            
            // TODO: 토큰 검증 및 InGame_Server로 실제 gRPC 호출 구현
            await Task.Delay(50);
            
            return new JoinGameResponse
            {
                Success = true,
                RoomId = "room_" + Guid.NewGuid().ToString("N")[..8],
                GameServerEndpoint = "localhost:7777",
                Message = "게임 참여 성공 (테스트 구현)"
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"JoinGame 오류: {ex.Message}");
            return new JoinGameResponse
            {
                Success = false,
                Message = "게임 참여 처리 중 오류가 발생했습니다."
            };
        }
    }

    /// <summary>
    /// 게임 액션 처리 - InGame_Server로 라우팅 (간단한 구현)
    /// </summary>
    public override async Task<GameActionResponse> GameAction(GameActionRequest request, ServerCallContext context)
    {
        try
        {
            Console.WriteLine($"API Gateway: 게임 액션 요청 수신 - {request.ActionType}");
            
            // TODO: 토큰 검증 및 InGame_Server로 실제 gRPC 호출 구현
            await Task.Delay(50);
            
            return new GameActionResponse
            {
                Success = true,
                Message = "액션 처리 완료 (테스트 구현)",
                ResultData = $"Action '{request.ActionType}' processed successfully"
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GameAction 오류: {ex.Message}");
            return new GameActionResponse
            {
                Success = false,
                Message = "게임 액션 처리 중 오류가 발생했습니다."
            };
        }
    }

    /// <summary>
    /// 채팅 전송 - Chat_Server로 라우팅 (간단한 구현)
    /// </summary>
    public override async Task<SendChatResponse> SendChat(SendChatRequest request, ServerCallContext context)
    {
        try
        {
            Console.WriteLine($"API Gateway: 채팅 전송 요청 수신 - {request.Message}");
            
            // TODO: 토큰 검증 및 Chat_Server로 실제 gRPC 호출 구현
            await Task.Delay(50);
            
            return new SendChatResponse
            {
                Success = true,
                Message = "메시지 전송 완료 (테스트 구현)",
                MessageId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SendChat 오류: {ex.Message}");
            return new SendChatResponse
            {
                Success = false,
                Message = "채팅 전송 중 오류가 발생했습니다."
            };
        }
    }

    /// <summary>
    /// 채팅 기록 조회 - Chat_Server로 라우팅 (간단한 구현)
    /// </summary>
    public override async Task<GetChatHistoryResponse> GetChatHistory(GetChatHistoryRequest request, ServerCallContext context)
    {
        try
        {
            Console.WriteLine($"API Gateway: 채팅 기록 조회 요청 수신 - Room: {request.RoomId}");
            
            // TODO: 토큰 검증 및 Chat_Server로 실제 gRPC 호출 구현
            await Task.Delay(50);
            
            return new GetChatHistoryResponse
            {
                Success = true,
                Message = "채팅 기록 조회 완료 (테스트 구현)"
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetChatHistory 오류: {ex.Message}");
            return new GetChatHistoryResponse
            {
                Success = false,
                Message = "채팅 기록 조회 중 오류가 발생했습니다."
            };
        }
    }

    /// <summary>
    /// 범용 요청 처리 - 서비스별 라우팅
    /// </summary>
    public override async Task<GatewayResponse> ProcessRequest(GatewayRequest request, ServerCallContext context)
    {
        try
        {
            // 로깅
            Console.WriteLine($"ProcessRequest: {request.Service}.{request.Method} from {request.UserId}");

            // 서비스별 라우팅
            switch (request.Service.ToLower())
            {
                case "auth":
                    return await ProcessAuthRequest(request);
                case "game":
                    return await ProcessGameRequest(request);
                case "chat":
                    return await ProcessChatRequest(request);
                case "db":
                    return await ProcessDbRequest(request);
                case "log":
                    return await ProcessLogRequest(request);
                default:
                    return new GatewayResponse
                    {
                        Success = false,
                        Message = $"알 수 없는 서비스: {request.Service}",
                        StatusCode = 404,
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ProcessRequest 오류: {ex.Message}");
            return new GatewayResponse
            {
                Success = false,
                Message = "요청 처리 중 오류가 발생했습니다.",
                StatusCode = 500,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }
    }

    private async Task<GatewayResponse> ProcessAuthRequest(GatewayRequest request)
    {
        // Auth_Server로 라우팅 (구현 예정)
        return new GatewayResponse
        {
            Success = true,
            Message = "Auth request processed",
            Data = "{}",
            StatusCode = 200,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
    }

    private async Task<GatewayResponse> ProcessGameRequest(GatewayRequest request)
    {
        // InGame_Server로 라우팅 (구현 예정)
        return new GatewayResponse
        {
            Success = true,
            Message = "Game request processed",
            Data = "{}",
            StatusCode = 200,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
    }

    private async Task<GatewayResponse> ProcessChatRequest(GatewayRequest request)
    {
        // Chat_Server로 라우팅 (구현 예정)
        return new GatewayResponse
        {
            Success = true,
            Message = "Chat request processed",
            Data = "{}",
            StatusCode = 200,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
    }

    private async Task<GatewayResponse> ProcessDbRequest(GatewayRequest request)
    {
        // DB_Server로 라우팅 (구현 예정)
        return new GatewayResponse
        {
            Success = true,
            Message = "DB request processed",
            Data = "{}",
            StatusCode = 200,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
    }

    private async Task<GatewayResponse> ProcessLogRequest(GatewayRequest request)
    {
        // Log_Server로 라우팅 (구현 예정)
        return new GatewayResponse
        {
            Success = true,
            Message = "Log request processed",
            Data = "{}",
            StatusCode = 200,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
    }
}