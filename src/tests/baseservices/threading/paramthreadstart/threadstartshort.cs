// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;

class ThreadStartShort
{
    short sNum = 0;

    public static int Main(string[] args)
    {
        // check args
        if(args.Length != 1)
        {
            Console.WriteLine("USAGE: ThreadStartShort <short>|min|max\n");
            return -1;
        }

        short s = 0;
        // check for max or min
        if(args[0].ToLower() == "max")
            s = Int16.MaxValue;
        else if(args[0].ToLower() == "min")
            s = Int16.MinValue;       
        else
            s = Convert.ToInt16(args[0]);

        ThreadStartShort tss = new ThreadStartShort();
        return tss.Run(s);
    }

    private int Run(short sPass)
    {
        Thread t = new Thread(new ParameterizedThreadStart(ThreadWorker));
        t.Start(sPass);
        t.Join();
        Console.WriteLine(sNum == sPass ? "Test Passed" : "Test Failed");
        return (sNum == sPass ? 100 : -1);
    }

    private void ThreadWorker(Object o)
    {
        sNum = (short)o;
        Console.WriteLine(sNum);
    }
}
