using System;
using UnityEngine;

namespace NetworkManager.Utils
{
    /// <summary>
    /// 사용자 정보 클래스
    /// </summary>
    [Serializable]
    public class UserInfo
    {
        [Header("사용자 기본 정보")]
        public string UserId = "";
        public string Email = "";
        public string Nickname = "";
        public int Level = 1;
        public long Experience = 0;
        
        [Header("플랫폼 정보")]
        public int PlatformType = 1; // 1: Android, 2: iOS, 3: PC
        public string PlatformId = "";
        
        [Header("게임 정보")]
        public string CurrentRoomId = "";
        public int Wins = 0;
        public int Losses = 0;
        public int TotalGames = 0;
        
        [Header("접속 정보")]
        public DateTime LastLoginTime = DateTime.MinValue;
        public DateTime CreatedTime = DateTime.MinValue;
        
        public UserInfo()
        {
        }
        
        public UserInfo(string userId, string email, string nickname)
        {
            UserId = userId;
            Email = email;
            Nickname = nickname;
            CreatedTime = DateTime.UtcNow;
        }
        
        /// <summary>
        /// 승률 계산
        /// </summary>
        public float GetWinRate()
        {
            if (TotalGames == 0) return 0f;
            return (float)Wins / TotalGames * 100f;
        }
        
        /// <summary>
        /// 사용자 정보 문자열 반환
        /// </summary>
        public override string ToString()
        {
            return $"사용자: {Nickname} (ID: {UserId})\n" +
                   $"레벨: {Level} (경험치: {Experience})\n" +
                   $"승부: {Wins}승 {Losses}패 (승률: {GetWinRate():F1}%)\n" +
                   $"플랫폼: {PlatformType}\n" +
                   $"최근 접속: {LastLoginTime:yyyy-MM-dd HH:mm}";
        }
    }
}