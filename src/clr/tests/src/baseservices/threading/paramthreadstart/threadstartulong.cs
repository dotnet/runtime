using System;
using System.Threading;

class ThreadStartULong
{
    ulong lNum = 0;

    public static int Main(string[] args)
    {
        // check args
        if(args.Length != 1)
        {
            Console.WriteLine("USAGE: ThreadStartULong <ulong>|min|max\n");
            return -1;
        }

        ulong l = 0;
        // check for max or min
        if(args[0].ToLower() == "max")
            l = UInt64.MaxValue;
        else if(args[0].ToLower() == "min")
            l = UInt64.MinValue;       
        else
            l = Convert.ToUInt64(args[0]);

        ThreadStartULong tsl = new ThreadStartULong();
        return tsl.Run(l);
    }

    private int Run(ulong lPass)
    {
        Thread t = new Thread(new ParameterizedThreadStart(ThreadWorker));
        t.Start(lPass);
        t.Join();
        Console.WriteLine(lNum == lPass ? "Test Passed" : "Test Failed");
        return (lNum == lPass ? 100 : -1);
    }

    private void ThreadWorker(Object o)
    {
        lNum = (ulong)o;
        Console.WriteLine(lNum);
    }
}