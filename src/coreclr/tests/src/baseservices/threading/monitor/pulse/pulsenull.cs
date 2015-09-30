using System;
using System.Threading;

public class Test
{

    public static int Main()
    {
        try
        {
            Monitor.Pulse(null);
            Console.WriteLine("Failed to throw exception on Monitor.Pulse");
            return 1;
        }
        catch(ArgumentNullException)
        {
            //Expected            
        }
        return 100;
    }
}

