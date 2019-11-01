// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;
using System.Collections;

class Class1
{
    static int Main(string[] args)
    {
        int rValue = 0;
        Thread[] threads = new Thread[100];
        ThreadSafe tsi = new ThreadSafe();

        Console.WriteLine("Creating threads");
        for (int i = 0; i < threads.Length - 1; i++)
        {
            if (i % 2 == 0)
                threads[i] = new Thread(new
                    ParameterizedThreadStart(tsi.ThreadWorkerA));
            else
                threads[i] = new Thread(new
                    ParameterizedThreadStart(tsi.ThreadWorkerB));

            threads[i].Start(args);
        }

        Console.WriteLine("Starting checker");
        threads[threads.Length - 1] = new Thread(new ThreadStart(tsi.ThreadChecker));
        threads[threads.Length - 1].Start();

        tsi.Signal();

        Console.WriteLine("Joining threads");
        for (int i = 0; i < threads.Length; i++)
            threads[i].Join();

        if (tsi.Pass)
            rValue = 100;
        Console.WriteLine("Test {0}", rValue == 100 ? "Passed" : "Failed");
        return rValue;
    }
}

public class ThreadSafe
{
    ManualResetEvent signal;
    private string curVal = "start string";
    private int numberOfIterations;
    private string newValueA = "hello";
    private string newValueB = "world";
    private bool success;

    public ThreadSafe() : this(10000) { }

    public ThreadSafe(int loops)
    {
        success = true;
        signal = new ManualResetEvent(false);
        numberOfIterations = loops;
    }

    public void Signal()
    {
        signal.Set();
    }

    public void ThreadWorkerA(object obj)
    {
        string[] str = (string[])obj;
        string ret = null;

        // get the value
        if (0 < str.Length)
        {
            if ("null" == str[0])
                newValueA = null;
            else if ("empty" == str[0])
                newValueA = string.Empty;
            else
                newValueA = str[0];
        }

        signal.WaitOne();
        for (int i = 0; i < numberOfIterations; i++)
        {
            ret = Interlocked.Exchange<string>(ref curVal, newValueA);

            // Check return value
            if (ret != newValueB && ret != newValueA && ret != "start string")
            {
                Console.WriteLine(ret + "," + newValueB + "," + newValueA);
                success = false;
            }
        }

    }

    public void ThreadWorkerB(object obj)
    {
        string[] str = (string[])obj;
        string ret = null;

        // get the value
        if (2 == str.Length)
        {
            if ("null" == str[1])
                newValueB = null;
            else if ("empty" == str[1])
                newValueB = string.Empty;
            else
                newValueB = str[1];
        }

        signal.WaitOne();

        for (int i = 0; i < numberOfIterations; i++)
        {
            ret = Interlocked.Exchange<string>(ref curVal, newValueB);

            // Check return value
            if (ret != newValueB && ret != newValueA && ret != "start string")
            {
                Console.WriteLine(ret + "," + newValueB + "," + newValueA);
                success = false;
            }
        }
    }

    public void ThreadChecker()
    {
        signal.WaitOne();

        while(curVal == "start string")
        {
            Thread.Sleep(0);
        }

        string tmpVal;
        for (int i = 0; i < numberOfIterations; i++)
        {
            tmpVal = curVal;
            if (tmpVal != newValueB && tmpVal != newValueA)
            {
                Console.WriteLine(tmpVal + "," + newValueB + "," + newValueA);
                success = false;
            }
            Thread.Sleep(0);
        }
    }

    public bool Pass
    {
        get
        {
            return (success);
        }
    }
}
