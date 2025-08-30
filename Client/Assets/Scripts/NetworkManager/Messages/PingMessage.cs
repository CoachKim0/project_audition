using System;
using UnityEngine;
using Newtonsoft.Json;

namespace NetworkManager.Messages
{
    /// <summary>
    /// 핑 메시지 클래스
    /// 연결 상태 확인용
    /// </summary>
    public class PingMessage : INetworkMessage
    {
        public string MessageType => "PING";
        public string UserId { get; set; } = "";
        public string Token { get; set; } = "";
        public long Timestamp { get; set; }
        public bool RequiresAuth => true;

        [Header("핑 정보")]
        public int SeqNo { get; set; }
        public long SendTime { get; set; }
        public long ReceiveTime { get; set; }

        public PingMessage()
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            SendTime = Timestamp;
        }

        public PingMessage(int seqNo) : this()
        {
            SeqNo = seqNo;
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
                Debug.LogError($"[PingMessage] 직렬화 실패: {ex.Message}");
                return new byte[0];
            }
        }

        public void Deserialize(byte[] data)
        {
            try
            {
                var json = System.Text.Encoding.UTF8.GetString(data);
                var deserializedMessage = JsonConvert.DeserializeObject<PingMessage>(json);
                
                if (deserializedMessage != null)
                {
                    SeqNo = deserializedMessage.SeqNo;
                    SendTime = deserializedMessage.SendTime;
                    ReceiveTime = deserializedMessage.ReceiveTime;
                    UserId = deserializedMessage.UserId;
                    Token = deserializedMessage.Token;
                    Timestamp = deserializedMessage.Timestamp;
                }
                
                ReceiveTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PingMessage] 역직렬화 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 핑 지연시간 계산 (밀리초)
        /// </summary>
        public long GetLatencyMs()
        {
            return (ReceiveTime - SendTime) * 1000;
        }
    }
}