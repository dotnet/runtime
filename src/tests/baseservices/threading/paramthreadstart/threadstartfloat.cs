// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;

class ThreadStartFloat
{
    float fNum = 0F;

    public static int Main(string[] args)
    {
        // check args
        if(args.Length != 1)
        {
            Console.WriteLine("USAGE: ThreadStartFloat <float>|min|max\n");
            return -1;
        }

        float f = 0F;
        // check for max or min
        if(args[0].ToLower() == "max")
            f = Single.MaxValue;
        else if(args[0].ToLower() == "min")
            f = Single.MinValue;       
        else
            f = Convert.ToSingle(args[0]);
        ThreadStartFloat tsf = new ThreadStartFloat();
        return tsf.Run(f);
    }

    private int Run(float fPass)
    {
        Thread t = new Thread(new ParameterizedThreadStart(ThreadWorker));
        t.Start(fPass);
        t.Join();
        Console.WriteLine(fNum == fPass ? "Test Passed" : "Test Failed");
        return (fNum == fPass ? 100 : -1);
    }

    private void ThreadWorker(Object o)
    {
        fNum = (float)o;
        Console.WriteLine(fNum);
    }
}
