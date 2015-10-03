using System;

public class Guid
{
    public static int Main(string[] args)
    {
        try
        {
            System.Guid guid = System.Guid.NewGuid();
            Console.WriteLine(guid.ToString("B"));
            if((args != null)
                && (args.Length >= 1)
                && args[0].Equals("*"))
            {
                Console.WriteLine(guid.ToString());
                Console.WriteLine(guid.ToString("N"));
                Console.WriteLine(guid.ToString("D"));
                Console.WriteLine(guid.ToString("P"));
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("#Error: " + ex.Message);
            return 1;
        }
        return 0;
    }
}