// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;

class ThreadStartDouble
{
    double dNum = 0D;

    public static int Main(string[] args)
    {
        // check args
        if(args.Length != 1)
        {
            Console.WriteLine("USAGE: ThreadStartDouble <double>" +
                "|min|max|pi|nan|negi|posi\n");
            return -1;
        }

        double d = 0D;
        // check for special cases
        switch(args[0].ToLower())
        {
            case "max":
                d = Double.MaxValue;
                break;
            case "min":
                d = Double.MinValue;
                break;
            case "pi":
                d = Math.PI;
                break;
            case "nan":
                d = Double.NaN;
                break;
            case "negi":
                d = Double.NegativeInfinity;
                break;
            case "posi":
                d = Double.PositiveInfinity;
                break;
            default:
                d = Convert.ToDouble(args[0]);
                break;
        }
        ThreadStartDouble tsd = new ThreadStartDouble();
        return tsd.Run(d);
    }

    private int Run(double dPass)
    {
        Thread t = new Thread(new ParameterizedThreadStart(ThreadWorker));
        t.Start(dPass);
        t.Join();

        bool bRet = false;
        if(Double.IsNaN(dPass))
            bRet = Double.IsNaN(dNum);
        else
            bRet = dNum == dPass;
        Console.WriteLine(bRet ? "Test Passed" : "Test Failed");
        return (bRet ? 100 : -1);
    }

    private void ThreadWorker(Object o)
    {
        dNum = (double)o;
        Console.WriteLine(dNum);
    }
}