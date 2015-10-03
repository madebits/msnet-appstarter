using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;

public class MD5
{
    private static HashAlgorithm hasher =
                new System.Security.Cryptography.MD5CryptoServiceProvider();

	public static int Main(string[] args)
	{
		try
		{
            if((args == null) || (args.Length <= 0))
            {
                throw new Exception("file argument missing");
            }
            for (int i = 0; i < args.Length; i++) 
            {
                HashFile(args[i]);
            }
		}
		catch(Exception ex)
		{
			Console.Error.WriteLine("#Error: " + ex.Message);
			return 1;
		}
		return 0;
	}

    private static void HashFile(string file)
    {
        using (Stream s = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024))
        {
            byte[] h = hasher.ComputeHash(s);
            StringBuilder sb = new StringBuilder(h.Length * 2);
            for (int i = 0; i < h.Length; i++)
            {
                sb.AppendFormat("{0:x2}", h[i]);
            }
            Console.WriteLine(sb.ToString());
        }
    }

}//EOC