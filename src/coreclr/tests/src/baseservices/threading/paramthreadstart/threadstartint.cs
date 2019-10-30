// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;

class ThreadStartInt
{
    int iNum = 0;

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
        t.Start(iPass);
        t.Join();
        Console.WriteLine(iNum == iPass ? "Test Passed" : "Test Failed");
        return (iNum == iPass ? 100 : -1);
    }

    private void ThreadWorker(Object o)
    {
        iNum = (int)o;
        Console.WriteLine(iNum);
    }
}