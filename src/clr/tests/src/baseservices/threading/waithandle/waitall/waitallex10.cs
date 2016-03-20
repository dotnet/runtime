// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

class WaitAllEx
{
    WaitHandle[] wh;
    ManualResetEvent myMRE;

    public WaitAllEx()
    {
        myMRE = new ManualResetEvent(false);
    }

    public static int Main(string[] args)
    {
        // Check number of args
        if(args.Length != 2)
        {
            Console.WriteLine("USAGE:  WaitAllEx10 /size:<int> /pos:<int>");
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

        WaitAllEx wae = new WaitAllEx();
        return wae.Run(iSize, iPos);
    }

    private int Run(int iNumElements, int iPos)
    {
        int iRet = -1;
        Console.WriteLine("Abandon during a wait, signaling others - " + 
            iNumElements + ", " + iPos);
        CreateOneMutexArray(iNumElements, iPos);

        Thread t = new Thread(new ThreadStart(this.AbandonAllMutexesWait));
        t.Start();
        myMRE.WaitOne();
        try
        {
            Console.WriteLine("Waiting...");
            bool bRet = WaitHandle.WaitAll(wh, 10000);
            Console.WriteLine("AbandonedMutexException was not thrown!");
        }
        catch(AbandonedMutexException)
        {
            // Expected
            iRet = 100;
        }
        catch(Exception e)
        {
            Console.WriteLine("Unexpected exception thrown: " + 
                e.ToString());
        }
        Console.WriteLine(100 == iRet ? "Test Passed" : "Test Failed");
        return iRet;
    }

    private void AbandonAllMutexesWait()
    {
        Mutex m = new Mutex();
        foreach(WaitHandle w in wh)
        {
            if(w.GetType() == m.GetType())
                w.WaitOne();
            else
                ((ManualResetEvent)w).Set();
        }
        myMRE.Set();
        Thread.Sleep(1000);
    }

    private void CreateOneMutexArray(int numElements, int iPos)
    {
        wh = new WaitHandle[numElements];
        for(int i=0;i<numElements;i++)
        {
            if(i == iPos)
                wh[i] = new Mutex(false, Common.GetUniqueName());
            else
                wh[i] = new ManualResetEvent(false);
        }
    }
}