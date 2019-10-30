// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;

class Class1
{
    
    static int Main(string[] args)
    {
        int rValue = 0;
        Thread[] threads = new Thread[100];
        string strIn = args[0];
        ThreadSafe tsi = new ThreadSafe(100, strIn);


        Console.WriteLine("Creating threads");
        for (int i = 0; i < threads.Length; i++)
        {
            threads[i] = new Thread(new ParameterizedThreadStart(tsi.ThreadWorker));
            threads[i].Start();
        }
			
        tsi.Signal();

        Console.WriteLine("Joining threads");
        for(int i=0;i<threads.Length;i++)
            threads[i].Join();

        // Build the expected string
        string strExpected = string.Empty;        
        for(int i=0;i<threads.Length;i++)
            strExpected += tsi.Expected;
        
        if(tsi.Val == strExpected)
            rValue = 100;

	Console.WriteLine("Test Expected {0}, but found {1}", strExpected, tsi.Val);
        Console.WriteLine("Test {0}", rValue == 100 ? "Passed" : "Failed");
        return rValue;
    }
}

public class ThreadSafe
{
    ManualResetEvent signal;
    public string Val = string.Empty;		
    private int numberOfIterations;
    private string strIn = string.Empty;
    public ThreadSafe(int loops, object obj)
    {
        signal = new ManualResetEvent(false);
        numberOfIterations = loops;
        strIn = obj.ToString();

        if(0 < strIn.Length)
        {
            if("null" == strIn)
                strIn = null;
            else if("empty" == strIn)
                strIn = string.Empty;
        }
    }

    public void Signal()
    {
        signal.Set();
    }

    public void ThreadWorker(object obj)
    {
        signal.WaitOne();
        for(int i=0;i<numberOfIterations;i++)
            AddToTotal(strIn);
    }

    public string Expected
    {
        get
        {
            string strTemp = string.Empty;
            for(int i=0;i<numberOfIterations;i++)
                strTemp += strIn;

            return strTemp;
        }
    }

    private string AddToTotal(string addend)
    {
        string initialValue, newValue;
        do
        {
            initialValue = Val;
            newValue = initialValue + addend;
        } 
        while ((object)initialValue != Interlocked.CompareExchange<string>(
            ref Val, newValue, initialValue));

        return newValue;
    }	
}
