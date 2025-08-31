using Grpc.Core;
using DbServer.Grpc;

namespace DbServer.Services;

public class AuthGrpcService : DbServer.Grpc.AuthService.AuthServiceBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthGrpcService> _logger;

    public AuthGrpcService(IAuthService authService, ILogger<AuthGrpcService> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    public override async Task<LoginResponse> Login(LoginRequest request, ServerCallContext context)
    {
        try
        {
            var (success, token, userId, message) = await _authService.LoginAsync(request.Username, request.Password);

            return new LoginResponse
            {
                Success = success,
                Token = token ?? "",
                UserId = userId ?? "",
                Message = message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "gRPC 로그인 요청 처리 중 오류 발생");
            
            return new LoginResponse
            {
                Success = false,
                Token = "",
                UserId = "",
                Message = "서버 오류가 발생했습니다."
            };
        }
    }

    public override async Task<RegisterResponse> Register(RegisterRequest request, ServerCallContext context)
    {
        try
        {
            var (success, userId, message) = await _authService.RegisterAsync(
                request.Username, 
                request.Password, 
                request.Email, 
                request.Nickname);

            return new RegisterResponse
            {
                Success = success,
                UserId = userId ?? "",
                Message = message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "gRPC 회원가입 요청 처리 중 오류 발생");
            
            return new RegisterResponse
            {
                Success = false,
                UserId = "",
                Message = "서버 오류가 발생했습니다."
            };
        }
    }

    public override async Task<ValidateTokenResponse> ValidateToken(ValidateTokenRequest request, ServerCallContext context)
    {
        try
        {
            var (valid, userId) = await _authService.ValidateTokenAsync(request.Token);

            return new ValidateTokenResponse
            {
                Valid = valid,
                UserId = userId ?? ""
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "gRPC 토큰 검증 요청 처리 중 오류 발생");
            
            return new ValidateTokenResponse
            {
                Valid = false,
                UserId = ""
            };
        }
    }
}