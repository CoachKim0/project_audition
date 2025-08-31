using Microsoft.EntityFrameworkCore;
using DbServer.Data.Context;
using DbServer.Data.Entities;

namespace DbServer.Services;

public class UserService : IUserService
{
    private readonly GameDbContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(GameDbContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<User?> GetUserAsync(string userId)
    {
        try
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId && u.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "사용자 조회 중 오류 발생: {UserId}", userId);
            return null;
        }
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        try
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "사용자명으로 조회 중 오류 발생: {Username}", username);
            return null;
        }
    }

    public async Task<bool> UpdateUserAsync(string userId, string? nickname = null, string? email = null)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId && u.IsActive);

            if (user == null)
                return false;

            bool updated = false;

            if (!string.IsNullOrEmpty(nickname) && user.Nickname != nickname)
            {
                user.Nickname = nickname;
                updated = true;
            }

            if (!string.IsNullOrEmpty(email) && user.Email != email)
            {
                // 이메일 중복 체크
                var emailExists = await _context.Users
                    .AnyAsync(u => u.Email == email && u.UserId != userId && u.IsActive);

                if (emailExists)
                {
                    _logger.LogWarning("이메일 중복으로 업데이트 실패: {Email}", email);
                    return false;
                }

                user.Email = email;
                updated = true;
            }

            if (updated)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("사용자 정보 업데이트: {UserId}", userId);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "사용자 정보 업데이트 중 오류 발생: {UserId}", userId);
            return false;
        }
    }

    public async Task<IEnumerable<User>> GetUsersAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            return await _context.Users
                .Where(u => u.IsActive)
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "사용자 목록 조회 중 오류 발생");
            return Enumerable.Empty<User>();
        }
    }

    public async Task<bool> DeactivateUserAsync(string userId)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                return false;

            user.IsActive = false;
            
            // 활성 세션도 모두 비활성화
            var activeSessions = await _context.UserSessions
                .Where(s => s.UserId == userId && s.IsActive)
                .ToListAsync();

            foreach (var session in activeSessions)
            {
                session.IsActive = false;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("사용자 비활성화: {UserId}", userId);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "사용자 비활성화 중 오류 발생: {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> ActivateUserAsync(string userId)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                return false;

            user.IsActive = true;
            await _context.SaveChangesAsync();
            _logger.LogInformation("사용자 활성화: {UserId}", userId);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "사용자 활성화 중 오류 발생: {UserId}", userId);
            return false;
        }
    }
}