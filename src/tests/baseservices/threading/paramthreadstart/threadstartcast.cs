// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;

class ThreadStartInt
{
    object num;

    public static int Main(string[] args)
    {
        // check args
        if(args.Length != 1)
        {
            Console.WriteLine("USAGE: ThreadStartInt <int>|min|max\n");
            return -1;
        }

        int i = 0;
        // check for max or min
        if(args[0].ToLower() == "max")
            i = Int32.MaxValue;
        else if(args[0].ToLower() == "min")
            i = Int32.MinValue;       
        else
            i = Convert.ToInt32(args[0]);

        ThreadStartInt tsi = new ThreadStartInt();
        return tsi.Run(i);
    }

    private int Run(int iPass)
    {
        Thread t = new Thread(new ParameterizedThreadStart(ThreadWorker));
        t.Start(Convert.ToDouble(iPass));
        t.Join();
        bool bRet = (double)num == (double)iPass;
        Console.WriteLine(bRet ? "Test Passed" : "Test Failed");
        return (bRet ? 100 : -1);
    }

    private void ThreadWorker(Object o)
    {
        num = o;
        Console.WriteLine(num);
    }
}
