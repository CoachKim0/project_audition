namespace AuthServer.Services;

public class AuthTokenService
{
    private readonly ILogger<AuthTokenService> _logger;
    
    // 테스트용 하드코딩된 토큰 (실제로는 외부 OAuth 서비스 연동)
    private readonly HashSet<string> _validTokens = new()
    {
        "google_or_apple_auth_key_abcd_1234",
        "test_token_12345",
        "dev_auth_key_2024"
    };

    public AuthTokenService(ILogger<AuthTokenService> logger)
    {
        _logger = logger;
    }

    public async Task<(bool IsValid, string Message, string UserId)> ValidateTokenAsync(
        string authKey, string username, string platformType, string deviceId)
    {
        try
        {
            _logger.LogInformation("토큰 검증 요청: Username={Username}, AuthKey={AuthKey}, Platform={Platform}", 
                username, authKey?[..Math.Min(authKey?.Length ?? 0, 10)], platformType);

            // 시뮬레이션 딜레이
            await Task.Delay(50);

            // 테스트용 간단한 검증 - 하드코딩된 토큰과 비교
            if (_validTokens.Contains(authKey))
            {
                var userId = Guid.NewGuid().ToString();
                _logger.LogInformation("토큰 검증 성공: {Username}", username);
                
                return (true, "토큰 검증 성공", userId);
            }
            else
            {
                _logger.LogWarning("토큰 검증 실패: 유효하지 않은 토큰 - {Username}", username);
                return (false, "유효하지 않은 인증 토큰입니다.", "");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "토큰 검증 중 오류 발생: {Username}", username);
            return (false, "토큰 검증 중 서버 오류가 발생했습니다.", "");
        }
    }
}