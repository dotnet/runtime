using System;
using System.Threading;

public class Test
{

    public static int Main()
    {
        try
        {
            Monitor.PulseAll(null);
            Console.WriteLine("Failed to throw exception on Monitor.PulseAll");
            return 1;
        }
        catch(ArgumentNullException)
        {
            //Expected
        }
        return 100;
    }

}

