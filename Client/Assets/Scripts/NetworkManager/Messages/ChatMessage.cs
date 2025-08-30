using System;
using UnityEngine;
using Newtonsoft.Json;

namespace NetworkManager.Messages
{
    /// <summary>
    /// 채팅 메시지 클래스
    /// </summary>
    public class ChatMessage : INetworkMessage
    {
        public string MessageType => "CHAT";
        public string UserId { get; set; } = "";
        public string Token { get; set; } = "";
        public long Timestamp { get; set; }
        public bool RequiresAuth => true;

        [Header("채팅 정보")]
        public string RoomId { get; set; } = "";
        public string Content { get; set; } = "";
        public string SenderNickname { get; set; } = "";
        public ChatType Type { get; set; } = ChatType.Normal;

        public ChatMessage()
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public ChatMessage(string roomId, string content, ChatType type = ChatType.Normal) : this()
        {
            RoomId = roomId;
            Content = content;
            Type = type;
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
                Debug.LogError($"[ChatMessage] 직렬화 실패: {ex.Message}");
                return new byte[0];
            }
        }

        public void Deserialize(byte[] data)
        {
            try
            {
                var json = System.Text.Encoding.UTF8.GetString(data);
                var deserializedMessage = JsonConvert.DeserializeObject<ChatMessage>(json);
                
                if (deserializedMessage != null)
                {
                    RoomId = deserializedMessage.RoomId;
                    Content = deserializedMessage.Content;
                    SenderNickname = deserializedMessage.SenderNickname;
                    Type = deserializedMessage.Type;
                    UserId = deserializedMessage.UserId;
                    Token = deserializedMessage.Token;
                    Timestamp = deserializedMessage.Timestamp;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChatMessage] 역직렬화 실패: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 채팅 타입 열거형
    /// </summary>
    public enum ChatType
    {
        Normal = 0,      // 일반 채팅
        System = 1,      // 시스템 메시지
        Whisper = 2,     // 귓속말
        Team = 3,        // 팀 채팅
        Announcement = 4 // 공지사항
    }
}