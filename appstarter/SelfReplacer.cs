using System;
using System.Text;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace ws
{

    class SelfReplacer
    {
        // return true to close
        public static bool DoSelfReplace(string[] args) 
        {
            if ((args == null) || (args.Length < 3)) 
            {
                return false;
            }
            string currentExe = args[1];
            string newExe = args[2];
            StringBuilder sb = new StringBuilder();
            for (int i = 3; i < args.Length; i++)
            {
                sb.Append('\"').Append(args[i]).Append("\" ");
            }
            string currentExeArgs = sb.ToString().Trim();
            if (File.Exists(newExe))
            {
                if (Utils.CheckFileFullAccessWithDelay(currentExe) && Utils.CheckFileFullAccess(newExe, true))
                {
                    RawLog.Default.Log("srep<");
                    try
                    {
                        if (File.Exists(currentExe))
                        {
                            File.Delete(currentExe);
                        }
                        File.Move(newExe, currentExe);
                        Config.Default.StarterLastVersion = Config.Default.StarterNewVersion;
                        RawLog.Default.Log("srep>");
                    }
                    catch (Exception ex) { Utils.OnError(ex); }
                }
            }
            RawLog.Default.Log("rs");
            RawLog.Default.Dispose();
            System.Diagnostics.Process p = System.Diagnostics.Process.Start(currentExe, currentExeArgs);
            if (p != null)
            {
                p.Close();
                p = null;
            }
            return true;
        }

        // return true to close
        public static bool CheckReplace(string[] args)
        {
            string currentExe = Application.ExecutablePath;
            string replacerPath = Config.Default.GetStarterReplacerPath();
            if (File.Exists(replacerPath))
            {
                try
                {
                    if (Utils.CheckFileFullAccessWithDelay(replacerPath))
                    {
                        File.Delete(replacerPath);
                        RawLog.Default.Log("-srep");
                    }
                }
                catch
                {
                    // if we cannot delete replacer continue
                    return false;
                }
            }
            string newExePath = Config.Default.GetNewStarterPath();
            if (File.Exists(newExePath))
            {
                if (!Config.Default.IsNewStarterVersion) 
                {
                    // not for me
                    return false;
                }
                StringBuilder sb = new StringBuilder();
                sb.Append(Config.Default.GetCommandReplacerTag()).Append(" ");
                sb.Append('\"').Append(currentExe).Append("\" ");
                sb.Append('\"').Append(newExePath).Append("\" ");
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].StartsWith(Config.Default.CommandPrefix))
                    {
                        continue;
                    }
                    sb.Append('\"').Append(args[i]).Append("\" ");
                }
                string currentExeArgs = sb.ToString().Trim();
                File.Copy(currentExe, replacerPath);
                RawLog.Default.Log("srep+");
                RawLog.Default.Dispose();
                System.Diagnostics.Process p = System.Diagnostics.Process.Start(replacerPath, currentExeArgs);
                if (p != null)
                {
                    p.Close();
                    p = null;
                }
                return true;
            }
            return false;
        }


    }//EOC
}
