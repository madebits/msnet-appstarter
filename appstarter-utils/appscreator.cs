using System;
using System.Collections;
using System.IO;
using System.Text;

namespace tools
{
    class AppSCreator
    {
        string[] files = new string[2];
        string result = null;

        public static int Main(string[] args)
        {
            try
            {
                AppSCreator sc = new AppSCreator();
                sc.Init(args);
                sc.Apply();
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: " + ex.Message);
                Console.Error.WriteLine("Usage: appscreator /c configFile [/o output.exe] [/s appstarter.exe]");
            }
            return 1;
        }

        public void Init(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if ((args[i].Length >= 2)
                    && ((args[i][0] == '/') || (args[i][0] == '-')))
                {
                    switch (args[i].Substring(1).ToLower())
                    {
                        case "c": files[1] = args[++i]; break;
                        case "o": result = args[++i]; break;
                        case "s": files[0] = args[++i]; break;
                    }
                }
            }
            if (string.IsNullOrEmpty(files[1])) throw new Exception("-c configFile required");
            if (string.IsNullOrEmpty(files[0]))
            {
                files[0] = Path.GetFullPath("appstarter.exe");
                if (!File.Exists(files[0]))
                {
                    throw new Exception("-s appstarter.exe required");
                }
            }
            if (string.IsNullOrEmpty(result))
            {
                result = Path.GetFileNameWithoutExtension(files[0]) + "-out.exe";
            }
        }

        public void Apply()
        {
            using (Stream sout = File.Open(result, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {
                for (int i = 0; i < files.Length; i++)
                {
                    CopyFile(sout, files[i]);
                    if (i == 0)
                    {
                        byte[] m = new byte[] { 35, 64, 64, 64, 64 };
                        sout.Write(m, 0, m.Length);
                    }
                }
            }
        }

        public static void CopyFile(Stream sout, string f)
        {
            using (Stream sin = File.Open(f, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                CopyStream(sin, sout);
            }
        }

        public static void CopyStream(Stream sin, Stream sout)
        {
            byte[] buffer = new byte[1024 * 1024];
            while (true)
            {
                int r = sin.Read(buffer, 0, buffer.Length);
                if (r <= 0) break;
                sout.Write(buffer, 0, r);
            }
        }
    }//EOC
}