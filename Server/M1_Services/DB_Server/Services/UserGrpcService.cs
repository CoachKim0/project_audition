using Grpc.Core;
using DbServer.Grpc;

namespace DbServer.Services;

public class UserGrpcService : DbServer.Grpc.UserService.UserServiceBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserGrpcService> _logger;

    public UserGrpcService(IUserService userService, ILogger<UserGrpcService> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public override async Task<GetUserResponse> GetUser(GetUserRequest request, ServerCallContext context)
    {
        try
        {
            var user = await _userService.GetUserAsync(request.UserId);

            if (user == null)
            {
                return new GetUserResponse();
            }

            return new GetUserResponse
            {
                UserId = user.UserId,
                Username = user.Username,
                Nickname = user.Nickname,
                Email = user.Email,
                CreatedAt = new DateTimeOffset(user.CreatedAt).ToUnixTimeSeconds(),
                LastLogin = user.LastLoginAt.HasValue ? new DateTimeOffset(user.LastLoginAt.Value).ToUnixTimeSeconds() : 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "gRPC 사용자 조회 요청 처리 중 오류 발생");
            return new GetUserResponse();
        }
    }

    public override async Task<UpdateUserResponse> UpdateUser(UpdateUserRequest request, ServerCallContext context)
    {
        try
        {
            var success = await _userService.UpdateUserAsync(
                request.UserId, 
                string.IsNullOrEmpty(request.Nickname) ? null : request.Nickname,
                string.IsNullOrEmpty(request.Email) ? null : request.Email);

            return new UpdateUserResponse
            {
                Success = success,
                Message = success ? "사용자 정보가 업데이트되었습니다." : "사용자 정보 업데이트에 실패했습니다."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "gRPC 사용자 정보 업데이트 요청 처리 중 오류 발생");
            
            return new UpdateUserResponse
            {
                Success = false,
                Message = "서버 오류가 발생했습니다."
            };
        }
    }

    public override async Task<GetUserListResponse> GetUserList(GetUserListRequest request, ServerCallContext context)
    {
        try
        {
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Min(Math.Max(1, request.PageSize), 100); // 최대 100개로 제한

            var users = await _userService.GetUsersAsync(page, pageSize);
            
            var response = new GetUserListResponse
            {
                TotalCount = users.Count()
            };

            foreach (var user in users)
            {
                response.Users.Add(new GetUserResponse
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Nickname = user.Nickname,
                    Email = user.Email,
                    CreatedAt = new DateTimeOffset(user.CreatedAt).ToUnixTimeSeconds(),
                    LastLogin = user.LastLoginAt.HasValue ? new DateTimeOffset(user.LastLoginAt.Value).ToUnixTimeSeconds() : 0
                });
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "gRPC 사용자 목록 조회 요청 처리 중 오류 발생");
            return new GetUserListResponse { TotalCount = 0 };
        }
    }
}