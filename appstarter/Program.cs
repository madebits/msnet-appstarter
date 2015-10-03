using System;
using System.Collections;
using System.Windows.Forms;
using System.Threading;
using System.IO;

using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace ws
{
    static class Program
    {

        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                if (Utils.IsNetworkPath(Application.ExecutablePath))
                {
                    throw new Exception(Str.Def.Get(Str.NoNetPath));
                }
                SetExceptionHandler();
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                try
                {
                    LoadSettings();
                }
                catch (Exception xx)
                {
                    // not alone may be
                    Utils.OnError(xx);
                    return;
                }
                StarterCommand starterCommand = StarterCommand.Default;
                if ((args != null) && (args.Length >= 1))
                {
                    starterCommand = Config.Default.GetCommand(args[0]);
                }

                if (!ExclusiveLocker.Default.Lock(2000)) 
                {
                    // not alone
                    if (starterCommand == StarterCommand.Default) 
                    {
                        if (Config.Default.updateBeforeStart)
                        {
                            return;
                        }
                        try
                        {
                            Local.Start(false);
                        }
                        catch { }
                    }
                    else if (starterCommand == StarterCommand.ShowVer) 
                    {
                        try
                        {
                            DLicense.ShowText(Config.Default.GetShowVesion());
                        }
                        catch { }
                    }
                    return;
                }

                // log is avilable after this point only
                try
                {
                    RawLog.Default.Init();
                    DownThread.Default.Monitor.OnLog += RawLog.Default.Log;
                }
                catch (Exception xx) { Utils.OnError(xx); }

                RawLog.Default.Log("Cmd " + starterCommand.ToString());
                string tempLogPath = Config.Default.GetLogPath();

                switch (starterCommand)
                {
                    case StarterCommand.ShowVer:
                        DLicense.ShowText(Config.Default.GetShowVesion());
                        return;
                    case StarterCommand.KillApp:
                        Utils.KillApp(true, false);
                        return;
                    case StarterCommand.ClearLog:
                        RawLog.Default.Dispose();
                        Utils.DeleteFile(tempLogPath);
                        return;
                    case StarterCommand.ShowLog:

                        if (File.Exists(tempLogPath))
                        {
                            System.Diagnostics.Process p = System.Diagnostics.Process.Start(tempLogPath);
                            if (p != null)
                            {
                                p.Close();
                                p = null;
                            }
                        }
                        return;
                    case StarterCommand.ClearCache:
                        try
                        {
                            Utils.KillApp(false, true);
                        }
                        catch (Exception xx) { Utils.OnError(xx); }
                        Thread.Sleep(1000);
                        Local.ClearCache();
                        return;
                    case StarterCommand.ReplaceSelf:
                        if (SelfReplacer.DoSelfReplace(args))
                        {
                            return;
                        }
                        break;
                    default:
                        // do nothing
                        break;
                }
                if (SelfReplacer.CheckReplace(args))
                {
                    return;
                }
                                
                ServicePointManager.ServerCertificateValidationCallback
                    += new RemoteCertificateValidationCallback(ValidateRemoteCertificate);

                bool guiMode = false;
                DownForm df = new DownForm();

                if (Config.Default.updateBeforeStart) 
                {
                    if (starterCommand == StarterCommand.RunLocal)
                    {
                        guiMode = StartIt();
                        if (!guiMode)
                        {
                            return;
                        }
                    }
                    RawLog.Default.Log("ui");
                    Application.Run(df);
                    return;
                }

                guiMode = StartIt();
                
                if (!guiMode)
                {
                    if (starterCommand == StarterCommand.RunLocal)
                    {
                        return;
                    }
                    df.SetInivisibleMode();
                }
                else
                {
                    RawLog.Default.Log("ui");
                }

                Application.Run(df);
            }
            catch (Exception ex)
            {

                string m = ex.Message;
#if DEBUG
                m += " " + ex.StackTrace;
#endif
                MessageBox.Show(null, m, Str.Def.Get(Str.Error) + " - " + Config.Default.AppName);
            }
            finally
            {
                try
                {
                    RawLog.Default.Dispose();
                }
                catch { }
                try
                {
                    ExclusiveLocker.Default.Dispose();
                }
                catch (Exception xx) { Utils.OnError(xx); }
            }
        }

        private static void LoadSettings()
        {
            string exePath = Application.ExecutablePath;
            string settingsFile = Path.Combine(Path.GetDirectoryName(exePath), Path.GetFileNameWithoutExtension(exePath) + ".debugconfig");
            string settings = null;
            if (File.Exists(settingsFile))
            {
                settings = File.ReadAllText(settingsFile);
            }
            Config.Default.Load(settings);
        }

        private static bool StartIt() 
        {
            bool guiMode = false;
            try
            {
                guiMode = Local.Start(true);
            }
            catch (Exception ex)
            {
                // something could be corrupted
                RawLog.Default.Log(Str.Def.Get(Str.Error) + " # " + ex.Message);
                Utils.DebugBreak();
                Local.ClearCacheNoError();
                guiMode = Local.Start(true);
            }
            return guiMode;
        }

        private static bool ValidateRemoteCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors policyErrors)
        {
            if (policyErrors != SslPolicyErrors.None)
            {
                RawLog.Default.Log("SslPolicyErrors " + ((int)policyErrors).ToString());
            }
            if (certificate != null)
            {
                RawLog.Default.Log("X509Certificate.Hash " + certificate.GetCertHashString());
            }
            if (!string.IsNullOrEmpty(Config.Default.AppSsl))
            {
                if (Config.Default.AppSsl.Equals("none", StringComparison.InvariantCultureIgnoreCase)) 
                {
                    return true;
                }
                return certificate.GetCertHashString().Equals(Config.Default.AppSsl, StringComparison.InvariantCultureIgnoreCase);
            }
            return policyErrors == SslPolicyErrors.None;
        }

        private static void SetExceptionHandler()
        {
            AppDomain.CurrentDomain.UnhandledException +=
                new UnhandledExceptionEventHandler(UnhandledException);
            Application.ThreadException +=
                new System.Threading.ThreadExceptionEventHandler(ThreadUnhandledException);
        }

        private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Utils.OnError(null);
        }

        static void ThreadUnhandledException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            Utils.OnError(null);
        }

    }//EOC
}
