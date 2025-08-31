using Grpc.Core;
using API_Gateway.Protos;
using Shared.Services;

namespace API_Gateway.Services;

/// <summary>
/// 단순화된 API Gateway 서비스 구현
/// </summary>
public class GatewayServiceImplSimple : GatewayService.GatewayServiceBase
{
    private readonly AuthServiceClient _authClient;

    public GatewayServiceImplSimple()
    {
        _authClient = new AuthServiceClient("http://localhost:5555");
    }

    /// <summary>
    /// 로그인 처리 - Auth_Server로 라우팅
    /// </summary>
    public override async Task<LoginResponse> Login(LoginRequest request, ServerCallContext context)
    {
        try
        {
            Console.WriteLine($"API Gateway: 로그인 요청 수신 - {request.Username}");
            
            // Auth_Server 호출
            var authResponse = await _authClient.LoginAsync(request.Username, request.Password);
            
            Console.WriteLine($"API Gateway: Auth_Server 응답 - {authResponse.Success}");
            
            // 응답 변환
            return new LoginResponse
            {
                Success = authResponse.Success,
                Message = authResponse.Message,
                AccessToken = authResponse.AccessToken,
                RefreshToken = authResponse.RefreshToken,
                UserId = authResponse.UserId
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"API Gateway Login 오류: {ex.Message}");
            return new LoginResponse
            {
                Success = false,
                Message = "로그인 처리 중 오류가 발생했습니다."
            };
        }
    }

    /// <summary>
    /// 회원가입 처리 - Auth_Server로 라우팅
    /// </summary>
    public override async Task<RegisterResponse> Register(RegisterRequest request, ServerCallContext context)
    {
        try
        {
            Console.WriteLine($"API Gateway: 회원가입 요청 수신 - {request.Username}");
            
            // Auth_Server 호출
            var authResponse = await _authClient.RegisterAsync(request.Username, request.Password, request.Email);
            
            Console.WriteLine($"API Gateway: Auth_Server 응답 - {authResponse.Success}");

            return new RegisterResponse
            {
                Success = authResponse.Success,
                Message = authResponse.Message,
                UserId = authResponse.UserId
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"API Gateway Register 오류: {ex.Message}");
            return new RegisterResponse
            {
                Success = false,
                Message = "회원가입 처리 중 오류가 발생했습니다."
            };
        }
    }

    /// <summary>
    /// 토큰 검증 - Auth_Server로 라우팅
    /// </summary>
    public override async Task<ValidateTokenResponse> ValidateToken(ValidateTokenRequest request, ServerCallContext context)
    {
        try
        {
            Console.WriteLine($"API Gateway: 토큰 검증 요청 수신");
            
            var authResponse = await _authClient.ValidateTokenAsync(request.Token);
            
            Console.WriteLine($"API Gateway: 토큰 검증 결과 - {authResponse.IsValid}");

            return new ValidateTokenResponse
            {
                IsValid = authResponse.IsValid,
                UserId = authResponse.UserId,
                Username = authResponse.Username
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"API Gateway ValidateToken 오류: {ex.Message}");
            return new ValidateTokenResponse
            {
                IsValid = false,
                UserId = 0,
                Username = ""
            };
        }
    }

    // 나머지 메서드들은 나중에 구현
    public override async Task<JoinGameResponse> JoinGame(JoinGameRequest request, ServerCallContext context)
    {
        return new JoinGameResponse
        {
            Success = false,
            Message = "게임 기능은 구현 예정입니다."
        };
    }

    public override async Task<GameActionResponse> GameAction(GameActionRequest request, ServerCallContext context)
    {
        return new GameActionResponse
        {
            Success = false,
            Message = "게임 액션 기능은 구현 예정입니다."
        };
    }

    public override async Task<SendChatResponse> SendChat(SendChatRequest request, ServerCallContext context)
    {
        return new SendChatResponse
        {
            Success = false,
            Message = "채팅 기능은 구현 예정입니다."
        };
    }

    public override async Task<GetChatHistoryResponse> GetChatHistory(GetChatHistoryRequest request, ServerCallContext context)
    {
        return new GetChatHistoryResponse
        {
            Success = false,
            Message = "채팅 기록 기능은 구현 예정입니다."
        };
    }

    public override async Task<GatewayResponse> ProcessRequest(GatewayRequest request, ServerCallContext context)
    {
        return new GatewayResponse
        {
            Success = false,
            Message = "범용 처리 기능은 구현 예정입니다.",
            StatusCode = 501,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
    }
}