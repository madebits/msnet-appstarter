using System;
using System.IO;
using System.Text;

namespace ws
{
    class RawLog : IDisposable
    {
        public static RawLog Default = new RawLog();
        private StreamWriter debugLog = null;
        private bool debugLogInited = false;
        private volatile bool disposed = false;

        ~RawLog() { Dispose(); }

        public void Dispose() 
        {
            try
            {
                disposed = true;
                lock (this)
                {
                    if (debugLog != null)
                    {
                        debugLog.Close();
                        debugLog = null;
                    }
                }
            }
            catch { Utils.DebugBreak(); }
        }

        public void Log(string s)
        {
            Log(s, false);
        }

        public void Log(string s, bool isError)
        {
            try
            {
                if (disposed) return;
                lock (this)
                {
                    if (debugLog == null)
                    {
                        Init();
                    }
                    if (s == null) s = "<NULL>";
                    if (isError) s = "Error: " + s;
                    debugLog.WriteLine(s);
                    debugLog.Flush();
                }
            }
            catch { Utils.DebugBreak(); }
        }

        public void Init() 
        {
            try
            {
                if (debugLogInited) return;
                if (disposed) return;
                lock (this)
                {
                    if (debugLogInited) return;
                    debugLogInited = true;
                    if (debugLog == null)
                    {
                        bool append = true;
                        string path = Config.Default.GetLogPath();
                        if (File.Exists(path))
                        {
                            FileInfo fi = new FileInfo(path);
                            if (fi.Length > 8 * 1024)
                            {
                                append = false;
                            }
                            fi = null;
                        }
                        debugLog = new StreamWriter(path, append);
                        debugLog.WriteLine("-Log " + Config.WStarterVersion + " " + Config.Default.StarterLastVersion + " -");
                        debugLog.Flush();
                    }
                }
            }
            catch { Utils.DebugBreak(); }
        }
    }//EOC
}
