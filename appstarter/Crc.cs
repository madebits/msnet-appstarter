using System;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace ws
{
    class Crc
    {
        private HashAlgorithm hasher = null;

        public const string Prefix = "###";

        public Crc() : this(false) { }
        public Crc(bool autoSalt)
        {
            if (autoSalt) 
            {
                Update(Config.Default.AppId);
            }
        }

        public void Update(string s)
        {
            if (s == null)
            {
                return;
            }
            byte[] b = Encoding.UTF8.GetBytes(s);
            Update(b, 0, b.Length);
        }
        
        public void Update(Stream s, Monitor m) 
        {
            if (s == null) return;
            byte[] buffer = new byte[1024 * 1024];
            if (m != null) m.LogProgress(0);
            long totalLength = s.Length;
            long currentLength = 0;
            int lastProgress = 0;
            while (true) 
            {
                if ((m != null) && m.ShouldStop()) 
                {
                    break;
                }
                int r = s.Read(buffer, 0, buffer.Length);
                if (r < 0) break;
                if (r == 0)
                {
                    if (s.Length != 0) break;
                    else
                    {
                        Update(buffer, 0, r);
                        break;
                    }
                }
                Update(buffer, 0, r);
                currentLength += r;
                if ((m != null) && (totalLength > 0)) 
                {
                    int progress = (int)((double)currentLength / (double)totalLength * 100.0);
                    if (progress != lastProgress)
                    {
                        lastProgress = progress;
                        m.LogProgress(progress);
                    }
                }
            }
            if (m != null) m.LogProgress(100);
        }

        public void Update(byte[] buffer, int offset, int length)
        {
            if (buffer == null) return;
            if (hasher == null)
            {
                hasher = new MD5CryptoServiceProvider();
            }
            hasher.TransformBlock(buffer, offset, length, buffer, 0);
        }

        public string GetValue()
        {
            return GetValue(true);
        }

        public string GetValue(bool reset)
        {
            if (hasher == null) return string.Empty;
            byte[] emptyBuffer = new byte[0];
            hasher.TransformFinalBlock(emptyBuffer, 0, 0);
            string s = Byte2Hex(hasher.Hash, null);
            if (reset)
            {
                hasher = null; // reset
            }
            return s;
        }

        public static string Byte2Hex(byte[] h, Monitor m)
        {
            if ((h == null) || (h.Length <= 0))
            {
                return string.Empty;
            }
            StringBuilder sb = new StringBuilder(h.Length * 2);
            for (int i = 0; i < h.Length; i++)
            {
                if ((m != null) && m.ShouldStop())
                {
                    return string.Empty;
                }
                sb.AppendFormat("{0:x2}", h[i]);
            }
            return sb.ToString();
        }

        public static string HashFile(string file, bool reportLength)
        {
            try
            {
                if (!File.Exists(file)) return string.Empty;
                HashAlgorithm hasher = new MD5CryptoServiceProvider();
                using (Stream s = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024))
                {
                    byte[] h = hasher.ComputeHash(s);
                    string t = Byte2Hex(hasher.Hash, null);
                    if (reportLength)
                    {
                        t += " - " + s.Length + " bytes - " + Utils.SizeStr(s.Length);
                    }
                    return t;
                }
            }
            catch (Exception xx) { Utils.OnError(xx); }
            return string.Empty;
        }

    }//EOC
}
