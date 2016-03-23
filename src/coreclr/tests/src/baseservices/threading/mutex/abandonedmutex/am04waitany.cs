// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

// This is a non-9x test.

class WaitAnyEx
{
    WaitHandle[] wh;
    private int iRet = -1;
    private ManualResetEvent mre = new ManualResetEvent(false);

    public static int Main(string[] args)
    {
        // Check number of args
        if(args.Length != 2)
        {
            Console.WriteLine("USAGE:  AM04WaitAny /size:<int> /pos:<int>");
            return -1;
        }

        // Get the args
        int iPos=-1, iSize = -1;;
        
        for(int i=0;i<args.Length;i++)
        {
            if(args[i].ToLower().StartsWith("/size:"))
            {
                iSize = Convert.ToInt32(args[i].Substring(6));
                continue;
            }

            if(args[i].ToLower().StartsWith("/pos:"))
            {
                iPos = Convert.ToInt32(args[i].Substring(5));
            }
        }

        WaitAnyEx wae = new WaitAnyEx();
        wae.Run(iSize, iPos);

        Console.WriteLine(100 == wae.iRet ? "Test Passed" : "Test Failed");
        return wae.iRet;
    }

    private void Run(int iArraySize, int iPosToAbandon)
    {
        Console.WriteLine("Abandon a particular mutex - " + iArraySize + ", " + 
            iPosToAbandon);
        CreateMutexArray(iArraySize, iPosToAbandon);
        Thread t = new Thread(new 
            ParameterizedThreadStart(this.AbandonMutexPos));
        t.Start(iPosToAbandon);
        mre.WaitOne();
        int i = -1;

        try
        {
            Console.WriteLine("Waiting...");
            i = WaitHandle.WaitAny(wh, -1);
        }
        catch(AbandonedMutexException am)
        {
            Console.WriteLine("AbandonedMutexException thrown!  Checking values...");
            if(iPosToAbandon == am.MutexIndex && wh[am.MutexIndex] == am.Mutex)
                iRet = 50;

            // We should now be in a state where this thread owns the mutex
            am.Mutex.WaitOne();
            am.Mutex.ReleaseMutex();
        }
        catch(Exception e)
        {
            Console.WriteLine("Unexpected exception thrown: " + 
                e.ToString());
        }
        t.Join();

        // Release a 2nd time
        try
        {
            Console.WriteLine("Release again");
            ((Mutex)wh[iPosToAbandon]).ReleaseMutex();
            iRet += 50;
        }
        catch(Exception e)
        {
            Console.WriteLine("Unexpected exception thrown: " + 
                e.ToString());
        }
    }

    private void AbandonMutexPos(Object o)
    {
        wh[Convert.ToInt32(o)].WaitOne();
        mre.Set();
    }

    private void CreateMutexArray(int numElements, int iPos)
    {
        wh = new WaitHandle[numElements];
        for(int i=0;i<numElements;i++)
        {
            if(i != iPos)
                wh[i] = new AutoResetEvent(false);
            else
                wh[i] = new Mutex(false, Common.GetUniqueName());
        }
    }
}