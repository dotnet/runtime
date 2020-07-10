// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;

class ThreadStartUShort
{
    ushort sNum = 0;

    public static int Main(string[] args)
    {
        // check args
        if(args.Length != 1)
        {
            Console.WriteLine("USAGE: ThreadStartUShort <ushort>|min|max\n");
            return -1;
        }

        ushort s = 0;
        // check for max or min
        if(args[0].ToLower() == "max")
            s = UInt16.MaxValue;
        else if(args[0].ToLower() == "min")
            s = UInt16.MinValue;       
        else
            s = Convert.ToUInt16(args[0]);

        ThreadStartUShort tss = new ThreadStartUShort();
        return tss.Run(s);
    }

    private int Run(ushort sPass)
    {
        Thread t = new Thread(new ParameterizedThreadStart(ThreadWorker));
        t.Start(sPass);
        t.Join();
        Console.WriteLine(sNum == sPass ? "Test Passed" : "Test Failed");
        return (sNum == sPass ? 100 : -1);
    }

    private void ThreadWorker(Object o)
    {
        sNum = (ushort)o;
        Console.WriteLine(sNum);
    }
}
