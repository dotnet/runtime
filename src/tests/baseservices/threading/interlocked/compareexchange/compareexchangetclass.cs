// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;

class Class1
{

    static int Main(string[] args)
    {
        int rValue = 0;
        Thread[] threads = new Thread[100];
        ThreadSafe tsi = new ThreadSafe();

        KrisClass kcIn = new KrisClass(args[0]);

        Console.WriteLine("Creating threads");
        for (int i = 0; i < threads.Length; i++)
        {
            threads[i] = new Thread(new ParameterizedThreadStart(tsi.ThreadWorker));
            threads[i].Start(kcIn);
        }

        tsi.Signal();

        Console.WriteLine("Joining threads");
        for (int i = 0; i < threads.Length; i++)
            threads[i].Join();

        // Build the expected string
        KrisClass kcExpected = new KrisClass("hello world! ");
        for (int i = 0; i < threads.Length * 100; i++)
            kcExpected = kcExpected + kcIn;

        if (kcExpected == tsi.GetValue)
            rValue = 100;
        Console.WriteLine("Test Expected {0}, but found {1}", kcExpected, tsi.GetValue);
        Console.WriteLine("Test {0}", rValue == 100 ? "Passed" : "Failed");
        return rValue;
    }
}

public class ThreadSafe
{
    ManualResetEvent signal;
    public KrisClass Val = new KrisClass("hello world! ");
    private int numberOfIterations;
    public ThreadSafe() : this(100) { }
    public ThreadSafe(int loops)
    {
        signal = new ManualResetEvent(false);
        numberOfIterations = loops;
    }

    public void Signal()
    {
        signal.Set();
    }

    public void ThreadWorker(Object objIn)
    {
        KrisClass kcIn = (KrisClass)objIn;
        signal.WaitOne();
        for (int i = 0; i < numberOfIterations; i++)
            AddToTotal(kcIn);
    }

    private KrisClass AddToTotal(KrisClass addend)
    {
        KrisClass initialValue;
        KrisClass newValue;

        do
        {
            initialValue = Val;
            newValue = initialValue + addend;
        }
        while ((object)initialValue != Interlocked.CompareExchange<KrisClass>(
            ref Val, newValue, initialValue));

        return newValue;
    }

    public KrisClass GetValue
    {
        get
        {
            return Val;
        }
    }
}

public class KrisClass
{
    string retVal = string.Empty;
    public KrisClass(string setVal)
    {
        retVal = setVal;
    }

    public string ClassVal
    {
        get
        {
            return retVal;
        }
    }

    public static KrisClass operator +(KrisClass kc1, KrisClass kc2)
    {
        return new KrisClass(kc1.ClassVal + kc2.ClassVal);
    }

    public static bool operator ==(KrisClass kc1, KrisClass kc2)
    {
        if (kc1.ClassVal == kc2.ClassVal)
            return true;
        else
            return false;
    }

    public static bool operator !=(KrisClass kc1, KrisClass kc2)
    {
        if (kc1.ClassVal != kc2.ClassVal)
            return true;
        else
            return false;
    }

    public override bool Equals(object o)
    {
        try
        {
            return (bool)(this == (KrisClass)o);
        }
        catch
        {
            return false;
        }
    }

    public override int GetHashCode()
    {
        return 0;
    }
}
