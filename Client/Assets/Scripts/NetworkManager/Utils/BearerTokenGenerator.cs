using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public class UserInfoToken
{
    public string UserId { get; set; } = "";
    public long BaseTimeStamp { get; set; } = 0;
}

public class BearerTokenGenerator
{
    public static string GenerateToken(string secretKey, string userId, long baseTimeStamp, string padding1, string padding2, string strIV)
    {
        // SessionToken 생성
        string combinedString = $"{padding1}.{userId}.{baseTimeStamp}.{padding2}. ";

        // AES 암호화
        string encryptedString = EncryptWithAES256(combinedString, secretKey, strIV);
        return encryptedString;
    }

    private static string EncryptWithAES256(string plainText, string key, string strIV)
    {
        // AES 키와 IV 설정
        using var aes = Aes.Create();
        aes.Key = GenerateKey(key); // 32바이트 키 생성
        aes.IV = Encoding.UTF8.GetBytes(strIV);
        aes.Mode = CipherMode.CBC; // CBC 모드 사용
        aes.Padding = PaddingMode.PKCS7; // PKCS7 패딩 사용

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        using var writer = new StreamWriter(cs, Encoding.UTF8);

        // 평문을 암호화
        writer.Write(plainText);
        writer.Close();

        // IV와 암호화된 데이터를 결합하여 반환
        byte[] encryptedData = ms.ToArray();
        byte[] result = new byte[aes.IV.Length + encryptedData.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encryptedData, 0, result, aes.IV.Length, encryptedData.Length);

        return Convert.ToBase64String(result);
    }

    private static byte[] GenerateKey(string key)
    {
        // AES 키는 32바이트여야 함
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(key)); // 32바이트 키 생성
    }

    private static byte[] GenerateFixedIV()
    {
        // 특정 문자열을 기반으로 IV 생성
        string ivString = "Sal10zioOsIVfewE"; // 16바이트 길이의 문자열이어야 함
        if (ivString.Length != 16)
        {
            throw new ArgumentException("IV 문자열은 반드시 16바이트(문자)여야 합니다.");
        }

        return Encoding.UTF8.GetBytes(ivString);
    }
}
