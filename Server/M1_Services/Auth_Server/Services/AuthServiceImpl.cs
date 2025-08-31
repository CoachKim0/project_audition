using Auth_Server.Protos;
using Auth_Server.Utils;
using Grpc.Core;

namespace Auth_Server.Services;

public class AuthServiceImpl : AuthService.AuthServiceBase
{
    private readonly ILogger<AuthServiceImpl> _logger;
    private readonly JwtTokenService _jwtService;
    private readonly Dictionary<string, UserData> _users; // 임시 메모리 저장소
    private readonly Dictionary<string, string> _refreshTokens;
    private int _nextUserId = 1;

    public AuthServiceImpl(ILogger<AuthServiceImpl> logger, JwtTokenService jwtService)
    {
        _logger = logger;
        _jwtService = jwtService;
        _users = new Dictionary<string, UserData>();
        _refreshTokens = new Dictionary<string, string>();
        
        // 테스트용 기본 사용자
        _users["testuser"] = new UserData 
        { 
            UserId = _nextUserId++, 
            Username = "testuser", 
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("testpass"),
            Email = "test@example.com"
        };
    }

    public override async Task<LoginResponse> Login(LoginRequest request, ServerCallContext context)
    {
        _logger.LogInformation($"Login attempt for user: {request.Username}");

        try
        {
            if (!_users.ContainsKey(request.Username))
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            var user = _users[request.Username];
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "Invalid password"
                };
            }

            var accessToken = _jwtService.GenerateAccessToken(user.UserId, user.Username);
            var refreshToken = _jwtService.GenerateRefreshToken();
            
            _refreshTokens[refreshToken] = user.Username;

            _logger.LogInformation($"User {request.Username} logged in successfully");

            return new LoginResponse
            {
                Success = true,
                Message = "Login successful",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                UserId = user.UserId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return new LoginResponse
            {
                Success = false,
                Message = "Internal server error"
            };
        }
    }

    public override async Task<RegisterResponse> Register(RegisterRequest request, ServerCallContext context)
    {
        _logger.LogInformation($"Registration attempt for user: {request.Username}");

        try
        {
            if (_users.ContainsKey(request.Username))
            {
                return new RegisterResponse
                {
                    Success = false,
                    Message = "User already exists"
                };
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var userId = _nextUserId++;
            
            _users[request.Username] = new UserData
            {
                UserId = userId,
                Username = request.Username,
                PasswordHash = passwordHash,
                Email = request.Email
            };

            _logger.LogInformation($"User {request.Username} registered successfully");

            return new RegisterResponse
            {
                Success = true,
                Message = "Registration successful",
                UserId = userId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return new RegisterResponse
            {
                Success = false,
                Message = "Internal server error"
            };
        }
    }

    public override async Task<ValidateTokenResponse> ValidateToken(ValidateTokenRequest request, ServerCallContext context)
    {
        try
        {
            var principal = _jwtService.ValidateToken(request.Token);
            if (principal == null)
            {
                return new ValidateTokenResponse
                {
                    IsValid = false
                };
            }

            var userIdClaim = principal.FindFirst("userId")?.Value;
            var usernameClaim = principal.FindFirst("username")?.Value;

            if (int.TryParse(userIdClaim, out int userId))
            {
                return new ValidateTokenResponse
                {
                    IsValid = true,
                    UserId = userId,
                    Username = usernameClaim ?? ""
                };
            }

            return new ValidateTokenResponse
            {
                IsValid = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return new ValidateTokenResponse
            {
                IsValid = false
            };
        }
    }

    public override async Task<RefreshTokenResponse> RefreshToken(RefreshTokenRequest request, ServerCallContext context)
    {
        try
        {
            if (!_refreshTokens.ContainsKey(request.RefreshToken))
            {
                return new RefreshTokenResponse
                {
                    Success = false
                };
            }

            var username = _refreshTokens[request.RefreshToken];
            var user = _users[username];

            var newAccessToken = _jwtService.GenerateAccessToken(user.UserId, user.Username);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            _refreshTokens.Remove(request.RefreshToken);
            _refreshTokens[newRefreshToken] = username;

            return new RefreshTokenResponse
            {
                Success = true,
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return new RefreshTokenResponse
            {
                Success = false
            };
        }
    }

    public override async Task<GetUserInfoResponse> GetUserInfo(GetUserInfoRequest request, ServerCallContext context)
    {
        try
        {
            var user = _users.Values.FirstOrDefault(u => u.UserId == request.UserId);
            if (user == null)
            {
                return new GetUserInfoResponse
                {
                    Success = false
                };
            }

            return new GetUserInfoResponse
            {
                Success = true,
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user info");
            return new GetUserInfoResponse
            {
                Success = false
            };
        }
    }

    private class UserData
    {
        public int UserId { get; set; }
        public string Username { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string Email { get; set; } = "";
    }
}