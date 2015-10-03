using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace ws
{
    enum StarterCommand { Default, ClearCache, RunLocal, ReplaceSelf, ShowLog, ClearLog, KillApp, ShowVer }

    class Config
    {
        public static Config Default = new Config();
        
        private KeyStore keyStore = new KeyStore();
        public KeyStore KeyStore { get { return this.keyStore; } }

        private string appId = "app1";
        private string appName = string.Empty;
        private string appSsl = string.Empty;
        private string appEnc = string.Empty;
        private string appUrl = "http://127.0.0.1/webstart/files.txt";
        private string appUrl2 = string.Empty;
        private bool checkRemovableDiskType = false;
        private bool reportFileSize = true;
        private double deltaCheckHours = 0.0;
        private double deltaCheckHoursRemovable = 0.0;
        private string commandPrefix = "--starter--";
        private string userAgentSuffix = string.Empty;
        private string userAgentSuffixAppArg = string.Empty;
        private string starterPathAppArg = string.Empty;
        private int downloadBufferDelayMls = 0;
        public string license = string.Empty;
        public int licenseWinW = -1;
        public int licenseWinH = -1;
        public bool checkRemoteFileDate = true;
        public bool updateBeforeStart = false;

        private bool removableDiskTypeSet = false;
        private bool isRemovableDiskType = false;

        public const string WStarter = "WStarter";
        public static readonly string OSVersion = Environment.OSVersion.ToString().Replace("Microsoft Windows ", "MSW").Replace("Service Pack ", "SP").Replace(' ', '_');
        public const string WStarterVersion = "1.0.2";
        public const string WStarterStoreVersion = "1.0.0";
        private const string starter = "starter.";
        private const string app = "app.";

        //
        private const string PAppSsl = app + "sslcerthash";
        private const string PAppEnc = app + "enc";
        private const string PAppGuid = app + "guid";
        private const string PAppTitle = app + "title";
        private const string PAppUrl = app + "url";
        private const string PAppUrl2 = app + "url2";
        private const string PAppLicense = app + "license";
        private const string PSRemDisk = starter + "checkremovabledisk";
        private const string PSReportFileSize = starter + "reportfilesize";
        private const string PSDHours = starter + "deltacheckhours";
        private const string PSDHoursRem = starter + "deltacheckhoursrem";
        private const string PSCmdPrefix = starter + "cmdprefix";
        private const string PSUASuffix = starter + "useragentsuffix";
        private const string PSUASuffixArg = app + "arg.useragentsuffix";
        private const string PSStarterPathArg = app + "arg.starterpath";
        private const string PSDownDelay = starter + "downdelaymls";
        private const string PAppLicW = app + "license.w";
        private const string PAppLicH = app + "license.h";
        private const string PSCheckRemoteDate = starter + "checkremotedate";
        private const string PSUpdateBeforeStart = starter + "updatebeforestart";

        public Config()
        {
#if DEBUG
        appId = "{A35A0085-486F-4ff9-8BD1-D057FBE0AD5F}";
        //appUrl = "http://www.myplanetsoft.com/webstart/wt/files.txt"; 
        appUrl = "http://127.0.0.1/myplanet/webstart/wt/1/lfiles.txt"; //ocal
        appUrl2 = "http://127.0.0.1/myplanet/webstart/wt/2/lfiles.txt"; 
        appName = "Test";
        userAgentSuffix = "WT";
        userAgentSuffixAppArg = "--starter--appua-";
        starterPathAppArg = "--starter--path-";
        downloadBufferDelayMls = 50;
#endif
        }

        #region commands

        public const string CommandReplacerTag = "self-replace-mode{B4508516-F79C-4f50-B7E6-A2E8CCB8D6DB}--";
        public const string CommandClearCache = "uninstall--";
        public const string CommandRunLocal = "run-local--";
        public const string CommandShowLog = "show-log--";
        public const string CommandClearLog = "clear-log--";
        public const string CommandKillApp = "kill-app--";
        public const string CommandShowVersion = "show-info--";

        public StarterCommand GetCommand(string command) 
        {
            if (string.IsNullOrEmpty(command)) return StarterCommand.Default;
            if (!command.StartsWith(commandPrefix)) return StarterCommand.Default;
            string cmd = command.Substring(commandPrefix.Length);
            switch (cmd) 
            {
                case Config.CommandClearCache:
                    return StarterCommand.ClearCache;
                case Config.CommandRunLocal:
                    return StarterCommand.RunLocal;
                case Config.CommandReplacerTag:
                    return StarterCommand.ReplaceSelf;
                case Config.CommandShowLog:
                    return StarterCommand.ShowLog;
                case Config.CommandClearLog:
                    return StarterCommand.ClearLog;
                case Config.CommandKillApp:
                    return StarterCommand.KillApp;
                case Config.CommandShowVersion:
                    return StarterCommand.ShowVer;
                default:
                    //throw new Exception("? " + command);
                    return StarterCommand.Default;
            }
        }

        public string GetCommandReplacerTag() 
        {
            return commandPrefix + Config.CommandReplacerTag;
        }

        public string CommandPrefix 
        {
            get { return this.commandPrefix; }
            set
            {
                if (string.IsNullOrEmpty(value)) return;
                value = value.Trim();
                if (value.IndexOfAny(new char[]{' ', '\t'}) >= 0) return;
                this.commandPrefix = value;
            }
        }
        
        #endregion commands
  
        public string GetShowVesion() 
        {
            const string star = "#";
            string aua = UserAgentAppArg;
            const string EQ = "=";

            string text =
                     PAppGuid + EQ + AppId + Environment.NewLine
                   + PAppEnc + EQ + AppEnc + Environment.NewLine
                   + PAppSsl + EQ + AppSsl + Environment.NewLine
                   + PAppTitle + EQ + appName + Environment.NewLine
                   + PAppUrl + EQ + this.AppUrl + Environment.NewLine
                   + PAppUrl2 + EQ + this.AppUrl2 + Environment.NewLine
                   + PSRemDisk + EQ + (checkRemovableDiskType ? "1" : "0") + Environment.NewLine
                   + PSReportFileSize + EQ + (reportFileSize ? "1" : "0") + Environment.NewLine
                   + PSDHours + EQ + ((long)deltaCheckHours).ToString() + Environment.NewLine
                   + PSDHoursRem + EQ + ((long)deltaCheckHoursRemovable).ToString() + Environment.NewLine
                   + PSCheckRemoteDate + EQ + (this.checkRemoteFileDate ? "1" : "0") + Environment.NewLine
                   + PSCmdPrefix + EQ + this.commandPrefix + Environment.NewLine
                   + PSUASuffix + EQ + this.userAgentSuffix + Environment.NewLine
                   + PSUASuffixArg + EQ + userAgentSuffixAppArg + Environment.NewLine
                   + PSStarterPathArg + EQ + this.starterPathAppArg + Environment.NewLine
                   + PSUpdateBeforeStart + EQ + (this.updateBeforeStart ? "1" : "0") + Environment.NewLine
                   + PSDownDelay + EQ + this.downloadBufferDelayMls + Environment.NewLine
                   + PAppLicW + EQ + licenseWinW.ToString() + Environment.NewLine
                   + PAppLicH + EQ + licenseWinH.ToString() + Environment.NewLine;

            string[] lic = this.license.Split('\n');
            for (int i = 0; i < lic.Length; i++)
            {
                string l = lic[i].Trim('\n', '\r');
                if (string.IsNullOrEmpty(l)) continue;
                text += PAppLicense + EQ + l + Environment.NewLine;
            }
                   
            text += Environment.NewLine
                   + Str.Def.ToString()
                   + Environment.NewLine
                   + "# internal" + Environment.NewLine
                   + star + starter + "ver: " + Config.WStarterVersion + " " + StarterLastVersion + Environment.NewLine
                   + star + starter + "store.ver: " + Config.WStarterStoreVersion + Environment.NewLine
                   + star + starter + "md5: " + Crc.HashFile(Application.ExecutablePath, true) + Environment.NewLine
                   + star + starter + "useragent" + ": " + this.UserAgent + Environment.NewLine
                   + star + starter + PSUASuffixArg + ":" + (string.IsNullOrEmpty(aua) ? string.Empty : aua) + Environment.NewLine
                   + star + starter + app + "folder: " + this.AppFolder + Environment.NewLine
                   + star + starter + app + "newver: " + (this.NewVersionFileTag ? "1" : "0") + Environment.NewLine
                   + star + starter + "self.newver: " + (IsNewStarterVersion ? "1" : "0") + " " + this.StarterNewVersion + Environment.NewLine
                   + star + starter + "isremovabledisk: " + (this.IsRemovableDiskType ? "1" : "0") + Environment.NewLine
                   + star + starter + "notallowed: " + Utils.NotAllowedCharsStr()  + Environment.NewLine
                   ;
            return text;
        }

        public string UserAgentAppArg
        {
            get
            {
                if (string.IsNullOrEmpty(this.userAgentSuffix)) return null;
                if (string.IsNullOrEmpty(this.userAgentSuffixAppArg)) return null;
                return "\"" + this.userAgentSuffixAppArg + this.userAgentSuffix + "\"";
            }
        }

        public string StarterPathAppArg
        {
            get
            {
                if (string.IsNullOrEmpty(this.starterPathAppArg)) return null;
                return "\"" + this.starterPathAppArg + Application.ExecutablePath + "\"";
            }
        }
        
        #region data

        public bool HasLicense
        {
            get { return !string.IsNullOrEmpty(this.license); }
        }

        public int DownDelay 
        {
            get { return downloadBufferDelayMls; }
        }

        public double DeltaCheckHours
        {
            get
            {
                if (IsRemovableDiskType)
                {
                    return DeltaCheckHoursRemovable;
                }
                return DeltaCheckHoursStatic;
            }
        }

        private double DeltaCheckHoursStatic
        {
            get { return deltaCheckHours; }
            set 
            {
                if (value < 0.0) value = 0.0;
                deltaCheckHours = value;
            }
        }

        private double DeltaCheckHoursRemovable
        {
            get { return deltaCheckHoursRemovable; }
            set
            {
                if (value < 0.0) value = 0.0;
                deltaCheckHoursRemovable = value;
            }
        }

        public bool IsRemovableDiskType 
        {
            get
            {
                SetIsRemovableDisk(); 
                return isRemovableDiskType; 
            }
        }

        public bool ReportFileSize
        {
            get { return reportFileSize; }
        }

        public string AppName 
        {
            get { return appName; }
        }

        public string AppUrl
        {
            get
            { 
                return appUrl;
            }
        }

        public string AppUrl2
        {
            get
            {
                return appUrl2;
            }
        }

        public string AppId 
        {
            get { return appId; }
            set
            {
                appId = Utils.ClearStr(value); 
            }
        }

        public string AppSsl
        {
            get
            {
                return appSsl;
            }
        }

        public string AppEnc
        {
            get
            {
                return appEnc;
            }
        }

        public bool AppEncOn
        {
            get
            {
                return !string.IsNullOrEmpty(AppEnc);
            }
        }

        #endregion data

        #region corepaths

        private void SetIsRemovableDisk() 
        {
            if (!removableDiskTypeSet)
            {
                lock (this)
                {
                    if (!removableDiskTypeSet)
                    {
                        removableDiskTypeSet = true;
                        if (this.checkRemovableDiskType)
                        {
                            isRemovableDiskType = (GetDiskType(Application.ExecutablePath) == System.IO.DriveType.Removable);
                        }
                    }
                }
            }
        }

        public string WStarterPath 
        {
            get 
            {
                SetIsRemovableDisk();
                string path =
                    isRemovableDiskType
                    ? Path.GetDirectoryName(Application.ExecutablePath)
                    : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                path = Path.Combine(path, Config.WStarter);
                return path;
            }
        }

        public string AppFolder 
        {
            get
            {
                string path = WStarterPath;
                path = Path.Combine(path, AppId);
                return path;
            }
        }
      
        public enum PathType { Direct, Working, Temp }
        public string GetPath(string relativePath, PathType pathType, bool isDir)
        {
            string path = AppFolder;
            if (pathType != PathType.Direct) 
            {
                bool isTemp = (pathType == PathType.Temp);
                path = Path.Combine(AppFolder, isTemp ? "Temp" : "Working");
            }
            if (!string.IsNullOrEmpty(relativePath))
            {
                path = Path.Combine(path, relativePath);
            }
            string dir = isDir ? path : Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return path;
        }

        public string GetPath(string relativePath, bool isTemp, bool isDir)
        {
            return GetPath(relativePath, isTemp ? PathType.Temp : PathType.Working, isDir);
        }

        public string GetDirectAppPath(string suffix) 
        {
            return GetPath(AppId + suffix, PathType.Direct, false);
        }

        #endregion corepaths

        public string GetFileListPath(bool isTemp) 
        {
            return GetPath(AppId + ".version", isTemp, false);
        }

        public bool NewVersionFileTag 
        {
            get 
            {
                return File.Exists(GetNewVersionFileTagPath());
            }
            set
            {
                string f = GetNewVersionFileTagPath();
                if (value)
                {
                    using (Stream s = Utils.OpenWrite(f))
                    { }
                }
                else
                {
                    Utils.DeleteFile(f);
                }
            }
        }

        public bool IsNewStarterVersion
        {
            get
            {
                string newStarterVersion = StarterNewVersion;
                if (!newStarterVersion.Equals(StarterLastVersion))
                {
                    return true;
                }
                return false;
            }
        }

        public string StarterNewVersion
        {
            get
            {
                return Config.Default.KeyStore.GetString(KeyStoreIds.NewStarterVersionS, Config.WStarterVersion, false);
            }
            set
            {
                if (string.IsNullOrEmpty(value)) value = WStarterVersion;
                this.KeyStore.SetString(KeyStoreIds.NewStarterVersionS, value, true);
            }
        }


        public string StarterLastVersion
        {
            get
            {
                return Config.Default.KeyStore.GetString(KeyStoreIds.StarterLastVersionS, Config.WStarterVersion, false);
            }
            set
            {
                if (string.IsNullOrEmpty(value)) value = WStarterVersion;
                this.KeyStore.SetString(KeyStoreIds.StarterLastVersionS, value, true);
            }
        }

        private string GetNewVersionFileTagPath()
        {
            return GetDirectAppPath(".newversion");
        }

        public string GetNewStarterPath() 
        {
            return GetDirectAppPath(".starter");
        }

        public string GetStarterReplacerPath()
        {
            return GetDirectAppPath(".exe");
        }

        public string GetLogPath()
        {
            return GetDirectAppPath(".log");
        }

        private static System.IO.DriveType GetDiskType(string disk)
        {
            string name = string.Empty;
            if (disk.StartsWith("A:")) return System.IO.DriveType.Removable;
            System.IO.DriveInfo drive = GetDrive(disk);
            if (drive != null)
            {
                return drive.DriveType;
            }
            return System.IO.DriveType.Unknown;
        }

        public static System.IO.DriveInfo GetDrive(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            path = Path.GetFullPath(path);
            if (Utils.IsNetworkPath(path)) 
            {
                return null;
            }
            path = path.Substring(0, 2);
            System.IO.DriveInfo[] drives = System.IO.DriveInfo.GetDrives();
            foreach (System.IO.DriveInfo drive in drives)
            {
                if (drive.Name.StartsWith(path))
                {
                    return drive;
                }
            }
            return null;
        }

        public string UserAgent
        {
            get
            {
                string t = Config.WStarter
                    + " " + Config.WStarterVersion
                    + " " + AppId
                    + " [" + Config.OSVersion + "]"
                    + " " + System.Threading.Thread.CurrentThread.CurrentCulture.Name
                    + " CLR" + Environment.Version.ToString()
                    ;
                if (IsRemovableDiskType) 
                {
                    t += " RemovableDisk";
                }
                if (!string.IsNullOrEmpty(this.userAgentSuffix)) 
                {
                    t += " " + this.userAgentSuffix;
                }
                return t;
            }
        }

        private void CheckStoreVersion() 
        {
            string storeVersion = KeyStore.GetString(KeyStoreIds.StoreVersionS, WStarterStoreVersion, true);
            if (!storeVersion.Equals(WStarterStoreVersion))
            {
                Local.ClearCache();
                RawLog.Default.Init(); // restart log
            }
            KeyStore.SetString(KeyStoreIds.StoreVersionS, WStarterStoreVersion, true);
        }

        public void Load(string data) 
        {
            ClearStreams();
            if (string.IsNullOrEmpty(data))
            {
                using (Stream s = Utils.OpenRead(Application.ExecutablePath))
                {
                    int state = 0;
                    while (true)
                    {
                        if (state == 5)
                        {
                            break;
                        }
                        int b = s.ReadByte();
                        if (b < 0) break;
                        switch (b)
                        {
                            case '#': state = 1; break;
                            case '@': if (state > 0)
                                {
                                    state++;
                                }
                                break;
                            default:
                                state = 0;
                                break;
                        }

                    }
                    if (state == 5)
                    {
                        long currentPosition = s.Position;
                        byte[] uft8bom = new byte[3];
                        int r = s.Read(uft8bom, 0, uft8bom.Length);
                        if ((r < 3) || !IsBom(uft8bom))
                        {
                            s.Seek(currentPosition, SeekOrigin.Begin);
                        }
                        using (StreamReader sr = new StreamReader(s, Encoding.UTF8, false))
                        {
                            data = sr.ReadToEnd();
                        }
                    }
                }
            }
            if(!string.IsNullOrEmpty(data))
            {
                LoadFromText(data);
            }
            KeyStore.path = GetStoreVersionPath();
            CheckStoreVersion();
        }

        private void LoadFromText(string data)
        {
            if (data == null) throw new ApplicationException("Not configured!");
            string[] lines = data.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                line = line.Trim(' ', '\t', '\r', '\n');
                if ((line.Length <= 0) || line.StartsWith("#")) continue;
                int idx = line.IndexOf('=');
                if (idx <= 0) continue;
                string key = line.Substring(0, idx).Trim(' ', '\t', '\r', '\n');
                string val = line.Substring(idx + 1).Trim(' ', '\t', '\r', '\n');
                if (string.IsNullOrEmpty(key)) continue;
                if (string.IsNullOrEmpty(val)) continue;
                switch (key.ToLower())
                {
                    case PAppSsl: this.appSsl = val; break;
                    case PAppEnc: this.appEnc = val; break;
                    case PAppGuid: AppId = val; break;
                    case PAppTitle: this.appName = val; break;
                    case PAppUrl: this.appUrl = val; break;
                    case PAppUrl2: this.appUrl2 = val; break;
                    case PSRemDisk: checkRemovableDiskType = val.Equals("1") ? true : false; break;
                    case PSReportFileSize: reportFileSize = val.Equals("1") ? true : false; break;
                    case PSDHours: DeltaCheckHoursStatic = (double)Convert.ToInt64(val, System.Globalization.CultureInfo.InvariantCulture); break;
                    case PSDHoursRem: DeltaCheckHoursRemovable = (double)Convert.ToInt64(val, System.Globalization.CultureInfo.InvariantCulture); break;
                    case PSCmdPrefix: CommandPrefix = Utils.ClearStr(val, true); break;
                    case PSUASuffix: this.userAgentSuffix = Utils.ClearStr(val); break;
                    case PSUASuffixArg: this.userAgentSuffixAppArg = Utils.ClearStr(val, true); break;
                    case PSDownDelay: downloadBufferDelayMls = Convert.ToInt32(val, System.Globalization.CultureInfo.InvariantCulture); break;
                    case PAppLicense: this.license += val.Replace("^p", Environment.NewLine).Replace("^t", "\t") + Environment.NewLine; break;
                    case PAppLicW: this.licenseWinW = Convert.ToInt32(val, System.Globalization.CultureInfo.InvariantCulture); break;
                    case PAppLicH: this.licenseWinH = Convert.ToInt32(val, System.Globalization.CultureInfo.InvariantCulture); break;
                    case PSStarterPathArg: starterPathAppArg = Utils.ClearStr(val, true); break;
                    case PSCheckRemoteDate: checkRemoteFileDate = val.Equals("1") ? true : false; break;
                    case PSUpdateBeforeStart: updateBeforeStart = val.Equals("1") ? true : false; break;
                    default:
                        if (key.StartsWith(Str.SPrefix))
                        {
                            Str.Def.Replace(key, val);
                        }
                        break;
                }
            }
        }
                



        private string GetStoreVersionPath()
        {
            return GetDirectAppPath(".store");
        }

        private static bool IsBom(byte[] b)
        {
            if (b == null || b.Length < 3) return false;
            return ((b[0] == 0xef) && (b[1] == 0xbb) && (b[2] == 0xbf));
        }

        private void ClearStreams() 
        {
            try
            {
                FileStreams fs = new FileStreams(Application.ExecutablePath);
                if ((fs != null) && (fs.Count > 0))
                {
                    foreach (StreamInfo s in fs)
                    {
                        s.Delete();
                    }
                }
            }
            catch (Exception xx) { Utils.OnError(xx); }
        }

    }//EOC
}
