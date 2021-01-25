// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;

class ThreadStartDecimal
{
    decimal dNum = 0M;

    public static int Main(string[] args)
    {
        // check args
        if(args.Length != 1)
        {
            Console.WriteLine("USAGE: ThreadStartDouble <decimal>|min|max\n");
            return -1;
        }

        decimal d = 0M;
        // check for max or min
        if(args[0].ToLower() == "max")
            d = Decimal.MaxValue;
        else if(args[0].ToLower() == "min")
            d = Decimal.MinValue;       
        else
            d = Convert.ToDecimal(args[0]);
        ThreadStartDecimal tsd = new ThreadStartDecimal();
        return tsd.Run(d);
    }

    private int Run(decimal dPass)
    {
        Thread t = new Thread(new ParameterizedThreadStart(ThreadWorker));
        t.Start(dPass);
        t.Join();
        Console.WriteLine(dNum == dPass ? "Test Passed" : "Test Failed");
        return (dNum == dPass ? 100 : -1);
    }

    private void ThreadWorker(Object o)
    {
        dNum = (decimal)o;
        Console.WriteLine(dNum);
    }
}
