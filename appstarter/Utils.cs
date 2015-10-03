using System;
using System.Collections;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace ws
{
    class Monitor
    {
        public delegate bool DShouldStop(int waitTimeMls);
        public delegate void DLogProgress(int p);
        public delegate void DLog(string s, bool isError);

        public DShouldStop shouldStop = null;
        public event DLogProgress OnLogProgress = null;
        public event DLog OnLog = null;

        public bool ShouldStop() 
        {
            return ShouldStop(0);
        }

        public bool ShouldStop(int waitTimeMls)
        {
            if (shouldStop == null) return false;
            return shouldStop(waitTimeMls);
        }

        public void LogProgress(int p) 
        {
            if (OnLogProgress == null) return;
            OnLogProgress(p);
        }

        public void Log(string s)
        {
            Log(s, false);
        }
        
        public void Log(string s, bool isError)
        {
            if (OnLog == null) return;
            OnLog(s, isError);
        }

    }//EOC

    class Utils
    {
        public static Stream OpenRead(string file)
        {
            return new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024 * 5);
        }

        public static Stream OpenWrite(string file) 
        {
            return new FileStream(file, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1024 * 1024 * 5);
        }

        public static void WriteStream(Stream ins, Stream outs, Monitor m, long insLength)
        {
            byte[] emptyBuffer = new byte[0];
            byte[] buffer = new byte[512 * 1024];
            long totalLength = insLength;
            if (insLength < 0) 
            {
                try
                {
                    totalLength = ins.Length - ins.Position;
                }
                catch 
                {
                    totalLength = -1;
                }
            }
            long currentLength = 0;
            if (m != null)
            {
                m.LogProgress(0);
            }
            int lastProgress = 0;
            while(true)
            {
                if ((m != null) && m.ShouldStop()) 
                {
                    break;
                }
                int r = ins.Read(buffer, 0, buffer.Length);
                if (r <= 0)
                {
                    break;
                }
                if ((m != null) && m.ShouldStop())
                {
                    break;
                }
                outs.Write(buffer, 0, r);
                if ((m != null) && m.ShouldStop())
                {
                    break;
                }
                if ((m != null) && m.ShouldStop())
                {
                    break;
                }
                if ((m != null) && (totalLength > 0))
                {
                    currentLength += r;
                    int progress = (int)((double)currentLength / (double)totalLength * 100.0);
                    if (progress != lastProgress)
                    {
                        lastProgress = progress;
                        m.LogProgress(progress);
                    }
                }
                if ((m != null) && (Config.Default.DownDelay > 0)) 
                {
                    if (m.ShouldStop(Config.Default.DownDelay))
                    {
                        break;
                    }
                }
            }
            if ((m != null) && m.ShouldStop())
            {
                return;
            }
            if (m != null)
            {
                m.LogProgress(100);
            }
        }
                
        public static void OnError(Exception ex)
        {
            string msg = ((ex == null) ? null : ex.Message + " " + ex.StackTrace);
            RawLog.Default.Log(msg, true);
            DebugBreak();
        }

        public static void DebugBreak() 
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Break();
            }
        }

        public static void DeleteDir(string dir) 
        {
            if (dir == null) return;
            if (Directory.Exists(dir)) 
            {
                Directory.Delete(dir, true);
            }
        }

        public static bool DeleteFile(string f)
        {
            if (f == null) return false;
            if (File.Exists(f))
            {
                File.Delete(f);
                return true;
            }
            return false;
        }

        public static bool CheckFullAccess(string dir) 
        {
            if (dir == null) return true;
            try
            {
                string[] ff = Directory.GetFiles(dir);
                if (ff != null) 
                {
                    for (int i = 0; i < ff.Length; i++)
                    {
                        if (!CheckFileFullAccess(ff[i], false)) 
                        {
                            return false;
                        }
                    }
                }
                ff = Directory.GetDirectories(dir);
                if (ff != null)
                {
                    for (int i = 0; i < ff.Length; i++)
                    {
                        if (!CheckFullAccess(ff[i])) 
                        {
                            return false;
                        }
                    }
                }
            }
            catch 
            {
                return false;
            }
            return true;
        }

        public static bool CheckFileFullAccess(string file, bool checkExits) 
        {
            try
            {
                if (string.IsNullOrEmpty(file)) return false;
                if (checkExits)
                {
                    if (!File.Exists(file))
                    {
                        return true;
                    }
                }
                using (Stream s = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {

                }
            }
            catch
            {
                RawLog.Default.Log("!access " + file);
                return false;
            }
            return true;
        }

        public static bool CheckFileFullAccessWithDelay(string file) 
        {
            for(int i = 0; i < 20; i++) // 5 seconds
            {
                System.Diagnostics.Debug.WriteLine("free check nr " + (i + 1) + " " + file);
                if (CheckFileFullAccess(file, true))
                {
                    return true;
                }
                try
                {
                    System.Threading.Thread.Sleep(250);
                }
                catch (Exception xx) { Utils.OnError(xx); }
            }
            return false;
        }

        public static void CopyReplaceFiles(string srcDir, string destDir) 
        {
            string baseSrcDir = srcDir;
            if (!baseSrcDir.EndsWith("\\") || !baseSrcDir.EndsWith("/"))
            {
                baseSrcDir += Path.DirectorySeparatorChar.ToString();
            }
            MoveDir(srcDir, baseSrcDir, destDir);
        }

        private static void MoveDir(string srcDir, string baseSrcDir, string baseDestDir) 
        {
            string[] ff = Directory.GetFiles(srcDir);
            if (ff != null) 
            {
                for (int i = 0; i < ff.Length; i++) 
                {
                    MoveFile(ff[i], baseSrcDir, baseDestDir);
                }
            }
            string[] dd = Directory.GetDirectories(srcDir);
            if (dd != null) 
            {
                for (int i = 0; i < dd.Length; i++)
                {
                    MoveDir(dd[i], baseSrcDir, baseDestDir);
                }
            }
            // remove dir if empty
            DeleteEmptyDir(srcDir, false);
        }

        public static void DeleteEmptyDir(string srcDir, bool checkExits) 
        {
            if (srcDir == null) return;
            if (checkExits) 
            {
                if (!Directory.Exists(srcDir))
                {
                    return;
                }
            }
            string[] ff = Directory.GetFiles(srcDir);
            string[] dd = Directory.GetDirectories(srcDir);
            if (((ff == null) || (ff.Length <= 0))
                && ((dd == null) || (dd.Length <= 0)))
            {
                try
                {
                    Directory.Delete(srcDir, false);
                }
                catch (Exception xx) { OnError(xx); }
            }
        }

        private static void MoveFile(string srcFile, string baseSrcDir, string baseDestDir) 
        {
            string destFile = Path.Combine(baseDestDir, srcFile.Substring(baseSrcDir.Length, srcFile.Length - baseSrcDir.Length));
            string dir = Path.GetDirectoryName(destFile);
            if (!Directory.Exists(dir)) 
            {
                Directory.CreateDirectory(dir);
            }
            if(File.Exists(destFile))
            {
                File.Delete(destFile);
            }
            File.Move(srcFile, destFile);
        }

        public static string GetArgsString() 
        {
            string[] c = Environment.GetCommandLineArgs();
            if (c != null) 
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 1; i < c.Length; i++)
                {
                    if (c[i].StartsWith(Config.Default.CommandPrefix))
                    {
                        continue;
                    }
                    sb.Append('\"').Append(c[i]).Append("\" ");
                }
                string t = Config.Default.UserAgentAppArg;
                if (!string.IsNullOrEmpty(t))
                {
                    sb.Append(t).Append(' ');
                }
                t = Config.Default.StarterPathAppArg;
                if (!string.IsNullOrEmpty(t))
                {
                    sb.Append(t).Append(' ');
                }
                return sb.ToString().Trim(' ');
            }
            return null;
        }
        
        public static string SizeStr(long size) 
        {
            if (size < 0) return string.Empty;
            double kb = (double)size / 1024.0;
            double mb = kb / 1024.0;
            double gb = mb / 1024.0;
            if (gb >= 1.0) return TrimZeroesString(String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.00}", gb)) + " GB";
            if (mb >= 1.0) return TrimZeroesString(String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.00}", mb)) + " MB";
            if (kb >= 1.0) return TrimZeroesString(String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.00}", kb)) + " KB";
            return ((size > 0) ? "1 KB" : "0 KB");
        }

        private static string TrimZeroesString(string r)
        {
            if (r.IndexOf('.') > 0)
            {
                r = r.TrimEnd('.', '0');
            }
            return r;
        }

        public static void KillProcess(string process)
        {
            try
            {
                bool killCurrent = false;
                System.Diagnostics.Process current = System.Diagnostics.Process.GetCurrentProcess();
                System.Diagnostics.Process[] p = System.Diagnostics.Process.GetProcessesByName(process);
                int r = p.Length;
                for (int i = 0; i < p.Length; i++)
                {
                    if ((current != null) && (p[i].Id == current.Id))
                    {
                        killCurrent = true;
                    }
                    else
                    {
                        try
                        {
                            p[i].Kill();
                            p[i].Close();
                            p[i] = null;
                        }
                        catch (Exception xx) { OnError(xx); }
                    }
                }
                if (killCurrent)
                {
                    current.Kill();
                    current.Close();
                    current = null;
                }
            }
            catch (Exception xx) { OnError(xx); }
        }

        public static void KillApp(bool killSelf, bool unregister) 
        {
            ArrayList files = null;
            ArrayList filesAsoc = new ArrayList();
            try
            {
                files = RemoteFile.Load(Config.Default.GetFileListPath(false), ref filesAsoc);
            }
            catch (Exception xx) { Utils.OnError(xx); }
            RemoteFile.Start(files, true);
            if (unregister)
            {
                try
                {
                    FileAssoc.Register(filesAsoc, false);
                }
                catch (Exception xx) { Utils.OnError(xx); }
            }
            if (killSelf)
            {
                Utils.KillProcess(Path.GetFileNameWithoutExtension(Application.ExecutablePath));
            }
        }

        public static string ClearStr(string s)
        {
            return ClearStr(s, false);
        }

        public static readonly char[] NotAllowedChars = new char[] { 
            '\\', '/',
            '*', '?', '<', '>', '\"',
            ':', '|', '@', '$', '+', 
            '#', '%', '!', ' ', '\t',
            '\r', '\n', '\'' };

        public static string NotAllowedCharsStr()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < NotAllowedChars.Length; i++)
            {
                if ((NotAllowedChars[i] == '\t') || (NotAllowedChars[i] == '\r') || (NotAllowedChars[i] == '\n')) 
                {
                    continue;
                }
                sb.Append(NotAllowedChars[i]);
            }
            return sb.ToString();
        }

        public static string ClearStr(string s, bool allowSlash) 
        {
            if (s != null)
            {
                s = s.Trim(' ', '\t', '\r', '\n');
                for (int i = 0; i < NotAllowedChars.Length; i++) 
                {
                    if (allowSlash && (NotAllowedChars[i] == '/')) 
                    {
                        continue;
                    }
                    s = s.Replace(NotAllowedChars[i], '_');
                }
            }
            return s;
        }

        public static bool IsNetworkPath(string s) 
        {
            if (string.IsNullOrEmpty(s)) return false;
            return (s.StartsWith(@"\\") || s.StartsWith("//"));
        }
        
    }//EOC
}
