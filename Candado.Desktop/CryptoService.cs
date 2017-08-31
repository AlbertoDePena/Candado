using Candado.Core;
using Candado.Desktop.Contracts;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Candado.Desktop
{
    public class CryptoService : ICryptoService
    {
        private readonly ISecretKeyProvider SecretKeyProvider;

        public CryptoService(ISecretKeyProvider secretKeyProvider)
        {
            SecretKeyProvider = secretKeyProvider;
        }

        public string Decrypt(string encryptedText)
        {
            byte[] array;

            using (var stream = new MemoryStream())
            {
                var des = CreateDES();

                var decryptStream = new CryptoStream(stream, des.CreateDecryptor(), CryptoStreamMode.Write);

                var encryptedTextBytes = Convert.FromBase64String(encryptedText);

                decryptStream.Write(encryptedTextBytes, 0, encryptedTextBytes.Length);
                decryptStream.FlushFinalBlock();

                array = stream.ToArray();
            }

            return Encoding.Unicode.GetString(array);
        }

        public string Encrypt(string plainText)
        {
            byte[] array;

            using (var stream = new MemoryStream())
            {
                var des = CreateDES();

                var cryptStream = new CryptoStream(stream, des.CreateEncryptor(), CryptoStreamMode.Write);

                var plainTextBytes = Encoding.Unicode.GetBytes(plainText);

                cryptStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                cryptStream.FlushFinalBlock();

                array = stream.ToArray();
            }

            return Convert.ToBase64String(array);
        }

        private TripleDES CreateDES()
        {
            var key = SecretKeyProvider.GetSecretKey();

            var md5 = new MD5CryptoServiceProvider();

            var des = new TripleDESCryptoServiceProvider()
            {
                Key = md5.ComputeHash(Encoding.Unicode.GetBytes(key))
            };

            des.IV = new byte[des.BlockSize / 8];

            return des;
        }
    }
}
