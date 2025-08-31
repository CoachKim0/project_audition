using Grpc.Core;
using AuthServer.Grpc;

namespace AuthServer.Services;

public class AuthGrpcService : AuthServer.Grpc.AuthService.AuthServiceBase
{
    private readonly AuthTokenService _authService;
    private readonly ILogger<AuthGrpcService> _logger;

    public AuthGrpcService(AuthTokenService authService, ILogger<AuthGrpcService> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    public override async Task<ValidateTokenResponse> ValidateToken(ValidateTokenRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC 토큰 검증 요청 수신: {Username}", request.Username);

            var (isValid, message, userId) = await _authService.ValidateTokenAsync(
                request.AuthKey,
                request.Username,
                request.PlatformType,
                request.DeviceId
            );

            return new ValidateTokenResponse
            {
                IsValid = isValid,
                Message = message,
                UserId = userId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "gRPC 토큰 검증 요청 처리 중 오류 발생");
            
            return new ValidateTokenResponse
            {
                IsValid = false,
                Message = "서버 오류가 발생했습니다.",
                UserId = ""
            };
        }
    }
}