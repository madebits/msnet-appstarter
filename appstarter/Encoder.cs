using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Security.Cryptography;

namespace ws
{
    public class Encoder
    {
        public static void Encode(string path, string pass, bool encode)
        {
            if (string.IsNullOrEmpty(pass)) return;
            string tmp = path + ".tmp";
            using (Stream input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024 * 5))
            {
                using (Stream output = new FileStream(tmp, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1024 * 1024 * 5))
                {
                    Transform(input, output, encode, pass);
                }
            }
            File.Delete(path);
            File.Move(tmp, path);
        }

        private static RNGCryptoServiceProvider Rand = new RNGCryptoServiceProvider();
        public static void Transform(
            Stream input,
            Stream output,
            bool encrypt,
            string password,
            int aesKeySizeInBits = 128)
        {
            try
            {
                byte[] salt = new byte[aesKeySizeInBits / 8];
                byte[] iv = new byte[16];
                if (encrypt)
                {
                    Rand.GetBytes(salt);
                    Rand.GetBytes(iv);
                    output.Write(salt, 0, salt.Length);
                    output.Write(iv, 0, iv.Length);
                }
                else
                {
                    input.Read(salt, 0, salt.Length);
                    input.Read(iv, 0, iv.Length);
                }
                PasswordDeriveBytes pdb = new PasswordDeriveBytes(password, salt, "SHA256", 1024);
                using (RijndaelManaged aes = new RijndaelManaged())
                {
                    aes.KeySize = aesKeySizeInBits;
                    byte[] key = pdb.GetBytes(aes.KeySize / 8);
                    aes.Mode = CipherMode.CBC;
                    ICryptoTransform cryptoTransform = encrypt
                        ? aes.CreateEncryptor(key, iv)
                        : aes.CreateDecryptor(key, iv);
                    using (CryptoStream cs = new CryptoStream(encrypt ? output : input, cryptoTransform,
                        encrypt ? CryptoStreamMode.Write : CryptoStreamMode.Read))
                    {
                        if (encrypt)
                        {
                            WriteStream(input, cs);
                            cs.FlushFinalBlock();
                        }
                        else
                        {
                            WriteStream(cs, output);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Failed to " + (encrypt ? "encrypt" : "decrypt") + " data!", ex);
            }
        }

        private static void WriteStream(Stream ins, Stream outs)
        {
            byte[] buffer = new byte[1024];
            while (true)
            {
                int r = ins.Read(buffer, 0, buffer.Length);
                if (r <= 0)
                {
                    break;
                }
                outs.Write(buffer, 0, r);
            }
        }
    }
}
