// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;

class ThreadStartByte
{
    byte iByte = 0x0;

    public static int Main(string[] args)
    {
        // check args
        if(args.Length != 1)
        {
            Console.WriteLine("USAGE: ThreadStartByte <int>|min|max\n");
            return -1;
        }

        byte b = 0x0;
        // check for max or min
        if(args[0].ToLower() == "max")
            b = Byte.MaxValue;
        else if(args[0].ToLower() == "min")
            b = Byte.MinValue;       
        else
            b = Convert.ToByte(args[0]);

        ThreadStartByte tsb = new ThreadStartByte();
        return tsb.Run(b);
    }

    private int Run(byte bPass)
    {
        Thread t = new Thread(new ParameterizedThreadStart(ThreadWorker));
        t.Start(bPass);
        t.Join();
        Console.WriteLine(iByte == bPass ? "Test Passed" : "Test Failed");
        return (iByte == bPass ? 100 : -1);
    }

    private void ThreadWorker(Object o)
    {
        iByte = (byte)o;
        Console.WriteLine(iByte);
    }
}