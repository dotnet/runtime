// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;

class ThreadStartSByte
{
    sbyte iByte = 0x0;

    public static int Main(string[] args)
    {
        // check args
        if(args.Length != 1)
        {
            Console.WriteLine("USAGE: ThreadStartSByte <int>|min|max\n");
            return -1;
        }

        sbyte b = 0x0;
        // check for max or min
        if(args[0].ToLower() == "max")
            b = SByte.MaxValue;
        else if(args[0].ToLower() == "min")
            b = SByte.MinValue;       
        else
            b = Convert.ToSByte(args[0]);

        ThreadStartSByte tsb = new ThreadStartSByte();
        return tsb.Run(b);
    }

    private int Run(sbyte bPass)
    {
        Thread t = new Thread(new ParameterizedThreadStart(ThreadWorker));
        t.Start(bPass);
        t.Join();
        Console.WriteLine(iByte == bPass ? "Test Passed" : "Test Failed");
        return (iByte == bPass ? 100 : -1);
    }

    private void ThreadWorker(Object o)
    {
        iByte = (sbyte)o;
        Console.WriteLine(iByte);
    }
}