// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;

class ExchangeClass
{   
    static int Main(string[] args)
    {
        int rValue = 0;
        Thread[] threads = new Thread[100];
        ThreadSafe tsi = new ThreadSafe();
        Console.WriteLine("Creating Threads");
        for (int i = 0; i < threads.Length - 1; i++)
        {
            if (i % 2 == 0)
                threads[i] = new Thread(new ThreadStart(tsi.ThreadWorkerA));
            else
                threads[i] = new Thread(new ThreadStart(tsi.ThreadWorkerB));
            threads[i].Start();
        }

        Console.WriteLine("Starting checker");
        threads[threads.Length - 1] = new Thread(new ThreadStart(tsi.ThreadChecker));
        threads[threads.Length - 1].Start();
        tsi.Signal();

        Console.WriteLine("Joining Threads");
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
    private KrisClass totalValue = new KrisClass(1);
    private int numberOfIterations;
    private KrisClass newValueA = new KrisClass(12345);
    private KrisClass newValueB = new KrisClass(67890);
    private bool success;
    public ThreadSafe(): this(10000) { }
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

    public void ThreadWorkerA()
    {
        KrisClass ret = null;
        signal.WaitOne();
        for (int i = 0; i < numberOfIterations; i++)
        {
            ret = Interlocked.Exchange<KrisClass>(ref totalValue, newValueA);

            // Check return value
            if (ret.ClassVal != newValueA.ClassVal && 
                ret.ClassVal != newValueB.ClassVal &&
                ret.ClassVal != 1)
            {
                Console.WriteLine(ret.ClassVal + "," + 
                    newValueB.ClassVal + "," + newValueA.ClassVal);
                success = false;
            }
        }

    }

    public void ThreadWorkerB()
    {
        KrisClass ret = null;
        signal.WaitOne();
        for (int i = 0; i < numberOfIterations; i++)
        {
            ret = Interlocked.Exchange<KrisClass>(ref totalValue, newValueB);

            // Check return value
            if (ret.ClassVal != newValueA.ClassVal && 
                ret.ClassVal != newValueB.ClassVal &&
                ret.ClassVal != 1)
            {
                Console.WriteLine(ret.ClassVal + "," + 
                    newValueB.ClassVal + "," + newValueA.ClassVal);
                success = false;
            }
        }
    }

    public void ThreadChecker()
    {
        signal.WaitOne();
        KrisClass tmpVal;
        for (int i = 0; i < numberOfIterations; i++)
        {
            tmpVal = totalValue;
            if (tmpVal.ClassVal != newValueB.ClassVal && 
                tmpVal.ClassVal != newValueA.ClassVal &&
                tmpVal.ClassVal != 1)
            {
                Console.WriteLine(tmpVal.ClassVal + "," + newValueB.ClassVal + "," + 
                    newValueA.ClassVal);
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

public class KrisClass
{
    int retVal = 0;
    public KrisClass(int setVal)
    {
        retVal = setVal;
    }

    public int ClassVal
    {
        get
        {
            return retVal;
        }
    }
}
