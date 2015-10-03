using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Security.Cryptography;
using System.Diagnostics;

public class AppSList
{
    HashAlgorithm hash = new MD5CryptoServiceProvider();
    ArrayList suffixes = new ArrayList();
    string starterExe = string.Empty;
    string baseUrl = "http://127.0.0.1/app";
    string startDir = ".";
    StreamWriter sw = null;
    bool useFileVersion = false;
    bool recursive = false;
    bool useText = false;
    string appendFile = null;
    bool minimal = false;
    bool addComment = false;

    [STAThread]
    public static int Main(string[] args)
    {
        System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

        AppSList lgen = new AppSList();
        try
        {
            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if ((args[i].Length >= 2)
                        && ((args[i][0] == '/') || (args[i][0] == '-')))
                    {
                        switch (args[i].Substring(1).ToLower())
                        {
                            case "i": lgen.startDir = args[++i]; break;
                            case "r": lgen.recursive = true; break;
                            case "u": lgen.baseUrl = args[++i]; break;
                            case "e":
                                string[] ext = args[++i].Split(';');
                                for (int j = 0; j < ext.Length; j++)
                                {
                                    string fext = ext[j].Trim(' ', ';', '\t').ToLower();
                                    if (string.IsNullOrEmpty(fext)) continue;
                                    lgen.suffixes.Add(fext);
                                }
                                break;
                            case "s": lgen.starterExe = Path.GetFileName(args[++i]).ToLower(); break;
                            case "o": lgen.sw = new StreamWriter(args[++i], false, Encoding.UTF8, 1024); break;
                            case "f": lgen.useFileVersion = true; break;
                            case "t": lgen.useText = true; break;
                            case "a": lgen.appendFile = args[++i]; break;
                            case "m": lgen.minimal = true; break;
                            case "c": lgen.addComment = true; break;
                            default:
                                throw new Exception("unknown argument " + args[i]);
                        }
                    }
                }
            }
            lgen.Generate();
            return 0;
        }
        catch(Exception ex)
        {
            Console.Error.WriteLine("Error: " + ex.Message);
            //    + " " + ex.StackTrace);
            Console.Error.WriteLine("Usage: appslist [/i directory] [/r] [/u baseUrl] [/o outputFile] [/e suffix;suffix;...] [/s starterExe] [/t] [/f] [/m] [/c] [/a appendTextFile]");
        }
        finally
        {
            if (lgen.sw != null) lgen.sw.Close();
            lgen.sw = null;
        }
        return 1;
    }
    
    public void Generate()
    {
        if (!startDir.EndsWith("\\") || !startDir.EndsWith("/"))
        {
            startDir += Path.DirectorySeparatorChar.ToString();
        }
        if (!baseUrl.EndsWith("/"))
        {
            baseUrl += "/";
        }
        if (addComment)
        {
            string comment = "#url|path|version|exe|args";
            if (!minimal) comment += "|text|size|md5";
            LogLine(comment);
        }
        LogLine("$$u=" + baseUrl);
        ProcessDir(startDir, baseUrl, startDir.Length);
        if(sw != null){ sw.Flush(); }
        if(!string.IsNullOrEmpty(appendFile))
        {
            using (StreamReader sr = new StreamReader(appendFile, true)) 
            {
                string text = sr.ReadToEnd();
                text = text.Trim('\r', '\n', '\t', ' ');
                LogLine(text);
            }
        }
    }

    private void ProcessDir(string dir, string baseUrl, int prefixLength)
    {
        string[] files = Directory.GetFiles(dir);
        if (files != null)
        {
            for (int i = 0; i < files.Length; i++)
            {
                if (!CanProcess(files[i])) continue;
                string name = files[i].Substring(prefixLength).Replace('\\', '/');
                LogFile(files[i], baseUrl, name);
            }
        }
        if (!recursive)
        {
            return;
        }
        files = Directory.GetDirectories(dir);
        if (files != null)
        {
            for (int i = 0; i < files.Length; i++)
            {
                ProcessDir(files[i], baseUrl, prefixLength);
            }
        }
    }

    private bool CanProcess(string file)
    {
        if (string.IsNullOrEmpty(file)) return false;
        if (Path.GetFullPath(file).ToLower().Equals(Path.GetFullPath(Environment.GetCommandLineArgs()[0]).ToLower())) return false;
        string f = Path.GetFileName(file).ToLower();
        if (suffixes.Count > 0)
        {
            for(int i = 0; i < suffixes.Count; i++)
            {
                if(f.EndsWith((string)suffixes[i]))
                {
                    return true;
                }
            }
            return false;
        }
        return true;
    }

    private void LogFile(string file, string baseUrl, string name)
    {
        FileInfo fi = new FileInfo(file);
        string version = fi.LastWriteTime.Ticks.ToString();
        if(useFileVersion)
        {
            try
            {
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(file);
                if(fvi != null)
                {
                    string t = fvi.FileVersion;
                    fvi = null;
                    if (!string.IsNullOrEmpty(t)) version = t;
                }
            }
            catch{}
        }

        string outName = name;
        string fname = Path.GetFileName(name).ToLower();
        if (fname.Equals(this.starterExe)) outName = "*starter*";
        
        StringBuilder sb = new StringBuilder();
        sb.Append("$u").Append(System.Web.HttpUtility.UrlEncode(name)).Append('|').Append(outName);
        sb.Append('|').Append(version);
        if (name.EndsWith(".exe"))
        {
            sb.Append("|*|*");
        }
        else
        {
            if (!minimal)
            {
                sb.Append("||");
            }
        }
        if (!minimal)
        {
            string md5 = string.Empty;
            try
            {
                using (Stream s = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024))
                {
                    byte[] h = hash.ComputeHash(s);
                    StringBuilder msb = new StringBuilder(h.Length * 2);
                    for (int i = 0; i < h.Length; i++)
                    {
                        msb.AppendFormat("{0:x2}", h[i]);
                    }
                    md5 = msb.ToString();
                }
            }
            catch
            {
                // file in use in write mode
                return;
            }

            sb.Append('|').Append(GetDesc(name)).Append('|');
            sb.Append(fi.Length).Append('|').Append(md5);
        }
        fi = null;
        LogLine(sb.ToString());
    }

    private string GetDesc(string name)
    {
        if (!useText) return string.Empty;
        if (name.EndsWith(".exe")) return "Downloading application ...";
        if (name.EndsWith(".dll")) return "Downloading application library ...";
        if (name.EndsWith(".chm")) return "Downloading help ...";
        if (name.EndsWith(".pdf")) return "Downloading manual ...";
        return string.Empty;
    }

    private void LogLine(string s)
    {
        if (sw != null)
        {
            sw.WriteLine(s);
        }
        else
        {
            Console.WriteLine(s);
        }
    }

}//EOC