using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Gugubao.Utility
{
    public class Encryption
    {
        /// <summary>
        /// 小程序AES-128-CBC解密
        /// </summary>
        /// <param name="data">加密数据</param>
        /// <param name="iv">向量</param>
        /// <returns></returns>
        public static string DecryptCBC(string data, string sessionKey, string iv)
        {
            var oldBytes = Convert.FromBase64String(data);
            var bKey = new byte[16];
            Array.Copy(Convert.FromBase64String(sessionKey.PadRight(16)), bKey, 16);
            var bIv = new byte[16];
            Array.Copy(Convert.FromBase64String(iv.PadRight(16)), bIv, 16);

            using var rijalg = new RijndaelManaged
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                Key = bKey,
                IV = bIv,
            };

            var decryptor = rijalg.CreateDecryptor(rijalg.Key, rijalg.IV);

            var rtByte = decryptor.TransformFinalBlock(oldBytes, 0, oldBytes.Length);

            return Encoding.UTF8.GetString(rtByte);
        }
    }
}

