using System;
using System.Collections;
using System.Windows.Forms;

namespace ws
{
    class FileAssoc
    {
        public string extension = string.Empty;
        public string progId = string.Empty;
        public string desc = string.Empty;
        public string exePath = string.Empty;
        public string iconPath = string.Empty;
        public int iconIdx = 0;

        public string ExePath 
        {
            get 
            {
                return GetPath(this.exePath);
            }
        }

        public string IconPath
        {
            get
            {
                return GetPath(this.iconPath);
            }
        }

        private string GetPath(string s)
        {
            if (string.IsNullOrEmpty(s)) 
            {
                return Application.ExecutablePath;           
            }
            return Config.Default.GetPath(s, false, false);
        }

        public bool Valid 
        {
            get
            {
                return !string.IsNullOrEmpty(extension)
                    && !string.IsNullOrEmpty(progId);
            }
        }

        public const string Prefix = "##*";

        public void FromString(string s) 
        {
            //progId|ext|desc|ipath|iidx|epath
            if (string.IsNullOrEmpty(s)) return;
            if (s.StartsWith(Prefix))
            {
                s = RemoteFile.CleanString(s.Substring(Prefix.Length));
            }
            if (string.IsNullOrEmpty(s)) return;
            string[] p = s.Split('|');
            if((p == null) || (p.Length < 2)) return;
            this.progId = RemoteFile.CleanString(p[0]);
            this.extension = RemoteFile.CleanString(p[1]);
            if (!string.IsNullOrEmpty(extension) && !this.extension.StartsWith(".")) 
            {
                this.extension = "." + this.extension;
            }
            if (p.Length > 2)
            {
                this.desc = RemoteFile.CleanString(p[2]);
            }
            if (p.Length > 3)
            {
                this.iconPath = RemoteFile.CleanString(p[3]);
            }
            if (p.Length > 4)
            {
                this.iconIdx = Convert.ToInt32(RemoteFile.CleanString(p[4]), System.Globalization.CultureInfo.InvariantCulture);
            }
            if (p.Length > 5)
            {
                this.exePath = RemoteFile.CleanString(p[5]);
            }
        }

        public override string ToString()
        {
            return Prefix + this.progId
                + "|" + this.extension
                + "|" + this.desc
                + "|" + this.iconPath
                + "|" + this.iconIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + "|" + this.exePath
                ;
        }

        public void Register(bool on) 
        {
            if (!Valid) return;
            if (on)
            {
                FileAssocWin.Register(this.extension, this.progId, this.desc, this.ExePath, this.IconPath, this.iconIdx);
            }
            else 
            {
                FileAssocWin.UnRegister(this.extension, this.progId);
            }
        }


        public bool IsEqual(FileAssoc other)
        {
            if (this.extension != other.extension) return false;
            if (this.progId != other.progId) return false;
            if (this.desc != other.desc) return false;
            if (this.exePath != other.exePath) return false;
            if (this.iconPath != other.iconPath) return false;
            if (this.iconIdx != other.iconIdx) return false;
            return true;
        }
        
        public static bool AreEqual(ArrayList f1, ArrayList f2)
        {
            if ((f1 == null) && (f2 == null)) return true;
            if ((f1 == null) && (f2.Count == 0)) return true;
            if ((f2 == null) && (f1.Count == 0)) return true;
            if (f1 == null) return false;
            if (f2 == null) return false;
            if ((f1.Count == 0) && (f2.Count == 0)) return true;
            if (f1.Count != f2.Count) return false;
            for (int i = 0; i < f1.Count; i++)
            {
                FileAssoc fa = (FileAssoc)f1[i];
                bool found = false;
                for (int j = 0; j < f2.Count; j++)
                {
                    if (((FileAssoc)f2[j]).IsEqual(fa))
                    {
                        found = true;
                    }
                }
                if (!found)
                {
                    return false;
                }
            }
            return true;
        }

        public static void Register(ArrayList f, bool on) 
        {
            if (Config.Default.IsRemovableDiskType)
            {
                return;
            }
            if ((f == null) || (f.Count <= 0)) return;
            for (int i = 0; i < f.Count; i++) 
            {
                FileAssoc fa = (FileAssoc)f[i];
                if (fa == null) continue;
                try
                {
                    fa.Register(on);
                }
                catch (Exception xx){ Utils.OnError(xx); }
            }
            try
            {
                FileAssocWin.NotifyShell();
            }
            catch (Exception xx) { Utils.OnError(xx); }
        }

    }//EOC
}
