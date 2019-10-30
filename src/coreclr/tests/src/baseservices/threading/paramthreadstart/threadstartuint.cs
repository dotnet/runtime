// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;

class ThreadStartUInt
{
    uint iNum = 0;

    public static int Main(string[] args)
    {
        // check args
        if(args.Length != 1)
        {
            Console.WriteLine("USAGE: ThreadStartUInt <uint>|min|max\n");
            return -1;
        }

        uint i = 0;
        // check for max or min
        if(args[0].ToLower() == "max")
            i = UInt32.MaxValue;
        else if(args[0].ToLower() == "min")
            i = UInt32.MinValue;       
        else
            i = Convert.ToUInt32(args[0]);

        ThreadStartUInt tsu = new ThreadStartUInt();
        return tsu.Run(i);
    }

    private int Run(uint iPass)
    {
        Thread t = new Thread(new ParameterizedThreadStart(ThreadWorker));
        t.Start(iPass);
        t.Join();
        Console.WriteLine(iNum == iPass ? "Test Passed" : "Test Failed");
        return (iNum == iPass ? 100 : -1);
    }

    private void ThreadWorker(Object o)
    {
        iNum = (uint)o;
        Console.WriteLine(iNum);
    }
}