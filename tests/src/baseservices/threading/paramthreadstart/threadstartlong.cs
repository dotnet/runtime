// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;

class ThreadStartLong
{
    long lNum = 0;

    public static int Main(string[] args)
    {
        // check args
        if(args.Length != 1)
        {
            Console.WriteLine("USAGE: ThreadStartLong <long>|min|max\n");
            return -1;
        }

        long l = 0;
        // check for max or min
        if(args[0].ToLower() == "max")
            l = Int64.MaxValue;
        else if(args[0].ToLower() == "min")
            l = Int64.MinValue;       
        else
            l = Convert.ToInt64(args[0]);

        ThreadStartLong tsl = new ThreadStartLong();
        return tsl.Run(l);
    }

    private int Run(long lPass)
    {
        Thread t = new Thread(new ParameterizedThreadStart(ThreadWorker));
        t.Start(lPass);
        t.Join();
        Console.WriteLine(lNum == lPass ? "Test Passed" : "Test Failed");
        return (lNum == lPass ? 100 : -1);
    }

    private void ThreadWorker(Object o)
    {
        lNum = (long)o;
        Console.WriteLine(lNum);
    }
}