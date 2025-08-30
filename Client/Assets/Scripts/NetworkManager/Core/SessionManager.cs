using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using gbBase;
using NetworkManager.Utils;
using NetworkManager.Messages;

namespace NetworkManager.Core
{
    /// <summary>
    /// 세션 관리 클래스
    /// 사용자 로그인 세션, 토큰 갱신 등을 담당합니다
    /// </summary>
    public class SessionManager : SingletonBase<SessionManager>
    {
        [Header("세션 설정")]
        [SerializeField] private float _sessionDurationSeconds = 3600f; // 1시간
        [SerializeField] private bool _autoRefreshToken = true;
        [SerializeField] private float _refreshThreshold = 300f; // 5분 전에 갱신
        
        [Header("현재 세션 상태 (읽기 전용)")]
        [SerializeField] private bool _isSessionValid;
        [SerializeField] private string _sessionToken = "";
        [SerializeField] private UserInfo _currentUser;
        [SerializeField] private DateTime _sessionCreatedTime;
        [SerializeField] private DateTime _sessionExpireTime;
        
        // 세션 갱신 관련
        private bool _isRefreshingToken = false;
        private bool _isRefreshTaskRunning = false;
        
        // 프로퍼티
        public bool IsSessionValid => _isSessionValid && !string.IsNullOrEmpty(_sessionToken) && DateTime.UtcNow < _sessionExpireTime;
        public string SessionToken => _sessionToken;
        public UserInfo CurrentUser => _currentUser;
        public DateTime SessionCreatedTime => _sessionCreatedTime;
        public DateTime SessionExpireTime => _sessionExpireTime;
        
        // 세션 이벤트
        public event Action<UserInfo> OnSessionStarted;
        public event Action OnSessionExpired;
        public event Action OnSessionRefreshed;
        public event Action<string> OnTokenRefreshFailed;
        
        protected void Awake()
        {
            InitializeSession();
        }
        
        private void Update()
        {
            // 세션 유효성 자동 체크
            CheckSessionValidityPeriodically();
        }
        
        private void OnDestroy()
        {
            StopTokenRefreshTimer();
        }
        
        /// <summary>
        /// 세션 초기화
        /// </summary>
        private void InitializeSession()
        {
            var config = NetworkManager.Instance?.Config;
            if (config != null)
            {
                _sessionDurationSeconds = config.sessionExpireTime;
                _autoRefreshToken = config.autoRefreshToken;
                _refreshThreshold = config.refreshThreshold;
            }
            
            Debug.Log("[SessionManager] 초기화 완료");
        }
        
        /// <summary>
        /// 새 세션 시작 (로그인/회원가입 성공 후 호출)
        /// </summary>
        public void StartSession(string sessionToken, UserInfo userInfo, float expireTimeSeconds = 0)
        {
            try
            {
                if (string.IsNullOrEmpty(sessionToken))
                {
                    Debug.LogError("[SessionManager] 빈 세션 토큰으로 세션을 시작할 수 없습니다");
                    return;
                }
                
                if (userInfo == null)
                {
                    Debug.LogError("[SessionManager] 사용자 정보 없이 세션을 시작할 수 없습니다");
                    return;
                }
                
                // 기존 세션 정리
                if (_isSessionValid)
                {
                    Debug.Log("[SessionManager] 기존 세션을 종료하고 새 세션을 시작합니다");
                    StopTokenRefreshTimer();
                }
                
                // 새 세션 정보 설정
                _sessionToken = sessionToken;
                _currentUser = userInfo;
                _sessionCreatedTime = DateTime.UtcNow;
                _sessionExpireTime = _sessionCreatedTime.AddSeconds(expireTimeSeconds > 0 ? expireTimeSeconds : _sessionDurationSeconds);
                _isSessionValid = true;
                
                // 사용자 최근 접속 시간 업데이트
                _currentUser.LastLoginTime = DateTime.UtcNow;
                
                Debug.Log($"[SessionManager] 세션 시작: {userInfo.Nickname} (만료: {_sessionExpireTime:yyyy-MM-dd HH:mm:ss})");
                
                // NetworkManager에 토큰 설정
                NetworkManager.Instance?.SetSessionToken(sessionToken);
                
                // 이벤트 발생
                OnSessionStarted?.Invoke(userInfo);
                
                // 자동 토큰 갱신 시작
                if (_autoRefreshToken)
                {
                    StartTokenRefreshTimer();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SessionManager] 세션 시작 중 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 세션 종료 (로그아웃 시 호출)
        /// </summary>
        public async UniTask EndSessionAsync()
        {
            try
            {
                Debug.Log("[SessionManager] 세션 종료 요청");
                
                if (!_isSessionValid)
                {
                    Debug.Log("[SessionManager] 이미 세션이 종료되어 있습니다");
                    return;
                }
                
                // 로그아웃 메시지 전송 (필요한 경우)
                if (NetworkManager.Instance != null && NetworkManager.Instance.IsConnected)
                {
                    var logoutMessage = new AuthMessage
                    {
                        AuthType = AuthType.Logout,
                        UserId = _currentUser?.UserId ?? "",
                        Token = _sessionToken
                    };
                    
                    await NetworkManager.Instance.SendMessageAsync(logoutMessage);
                }
                
                // 세션 정리
                ClearSession();
                
                Debug.Log("[SessionManager] 세션 종료 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SessionManager] 세션 종료 중 오류: {ex.Message}");
                ClearSession(); // 오류가 발생해도 세션은 정리
            }
        }
        
        /// <summary>
        /// 세션 유효성 검사
        /// </summary>
        public bool ValidateSession()
        {
            if (!_isSessionValid || string.IsNullOrEmpty(_sessionToken))
            {
                return false;
            }
            
            if (DateTime.UtcNow >= _sessionExpireTime)
            {
                Debug.LogWarning("[SessionManager] 세션 만료됨");
                HandleSessionExpired();
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 토큰 수동 갱신
        /// </summary>
        public async UniTask<bool> RefreshTokenAsync()
        {
            if (!_isSessionValid || _isRefreshingToken)
            {
                Debug.LogWarning("[SessionManager] 토큰 갱신 불가: 세션 무효 또는 이미 갱신 중");
                return false;
            }
            
            try
            {
                _isRefreshingToken = true;
                Debug.Log("[SessionManager] 토큰 갱신 시도...");
                
                // RefreshToken 메시지 생성 및 전송
                var refreshMessage = new AuthMessage
                {
                    AuthType = AuthType.RefreshToken,
                    UserId = _currentUser?.UserId ?? "",
                    Token = _sessionToken
                };
                
                var success = await NetworkManager.Instance.SendMessageAsync(refreshMessage);
                
                if (success)
                {
                    Debug.Log("[SessionManager] 토큰 갱신 요청 전송 완료 (응답 대기 중...)");
                    // 실제 토큰 갱신은 서버 응답에서 처리됨
                    return true;
                }
                else
                {
                    Debug.LogError("[SessionManager] 토큰 갱신 요청 전송 실패");
                    OnTokenRefreshFailed?.Invoke("토큰 갱신 요청 전송 실패");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SessionManager] 토큰 갱신 예외: {ex.Message}");
                OnTokenRefreshFailed?.Invoke(ex.Message);
                return false;
            }
            finally
            {
                _isRefreshingToken = false;
            }
        }
        
        /// <summary>
        /// 서버 응답으로 토큰 갱신 (AuthService에서 호출)
        /// </summary>
        public void UpdateSessionToken(string newToken, float expireTimeSeconds = 0)
        {
            try
            {
                if (string.IsNullOrEmpty(newToken))
                {
                    Debug.LogError("[SessionManager] 빈 토큰으로 갱신할 수 없습니다");
                    return;
                }
                
                _sessionToken = newToken;
                _sessionExpireTime = DateTime.UtcNow.AddSeconds(expireTimeSeconds > 0 ? expireTimeSeconds : _sessionDurationSeconds);
                
                // NetworkManager에 새 토큰 설정
                NetworkManager.Instance?.SetSessionToken(newToken);
                
                Debug.Log($"[SessionManager] 토큰 갱신 완료 (새 만료시간: {_sessionExpireTime:yyyy-MM-dd HH:mm:ss})");
                OnSessionRefreshed?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SessionManager] 토큰 갱신 처리 중 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 남은 세션 시간 반환
        /// </summary>
        public TimeSpan GetRemainingTime()
        {
            if (!_isSessionValid) return TimeSpan.Zero;
            
            var remaining = _sessionExpireTime - DateTime.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
        
        /// <summary>
        /// 세션 정보 문자열 반환
        /// </summary>
        public string GetSessionInfo()
        {
            if (!_isSessionValid)
                return "세션: 비활성";
                
            var remaining = GetRemainingTime();
            return $"사용자: {_currentUser?.Nickname ?? "Unknown"}\n" +
                   $"세션 유효: {IsSessionValid}\n" +
                   $"남은 시간: {remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}\n" +
                   $"토큰 길이: {_sessionToken.Length}\n" +
                   $"자동 갱신: {_autoRefreshToken}";
        }
        
        // Private Methods
        
        /// <summary>
        /// 주기적 세션 유효성 체크
        /// </summary>
        private void CheckSessionValidityPeriodically()
        {
            if (!_isSessionValid) return;
            
            // 5초마다 체크
            if (Time.frameCount % (60 * 5) == 0) // 60fps 기준 5초
            {
                if (!IsSessionValid && _isSessionValid)
                {
                    HandleSessionExpired();
                }
            }
        }
        
        /// <summary>
        /// 세션 만료 처리
        /// </summary>
        private void HandleSessionExpired()
        {
            Debug.LogWarning("[SessionManager] 세션 만료 처리");
            ClearSession();
            OnSessionExpired?.Invoke();
        }
        
        /// <summary>
        /// 세션 정보 정리
        /// </summary>
        private void ClearSession()
        {
            _isSessionValid = false;
            _sessionToken = "";
            _currentUser = null;
            _sessionCreatedTime = DateTime.MinValue;
            _sessionExpireTime = DateTime.MinValue;
            
            // NetworkManager에서 토큰 제거
            NetworkManager.Instance?.SetSessionToken("");
            
            // 토큰 갱신 타이머 중지
            StopTokenRefreshTimer();
            
            Debug.Log("[SessionManager] 세션 정보 정리 완료");
        }
        
        /// <summary>
        /// 토큰 갱신 타이머 시작
        /// </summary>
        private void StartTokenRefreshTimer()
        {
            if (!_autoRefreshToken) return;
            
            StopTokenRefreshTimer(); // 기존 타이머 중지
            _isRefreshTaskRunning = true;
            TokenRefreshLoopAsync().Forget();
            Debug.Log("[SessionManager] 자동 토큰 갱신 타이머 시작");
        }
        
        /// <summary>
        /// 토큰 갱신 타이머 중지
        /// </summary>
        private void StopTokenRefreshTimer()
        {
            _isRefreshTaskRunning = false;
            Debug.Log("[SessionManager] 자동 토큰 갱신 타이머 중지");
        }
        
        /// <summary>
        /// 토큰 갱신 루프
        /// </summary>
        private async UniTaskVoid TokenRefreshLoopAsync()
        {
            try
            {
                while (_isSessionValid && _autoRefreshToken && _isRefreshTaskRunning)
                {
                    var remaining = GetRemainingTime();
                    
                    // 임계값 이하로 남았으면 갱신 시도
                    if (remaining.TotalSeconds <= _refreshThreshold && remaining.TotalSeconds > 0)
                    {
                        Debug.Log($"[SessionManager] 토큰 만료 {remaining.TotalSeconds:F0}초 전, 자동 갱신 시도");
                        await RefreshTokenAsync();
                    }
                    
                    // 1분마다 체크
                    await UniTask.Delay(60000);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SessionManager] 토큰 갱신 루프 오류: {ex.Message}");
            }
        }
    }
}