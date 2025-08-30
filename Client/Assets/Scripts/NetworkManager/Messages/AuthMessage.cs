using System;
using UnityEngine;
using Newtonsoft.Json;

namespace NetworkManager.Messages
{
    /// <summary>
    /// 인증 관련 메시지
    /// </summary>
    public class AuthMessage : INetworkMessage
    {
        public string MessageType => "AUTH";
        public string UserId { get; set; } = "";
        public string Token { get; set; } = "";
        public long Timestamp { get; set; }
        public bool RequiresAuth => AuthType != AuthType.Login && AuthType != AuthType.Register;

        [Header("인증 정보")]
        public AuthType AuthType { get; set; } = AuthType.TokenAuth;
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string Nickname { get; set; } = "";
        public string AuthKey { get; set; } = "";
        public int PlatformType { get; set; } = 1;
        
        [Header("응답 데이터")]
        public string RetPassKey { get; set; } = "";
        public string RetSubPassKey { get; set; } = "";
        public int ResultCode { get; set; }
        public string ResultMessage { get; set; } = "";

        public AuthMessage()
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public byte[] Serialize()
        {
            try
            {
                var json = JsonConvert.SerializeObject(this);
                return System.Text.Encoding.UTF8.GetBytes(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthMessage] 직렬화 실패: {ex.Message}");
                return new byte[0];
            }
        }

        public void Deserialize(byte[] data)
        {
            try
            {
                var json = System.Text.Encoding.UTF8.GetString(data);
                var deserializedMessage = JsonConvert.DeserializeObject<AuthMessage>(json);
                
                if (deserializedMessage != null)
                {
                    AuthType = deserializedMessage.AuthType;
                    Email = deserializedMessage.Email;
                    Password = deserializedMessage.Password;
                    Nickname = deserializedMessage.Nickname;
                    AuthKey = deserializedMessage.AuthKey;
                    PlatformType = deserializedMessage.PlatformType;
                    RetPassKey = deserializedMessage.RetPassKey;
                    RetSubPassKey = deserializedMessage.RetSubPassKey;
                    ResultCode = deserializedMessage.ResultCode;
                    ResultMessage = deserializedMessage.ResultMessage;
                    UserId = deserializedMessage.UserId;
                    Token = deserializedMessage.Token;
                    Timestamp = deserializedMessage.Timestamp;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthMessage] 역직렬화 실패: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 인증 타입 열거형
    /// </summary>
    public enum AuthType
    {
        Login = 0,          // 이메일/패스워드 로그인
        Register = 1,       // 회원가입
        TokenAuth = 2,      // 토큰 인증 (기존 방식)
        Logout = 3,         // 로그아웃
        RefreshToken = 4    // 토큰 갱신
    }
}