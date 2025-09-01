using Microsoft.EntityFrameworkCore;
using DbServer.Data.Context;
using DbServer.Data.Entities;
using System.Security.Cryptography;
using System.Text;

namespace DbServer.Services;

public class AuthService : IAuthService
{
    private readonly GameDbContext _context;
    private readonly ILogger<AuthService> _logger;

    public AuthService(GameDbContext context, ILogger<AuthService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(bool Success, string? Token, string? UserId, long UserIdx, string Message)> LoginAsync(string username, string password)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

            if (user == null)
            {
                return (false, null, null, 0, "사용자를 찾을 수 없습니다.");
            }

            if (!VerifyPassword(password, user.PasswordHash))
            {
                return (false, null, null, 0, "비밀번호가 일치하지 않습니다.");
            }

            // 기존 활성 세션 비활성화
            await DeactivateUserSessionsAsync(user.UserId);

            // 새 토큰 생성
            var token = await GenerateTokenAsync(user.UserId);

            // 마지막 로그인 시간 업데이트
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("사용자 {Username} 로그인 성공", username);

            return (true, token, user.UserId, user.UserIdx, "로그인 성공");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "로그인 중 오류 발생: {Username}", username);
            return (false, null, null, 0, "로그인 중 오류가 발생했습니다.");
        }
    }

    public async Task<(bool Success, string? UserId, long UserIdx, string Message)> RegisterAsync(string username, string password, string email, string nickname)
    {
        try
        {
            // 중복 사용자명 체크
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username || u.Email == email);

            if (existingUser != null)
            {
                if (existingUser.Username == username)
                    return (false, null, 0, "이미 존재하는 사용자명입니다.");
                else
                    return (false, null, 0, "이미 존재하는 이메일입니다.");
            }

            // 새 사용자 생성
            var user = new User
            {
                Username = username,
                PasswordHash = HashPassword(password),
                Email = email,
                Nickname = string.IsNullOrEmpty(nickname) ? username : nickname,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("새 사용자 등록: {Username}", username);

            return (true, user.UserId, user.UserIdx, "회원가입이 완료되었습니다.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "회원가입 중 오류 발생: {Username}", username);
            return (false, null, 0, "회원가입 중 오류가 발생했습니다.");
        }
    }

    public async Task<(bool Valid, string? UserId, long UserIdx, string? Username)> ValidateTokenAsync(string token)
    {
        try
        {
            var session = await _context.UserSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Token == token && s.IsActive && s.ExpiresAt > DateTime.UtcNow);

            if (session?.User?.IsActive == true)
            {
                return (true, session.UserId, session.User.UserIdx, session.User.Username);
            }

            return (false, null, 0, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "토큰 검증 중 오류 발생");
            return (false, null, 0, null);
        }
    }

    public async Task<bool> LogoutAsync(string token)
    {
        try
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.Token == token && s.IsActive);

            if (session != null)
            {
                session.IsActive = false;
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "로그아웃 중 오류 발생");
            return false;
        }
    }

    public async Task<string> GenerateTokenAsync(string userId)
    {
        var token = GenerateSecureToken();
        
        var session = new UserSession
        {
            UserId = userId,
            Token = token,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(24), // 24시간 유효
            IsActive = true
        };

        _context.UserSessions.Add(session);
        await _context.SaveChangesAsync();

        return token;
    }

    private async Task DeactivateUserSessionsAsync(string userId)
    {
        var activeSessions = await _context.UserSessions
            .Where(s => s.UserId == userId && s.IsActive)
            .ToListAsync();

        foreach (var session in activeSessions)
        {
            session.IsActive = false;
        }

        await _context.SaveChangesAsync();
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var salt = "GameServer2024Salt"; // 실제로는 사용자별 랜덤 솔트 사용 권장
        var saltedPassword = password + salt;
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
        return Convert.ToBase64String(hashedBytes);
    }

    private static bool VerifyPassword(string password, string hash)
    {
        var hashedPassword = HashPassword(password);
        return hashedPassword == hash;
    }

    private static string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var tokenBytes = new byte[32];
        rng.GetBytes(tokenBytes);
        return Convert.ToBase64String(tokenBytes);
    }
}