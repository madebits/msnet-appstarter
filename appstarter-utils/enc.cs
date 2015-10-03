using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class Enc
{
    static string inputDir = null;
    static string pass = null;
    static bool encode = true;

    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            if ((args == null) || args.Length <= 0) throw new ApplicationException("arguments missing");
            for (int i = 0; i < args.Length; i++)
            {
                if ((args[i].Length >= 2)
                    && ((args[i][0] == '/') || (args[i][0] == '-')))
                {
                    switch (args[i].Substring(1).ToLower())
                    {
                        case "i": inputDir = args[++i]; break;
                        case "p": pass = args[++i]; break;
                        case "d": encode = false; break;
                        default:
                            throw new Exception("unknown argument " + args[i]);
                    }
                }
            }
            if (string.IsNullOrEmpty(inputDir)) throw new ApplicationException("inputDir");
            if (string.IsNullOrEmpty(pass)) throw new ApplicationException("password");
            ProcessDir(inputDir);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Error: " + ex.Message);
            //    + " " + ex.StackTrace);
            Console.Error.WriteLine("Usage: enc /i inputDirectory /p password [/d]");
        }
        return 1;
    }

    private static void ProcessDir(string inputDir)
    {
        string[] files = Directory.GetFiles(inputDir);
        if (files != null)
        {
            for (int i = 0; i < files.Length; i++)
            {
                ProcessFile(files[i]);
            }
        }
        files = Directory.GetDirectories(inputDir);
        if (files != null)
        {
            for (int i = 0; i < files.Length; i++)
            {
                ProcessDir(files[i]);
            }
        }
    }

    private static void ProcessFile(string file)
    {
        ws.Encoder.Encode(file, pass, encode);
    }
}

