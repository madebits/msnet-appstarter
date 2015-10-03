using System;
using System.Collections;
using System.Text;
using System.IO;

namespace ws
{
    class RemoteFile : IComparable
    {
        public string url = null;
        public string name = null;
        public string displayName = string.Empty;
        public long size = -1;
        public string version = string.Empty;
        public string crc = string.Empty;
        public bool exe = false;
        public bool passArgs = false;

        public void Init(string line)
        {
            if (string.IsNullOrEmpty(line)) return;
            line = line.Trim(' ', '\t', '\r', '\n');
            if (string.IsNullOrEmpty(line)) return;
            Init(line.Split('|'));
        }

        public static string CleanString(string s) 
        {
            return s.Trim(' ', '\t', '\r', '\n', '|');
        }

        private static long Str2Size(string str)
        {
            str = CleanString(str);
            if (string.IsNullOrEmpty(str))
            {
                return -1;
            }
            return Convert.ToInt64(str, System.Globalization.CultureInfo.InvariantCulture);
        }
        
        public void Init(string[] p)
        {
            //0.url|1.file|2.version|3.exe|4.args|5.displaytext|6.size|7.crc

            this.url = CleanString(p[0]);
            this.name = CleanString(p[1]);
            if ((this.name.IndexOf(':') >= 0) || (this.name.IndexOf('%') >= 0)) // abs path?
            {
                throw new Exception("wrong name");
            }
            this.name = this.name.Replace('/', '\\');
            if (name.StartsWith("\\")) name = name.Substring(1);

            this.version = CleanString(p[2]);
            if (p.Length > 3)
            {
                this.exe = CleanString(p[3]).Equals("*");
            }
            if (p.Length > 4)
            {
                this.passArgs = CleanString(p[4]).Equals("*");
            }
            if (p.Length > 5)
            {
                this.displayName = CleanString(p[5]);
            }
            if (p.Length > 6)
            {
                this.size = Str2Size(p[6]);
            }
            if (p.Length > 7)
            {
                this.crc = CleanString(p[7]);
            }
        }

        public override string ToString()
        {
            if (!Valid) return string.Empty;
            return "~|" + name
                + "|" + version
                + "|" + (exe ? "*" : string.Empty)
                + "|" + (passArgs ? "*" : string.Empty)
                + "|"
                + "|" + (size < 0 ? string.Empty : size.ToString(System.Globalization.CultureInfo.InvariantCulture))
                + "|" + crc
                ; //+ displayName
        }

        public bool IsStarterReplacer 
        {
            get
            {
                return this.name.Equals("*starter*");
            }
        }

        public bool ValidUrl
        {
            get
            {
                return !string.IsNullOrEmpty(this.url) && !url.Equals('~');
            }
        }

        public bool Valid
        {
            get
            {
                return !string.IsNullOrEmpty(url)
                    && !string.IsNullOrEmpty(name)
                    && !string.IsNullOrEmpty(version)
                    ;
            }
        }

        public bool IsSizeValid() 
        {
            return (size >= 0);
        }

        public string GetPath(bool isTemp)
        {
            string path = null;
            if (this.IsStarterReplacer)
            {
                path = Config.Default.GetNewStarterPath();
            }
            else
            {
                path = Config.Default.GetPath(this.name, isTemp, false);
            }
            return path;
        }

        public bool ValidateSize(string file)
        {
            if (!File.Exists(file)) return false; //? yes false
            if (!this.IsSizeValid()) return true;
            FileInfo fi = new FileInfo(file);
            if (fi == null) return false;
            bool ok = ValidateSize(fi.Length);
            fi = null;
            return ok;
        }

        public bool ValidateSize(long fileSize) 
        {
            if (!this.IsSizeValid()) return true;
            return (fileSize == this.size);
        }

        public bool ShouldCheckCrc 
        {
            get { return !string.IsNullOrEmpty(crc); }
        }

        public string DisplayName 
        {
            get
            {
                return string.IsNullOrEmpty(displayName)
                    ? (string.IsNullOrEmpty(name) ? string.Empty : name)
                    : displayName;
            }
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;
            RemoteFile other = (RemoteFile)obj;
            int r = Compare(this.name, other.name);
            if (r != 0) return r;
            r = Compare(this.version, other.version);
            if (r != 0) return r;
            if (this.IsSizeValid() && other.IsSizeValid())
            {
                if (this.size < other.size) r = -1;
                if (this.size > other.size) r = 1;
                else r = 0;
            }
            if (r != 0) return r;
            if (!string.IsNullOrEmpty(this.crc) && !string.IsNullOrEmpty(other.crc))
            {
                r = Compare(this.crc.ToLower(), other.crc.ToLower());
            }
            return r;
        }

        public bool IsSameFile(RemoteFile other)
        {
            return (CompareTo(other) == 0);
        }

        public bool IsSamePathFile(RemoteFile other, ref bool sameVersion) 
        {
            sameVersion = false;
            int r = Compare(this.name, other.name);
            if (r == 0)
            {
                sameVersion = (Compare(this.version, other.version) == 0);
            }
            return (r == 0);
        }

        private static int Compare(string s1, string s2) 
        {
            if((s1 == null) && (s2 == null)) return 0;
            if(s1 == null) return -1;
            if(s2 == null) return 1;
            return s1.CompareTo(s2);
        }

        /*
        public static ArrayList Load(string file)
        {
            ArrayList fileAssoc = new ArrayList();
            return Load(file, ref fileAssoc);
        }
        */
        
        public static ArrayList Load(string file, ref ArrayList fileAssoc)
        {
            if (!File.Exists(file)) return null;
            using (Stream s = Utils.OpenRead(file)) 
            {
                return Load(s, ref fileAssoc);
            }
        }

        public static ArrayList Load(Stream s, ref ArrayList fileAssoc)
        {
            using (StreamReader sr = new StreamReader(s))
            {
                return Load(sr, ref fileAssoc);
            }
        }

        public static ArrayList Load(StreamReader sr, ref ArrayList fileAssoc)
        {
            if (fileAssoc == null) { fileAssoc = new ArrayList(); }
            fileAssoc.Clear();

            ArrayList files = new ArrayList();
            Hashtable variables = new Hashtable();
            string crc = string.Empty;
            Crc hash = new Crc(true);
            for (string line = sr.ReadLine(); line != null; line = sr.ReadLine())
            {
                line = line.Trim(' ', '\t', '\r', '\n');
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }
                else if (line.StartsWith(Crc.Prefix))
                {
                    crc = line.Substring(Crc.Prefix.Length);
                    continue;
                }
                else if (line.StartsWith("#") && !line.StartsWith("##"))
                {
                    continue;
                }
                else if (line.StartsWith("$$"))
                {
                    int idx = line.IndexOf('=');
                    if (idx < 0) continue;
                    string key = line.Substring(1, idx - 1).Trim();
                    string val = line.Substring(idx + 1).Trim();
                    variables[key] = val;
                    continue;
                }

                foreach (string k in variables.Keys)
                {
                    line = line.Replace(k, (string)variables[k]);
                }

                if (line.StartsWith(FileAssoc.Prefix))
                {
                    FileAssoc fa = new FileAssoc();
                    fa.FromString(line);
                    if (fa.Valid)
                    {
                        hash.Update(fa.ToString());
                        fileAssoc.Add(fa);
                    }
                    continue;
                }
                
                RemoteFile rm = new RemoteFile();
                rm.Init(line);
                if (rm.Valid)
                {
                    // add starter only if new
                    if (rm.IsStarterReplacer) 
                    {
                        if (rm.version.Equals(Config.Default.StarterLastVersion))
                        {
                            continue;
                        }
                        else
                        {
                            string newExePath = Config.Default.GetNewStarterPath();
                            if (File.Exists(newExePath)) 
                            {
                                string newExeVersion = Config.Default.StarterNewVersion;
                                if (newExeVersion.Equals(rm.version)) 
                                {
                                    // already downloaded this starter version
                                    continue;
                                }
                            }
                        }
                    }
                    hash.Update(rm.ToString());
                    files.Add(rm);
                }
            }
            if (!string.IsNullOrEmpty(crc)) 
            {
                string newCrc = hash.GetValue();
                if (!crc.ToLower().Equals(newCrc)) 
                {
                    throw new Exception(Str.Def.Get(Str.FailedCrc));
                }
            }
            return files;
        }

        public static void Save(string file, ArrayList files, ArrayList fileAssoc)
        {
            if (file == null) return;
            if (files == null) return;
            using (Stream s = Utils.OpenWrite(file)) 
            {
                Save(s, files, fileAssoc);
            }
        }

        public static void Save(Stream outs, ArrayList files, ArrayList fileAssoc)
        {
            Crc crc = new Crc(true);
            using (StreamWriter sw = new StreamWriter(outs))
            {
                if (files != null)
                {
                    foreach (RemoteFile rm in files)
                    {
                        if (rm.Valid && !rm.IsStarterReplacer)
                        {
                            string s = rm.ToString();
                            crc.Update(s);
                            sw.WriteLine(s);
                        }
                    }
                }
                if (fileAssoc != null)
                {
                    foreach (FileAssoc fa in fileAssoc)
                    {
                        if ((fa != null) && fa.Valid) 
                        {
                            string s = fa.ToString();
                            crc.Update(s);
                            sw.WriteLine(s);
                        }
                    }
                }
                sw.WriteLine(Crc.Prefix + crc.GetValue());
            }
        }

        public static int Start(ArrayList files)
        {
            return Start(files, false);
        }

        public static bool VerifyLocalFiles(ArrayList files) 
        {
            if ((files == null) || (files.Count < 0))
            {
                return true;
            }
            foreach (RemoteFile rm in files)
            {
                if (!rm.Valid) return false;
                string path = rm.GetPath(false);
                if (!File.Exists(path)) return false;
            }
            return true;
        }

        public static int Start(ArrayList files, bool kill) 
        {
            if ((files == null) || (files.Count < 0))
            {
                Remote.SetLastCheckDate(0);
                return 0;
            }
            if (!kill)
            {
                if (!VerifyLocalFiles(files))
                {
                    return 0;
                    //throw new Exception(Str.Def.Get(Str.FailedLength));
                }
            }
            int count = 0;
            string args = Utils.GetArgsString();
            foreach (RemoteFile rm in files)
            {
                if (rm.Valid && rm.exe && !rm.IsStarterReplacer)
                {
                    string path = rm.GetPath(false);
                    if (File.Exists(path))
                    {
                        count++;
                        if (kill)
                        {
                            string name = Path.GetFileNameWithoutExtension(path);
                            Utils.KillProcess(name);
                        }
                        else
                        {
                            if (!rm.ValidateSize(path))
                            {
                                throw new Exception(Str.Def.Get(Str.FailedLength));
                            }
                            System.Diagnostics.Process p = null;
                            if (rm.passArgs && !string.IsNullOrEmpty(args))
                            {
                                p = System.Diagnostics.Process.Start(path, args);
                            }
                            else
                            {
                                p = System.Diagnostics.Process.Start(path);
                            }
                            if (p != null)
                            {
                                p.Close();
                                p = null;
                            }
                        }
                    }
                }
            }
            return count;
        }

        public static ArrayList GetNewFiles(ArrayList localFiles, ArrayList remoteFiles)
        {
            return GetNewFiles(localFiles, remoteFiles, false, false);
        }

        public static ArrayList GetNewFiles(ArrayList localFiles, ArrayList remoteFiles, bool checkOnlyPath, bool isTemp) 
        {
            if ((remoteFiles == null) || (remoteFiles.Count <= 0))
            {
                return null;
            }
            if ((localFiles == null) || (localFiles.Count <= 0))
            {
                return remoteFiles;
            }
            ArrayList newFiles = new ArrayList();
            for (int i = 0; i < remoteFiles.Count; i++) 
            {
                RemoteFile rm = (RemoteFile)remoteFiles[i];
                bool found = false;
                for (int j = 0; j < localFiles.Count; j++)
                {
                    RemoteFile lc = (RemoteFile)localFiles[j];
                    bool sameFile = false;
                    bool sameVersion = false;
                    bool sameName = lc.IsSamePathFile(rm, ref sameVersion);
                    if (checkOnlyPath)
                    {
                        sameFile = lc.name.Equals(rm.name);
                    }
                    else 
                    {
                        sameFile = lc.IsSameFile(rm);
                    }
                    if (sameFile)
                    {
                        if (checkOnlyPath)
                        {
                            found = true;
                        }
                        else
                        {
                            string file = rm.GetPath(isTemp);
                            bool exists = File.Exists(file);
                            if (exists)
                            {
                                if (rm.ValidateSize(file))
                                {
                                    if (rm.ShouldCheckCrc)
                                    {
                                        if (Crc.HashFile(file, false).ToLower().Equals(rm.crc))
                                        {
                                            found = true;
                                        }
                                    }
                                    else
                                    {
                                        found = true;
                                    }
                                }
                            }
                            else
                            {
                                // handle partial files
                                string tempFile = HttpGetter.GetTempFilePath(file);
                                bool tempExists = File.Exists(tempFile);
                                if (tempExists)
                                {
                                    if (rm.IsSizeValid())
                                    {
                                        if (rm.ValidateSize(tempFile))
                                        {
                                            if (rm.ShouldCheckCrc)
                                            {
                                                if (Crc.HashFile(tempFile, false).ToLower().Equals(rm.crc))
                                                {
                                                    File.Move(tempFile, file);
                                                    found = true;
                                                }
                                                else
                                                {
                                                    Utils.DeleteFile(tempFile);
                                                }
                                            }
                                            else
                                            {
                                                File.Move(tempFile, file);
                                                found = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    }
                    else
                    {
                        if (sameName && !sameVersion)
                        {
                            string file = rm.GetPath(isTemp);
                            Utils.DeleteFile(HttpGetter.GetTempFilePath(file));
                        }
                    }
                }
                if (!found)
                {
                    newFiles.Add(rm);
                }
            }
            return newFiles;
        }

        public static ArrayList GetRemovedFiles(ArrayList localFiles, ArrayList remoteFiles, bool isTemp)
        {
            return GetNewFiles(remoteFiles, localFiles, true, isTemp);
        }

        public static void DeleteFiles(ArrayList toDelete, bool isTemp) 
        {
            if (toDelete == null) return;
            for (int i = 0; i < toDelete.Count; i++)
            {
                RemoteFile rm = (RemoteFile)toDelete[i];
                if (!rm.Valid || rm.IsStarterReplacer) 
                {
                    continue;
                }
                string path = rm.GetPath(isTemp);
                try
                {
                    if (Utils.DeleteFile(path)) 
                    {
                        RawLog.Default.Log("Del " + path);
                    }
                    string dir = Path.GetDirectoryName(path);
                    Utils.DeleteEmptyDir(dir, true);
                }
                catch (Exception xx) { Utils.OnError(xx);  }
            }
        }

        public static long GetSize(ArrayList files)
        {
            if (files == null) return 0;
            long size = 0;
            foreach (RemoteFile rm in files)
            {
                if (rm.Valid && rm.IsSizeValid())
                {
                    size += rm.size;
                }
            }
            return size;
        }

        public static bool CheckDiskSpace(ArrayList files, ref long outSize) 
        {
            outSize = 0;
            long size = GetSize(files) * 2; //x2 for temp file transactions
            if (size <= 0) return true;
            outSize = size; 
            size += 150 * 1024; // buffer 150KB
            if (size <= 0) return true;
            string dir = Config.Default.AppFolder;
            if (!Directory.Exists(dir)) 
            {
                Directory.CreateDirectory(dir);
            }
            try
            {
                return DiskInfo.EnoughFreeDiskSpace(dir, size);
            }
            catch (Exception xx) { Utils.OnError(xx); }
            return true; // on error
        }

    }//EOC
}
