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
        if(tsi.RunAdd(i) && tsi.RunSub(i) && tsi.RunMult(i) && tsi.RunDiv(i))
            return 100;
        else
            return -1;
    }

    private bool RunAdd(int iPass)
    {
        Thread t = new Thread(new ParameterizedThreadStart(
            ThreadWorker));
        t.Start(iPass + iPass);
        t.Join();
        Console.WriteLine(iNum == iPass*2 ? "Test Passed" : "Test Failed");
        return (iNum == iPass*2);
    }

    private bool RunSub(int iPass)
    {
        Thread t = new Thread(new ParameterizedThreadStart(
            ThreadWorker));
        t.Start(iPass - iPass);
        t.Join();
        Console.WriteLine(0 == iNum ? "Test Passed" : "Test Failed");
        return (0 == iNum);
    }

    private bool RunMult(int iPass)
    {
        Thread t = new Thread(new ParameterizedThreadStart(
            ThreadWorker));
        t.Start(iPass * 10);
        t.Join();
        Console.WriteLine(iPass*10 == iNum ? "Test Passed" : "Test Failed");
        return (iPass*10 == iNum);
    }

    private bool RunDiv(int iPass)
    {
        Thread t = new Thread(new ParameterizedThreadStart(
            ThreadWorker));
        t.Start(iPass/10);
        t.Join();
        Console.WriteLine(iPass/10 == iNum ? "Test Passed" : "Test Failed");
        return (iPass/10 == iNum);
    }

    private void ThreadWorker(Object o)
    {
        iNum = (int)o;
        Console.WriteLine(iNum);
    }
}