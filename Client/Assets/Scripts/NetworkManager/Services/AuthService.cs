using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using gbBase;
using NetworkManager.Core;
using NetworkManager.Messages;
using NetworkManager.Utils;

namespace NetworkManager.Services
{
    /// <summary>
    /// 인증 서비스 클래스
    /// 회원가입, 로그인, 로그아웃 등을 담당합니다
    /// </summary>
    public class AuthService : SingletonBase<AuthService>
    {
        [Header("인증 상태 (읽기 전용)")]
        [SerializeField] private bool _isAuthenticating = false;
        [SerializeField] private string _lastAuthError = "";
        
        // 응답 대기용
        private UniTaskCompletionSource<AuthResult> _currentAuthTask;
        
        // 프로퍼티
        public bool IsAuthenticating => _isAuthenticating;
        public string LastAuthError => _lastAuthError;
        
        // 인증 이벤트
        public event Action<AuthResult> OnAuthenticationCompleted;
        public event Action<string> OnAuthenticationFailed;
        
        protected void Awake()
        {
            // NetworkManager 메시지 수신 이벤트 구독
            if (NetworkManager.Core.NetworkManager.Instance != null)
            {
                NetworkManager.Core.NetworkManager.Instance.OnMessageReceived += HandleNetworkMessage;
            }
        }
        
        private void OnDestroy()
        {
            if (NetworkManager.Core.NetworkManager.Instance != null)
            {
                NetworkManager.Core.NetworkManager.Instance.OnMessageReceived -= HandleNetworkMessage;
            }
        }
        
        /// <summary>
        /// 이메일/패스워드로 회원가입
        /// </summary>
        public async UniTask<AuthResult> RegisterAsync(string email, string password, string nickname)
        {
            if (_isAuthenticating)
            {
                return new AuthResult { IsSuccess = false, ErrorMessage = "이미 인증 중입니다" };
            }
            
            try
            {
                _isAuthenticating = true;
                _lastAuthError = "";
                
                LogDebug($"회원가입 시도: {email}, {nickname}");
                
                var authMessage = new AuthMessage
                {
                    AuthType = AuthType.Register,
                    Email = email,
                    Password = password, // 실제로는 클라이언트에서 해시 처리 필요
                    Nickname = nickname,
                    UserId = email, // 이메일을 UserId로 사용
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
                
                return await SendAuthRequestAsync(authMessage);
            }
            catch (Exception ex)
            {
                var errorMsg = $"회원가입 중 예외 발생: {ex.Message}";
                LogError(errorMsg);
                return new AuthResult { IsSuccess = false, ErrorMessage = errorMsg };
            }
            finally
            {
                _isAuthenticating = false;
            }
        }
        
        /// <summary>
        /// 이메일/패스워드로 로그인
        /// </summary>
        public async UniTask<AuthResult> LoginAsync(string email, string password)
        {
            if (_isAuthenticating)
            {
                return new AuthResult { IsSuccess = false, ErrorMessage = "이미 인증 중입니다" };
            }
            
            try
            {
                _isAuthenticating = true;
                _lastAuthError = "";
                
                LogDebug($"로그인 시도: {email}");
                
                var authMessage = new AuthMessage
                {
                    AuthType = AuthType.Login,
                    Email = email,
                    Password = password, // 실제로는 클라이언트에서 해시 처리 필요
                    UserId = email, // 이메일을 UserId로 사용
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
                
                return await SendAuthRequestAsync(authMessage);
            }
            catch (Exception ex)
            {
                var errorMsg = $"로그인 중 예외 발생: {ex.Message}";
                LogError(errorMsg);
                return new AuthResult { IsSuccess = false, ErrorMessage = errorMsg };
            }
            finally
            {
                _isAuthenticating = false;
            }
        }
        
        /// <summary>
        /// 토큰 기반 인증 (기존 Server_Manager 방식)
        /// </summary>
        public async UniTask<AuthResult> AuthenticateWithTokenAsync(string authKey, int platformType = 1, string userId = "")
        {
            if (_isAuthenticating)
            {
                return new AuthResult { IsSuccess = false, ErrorMessage = "이미 인증 중입니다" };
            }
            
            try
            {
                _isAuthenticating = true;
                _lastAuthError = "";
                
                if (string.IsNullOrEmpty(userId))
                    userId = NetworkManager.Core.NetworkManager.Instance?.Config?.defaultAuthId ?? "testuser";
                
                LogDebug($"토큰 인증 시도: {userId}");
                
                var authMessage = new AuthMessage
                {
                    AuthType = AuthType.TokenAuth,
                    AuthKey = authKey,
                    PlatformType = platformType,
                    UserId = userId,
                    Token = authKey, // 초기 토큰으로 authKey 사용
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
                
                return await SendAuthRequestAsync(authMessage);
            }
            catch (Exception ex)
            {
                var errorMsg = $"토큰 인증 중 예외 발생: {ex.Message}";
                LogError(errorMsg);
                return new AuthResult { IsSuccess = false, ErrorMessage = errorMsg };
            }
            finally
            {
                _isAuthenticating = false;
            }
        }
        
        /// <summary>
        /// 로그아웃
        /// </summary>
        public async UniTask<bool> LogoutAsync()
        {
            try
            {
                LogDebug("로그아웃 시도");
                
                if (SessionManager.Instance.IsSessionValid)
                {
                    await SessionManager.Instance.EndSessionAsync();
                }
                
                LogDebug("로그아웃 완료");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"로그아웃 중 예외 발생: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 토큰 갱신 요청
        /// </summary>
        public async UniTask<AuthResult> RefreshTokenAsync(string currentToken)
        {
            if (_isAuthenticating)
            {
                return new AuthResult { IsSuccess = false, ErrorMessage = "이미 인증 중입니다" };
            }
            
            try
            {
                _isAuthenticating = true;
                _lastAuthError = "";
                
                LogDebug("토큰 갱신 시도");
                
                var authMessage = new AuthMessage
                {
                    AuthType = AuthType.RefreshToken,
                    Token = currentToken,
                    UserId = SessionManager.Instance.CurrentUser?.UserId ?? "",
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
                
                return await SendAuthRequestAsync(authMessage);
            }
            catch (Exception ex)
            {
                var errorMsg = $"토큰 갱신 중 예외 발생: {ex.Message}";
                LogError(errorMsg);
                return new AuthResult { IsSuccess = false, ErrorMessage = errorMsg };
            }
            finally
            {
                _isAuthenticating = false;
            }
        }
        
        /// <summary>
        /// 자동 재인증 (연결 복구 후)
        /// </summary>
        public async UniTask<bool> AutoReauthenticateAsync()
        {
            try
            {
                if (!SessionManager.Instance.IsSessionValid)
                {
                    LogDebug("유효한 세션이 없어 자동 재인증을 건너뜁니다");
                    return false;
                }
                
                LogDebug("자동 재인증 시도");
                
                var config = NetworkManager.Core.NetworkManager.Instance?.Config;
                if (config != null && !string.IsNullOrEmpty(config.defaultAuthKey))
                {
                    var result = await AuthenticateWithTokenAsync(config.defaultAuthKey, config.defaultPlatformType, config.defaultAuthId);
                    return result.IsSuccess;
                }
                
                LogDebug("자동 재인증을 위한 설정이 없습니다");
                return false;
            }
            catch (Exception ex)
            {
                LogError($"자동 재인증 중 예외 발생: {ex.Message}");
                return false;
            }
        }
        
        // Private Methods
        
        /// <summary>
        /// 인증 요청 전송 및 응답 대기
        /// </summary>
        private async UniTask<AuthResult> SendAuthRequestAsync(AuthMessage authMessage)
        {
            try
            {
                // 응답 대기용 TaskCompletionSource 생성
                _currentAuthTask = new UniTaskCompletionSource<AuthResult>();
                
                // 네트워크 연결 확인
                if (NetworkManager.Core.NetworkManager.Instance == null || !NetworkManager.Core.NetworkManager.Instance.IsConnected)
                {
                    return new AuthResult 
                    { 
                        IsSuccess = false, 
                        ErrorMessage = "서버에 연결되어 있지 않습니다" 
                    };
                }
                
                // 인증 요청 전송
                var sendResult = await NetworkManager.Core.NetworkManager.Instance.SendMessageAsync(authMessage);
                if (!sendResult)
                {
                    return new AuthResult 
                    { 
                        IsSuccess = false, 
                        ErrorMessage = "인증 요청 전송 실패" 
                    };
                }
                
                LogDebug($"인증 요청 전송 완료: {authMessage.AuthType}");
                
                // 서버 응답 대기 (30초 타임아웃)
                var timeoutTask = UniTask.Delay(30000);
                var authTask = _currentAuthTask.Task;
                
                var (hasResultLeft, result) = await UniTask.WhenAny(authTask, timeoutTask);
                
                if (hasResultLeft) // 인증 응답 수신
                {
                    return await authTask;
                }
                else // 타임아웃
                {
                    return new AuthResult 
                    { 
                        IsSuccess = false, 
                        ErrorMessage = "인증 응답 시간 초과" 
                    };
                }
            }
            catch (Exception ex)
            {
                LogError($"인증 요청 처리 중 예외: {ex.Message}");
                return new AuthResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = ex.Message 
                };
            }
            finally
            {
                _currentAuthTask = null;
            }
        }
        
        /// <summary>
        /// 네트워크 메시지 처리
        /// </summary>
        private void HandleNetworkMessage(INetworkMessage message)
        {
            if (message is AuthMessage authMessage)
            {
                HandleAuthResponse(authMessage);
            }
        }
        
        /// <summary>
        /// 인증 응답 처리
        /// </summary>
        private void HandleAuthResponse(AuthMessage response)
        {
            try
            {
                LogDebug($"인증 응답 수신: {response.AuthType}, 결과: {response.ResultCode}");
                
                var result = new AuthResult();
                
                if (response.ResultCode == 0) // 성공
                {
                    result.IsSuccess = true;
                    result.SessionToken = !string.IsNullOrEmpty(response.RetPassKey) ? 
                        GenerateSessionToken(response) : response.Token;
                    
                    // 사용자 정보 생성
                    result.UserInfo = new UserInfo(
                        response.UserId,
                        response.Email,
                        response.Nickname
                    );
                    
                    // 세션 시작
                    if (SessionManager.Instance != null)
                    {
                        SessionManager.Instance.StartSession(result.SessionToken, result.UserInfo);
                    }
                    
                    LogDebug($"인증 성공: {result.UserInfo.Nickname}");
                    OnAuthenticationCompleted?.Invoke(result);
                }
                else // 실패
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = !string.IsNullOrEmpty(response.ResultMessage) ? 
                        response.ResultMessage : $"인증 실패 (코드: {response.ResultCode})";
                    
                    _lastAuthError = result.ErrorMessage;
                    LogError($"인증 실패: {result.ErrorMessage}");
                    OnAuthenticationFailed?.Invoke(result.ErrorMessage);
                }
                
                // 대기 중인 작업 완료
                _currentAuthTask?.TrySetResult(result);
            }
            catch (Exception ex)
            {
                LogError($"인증 응답 처리 중 예외: {ex.Message}");
                
                var errorResult = new AuthResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = ex.Message 
                };
                
                _currentAuthTask?.TrySetResult(errorResult);
            }
        }
        
        /// <summary>
        /// 세션 토큰 생성 (기존 Server_Manager 방식)
        /// </summary>
        private string GenerateSessionToken(AuthMessage response)
        {
            try
            {
                if (string.IsNullOrEmpty(response.RetPassKey))
                {
                    LogError("PassKey가 없어 세션 토큰을 생성할 수 없습니다");
                    return "";
                }
                
                // BearerTokenGenerator 사용 (기존 코드)
                var sessionToken = BearerTokenGenerator.GenerateToken(
                    response.RetPassKey,
                    response.UserId,
                    response.Timestamp,
                    "PADDING_1",
                    "PADDING_2",
                    response.RetSubPassKey
                );
                
                LogDebug($"세션 토큰 생성 완료 (길이: {sessionToken?.Length ?? 0})");
                return sessionToken ?? "";
            }
            catch (Exception ex)
            {
                LogError($"세션 토큰 생성 실패: {ex.Message}");
                return "";
            }
        }
        
        // Utility Methods
        
        private void LogDebug(string message)
        {
            var config = NetworkManager.Core.NetworkManager.Instance?.Config;
            if (config != null && config.enableDebugLog)
            {
                Debug.Log($"[AuthService] {message}");
            }
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[AuthService] {message}");
        }
    }
    
    /// <summary>
    /// 인증 결과 클래스
    /// </summary>
    [Serializable]
    public class AuthResult
    {
        public bool IsSuccess;
        public string ErrorMessage = "";
        public string SessionToken = "";
        public UserInfo UserInfo;
        public float ExpireTimeSeconds = 3600f; // 1시간 기본값
        
        public AuthResult()
        {
        }
        
        public AuthResult(bool success, string errorMessage = "", string sessionToken = "", UserInfo userInfo = null)
        {
            IsSuccess = success;
            ErrorMessage = errorMessage;
            SessionToken = sessionToken;
            UserInfo = userInfo;
        }
    }
}