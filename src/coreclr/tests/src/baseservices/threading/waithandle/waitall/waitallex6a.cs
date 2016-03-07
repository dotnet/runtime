// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

class WaitAllEx
{
    private WaitHandle[] wh;
    private ManualResetEvent myMRE;

    private WaitAllEx()
    {
        myMRE = new ManualResetEvent(false);
    }

    public static int Main(string[] args)
    {
        // Check number of args
        if(args.Length != 2)
        {
            Console.WriteLine("USAGE:  WaitAllEx6a /size:<int> /pos:<int>");
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

    private int Run(int iArraySize, int iPosToAbandon)
    {
        int iRet = -1;
        Console.WriteLine("Abandon a particular mutex - " + iArraySize + ", " + 
            iPosToAbandon);
        CreateMutexArray(iArraySize);
        Thread t = new Thread(new 
            ParameterizedThreadStart(this.AbandonMutexPos));
        t.Start(iPosToAbandon);
        myMRE.WaitOne();
        try
        {
            Console.WriteLine("Waiting...");
            bool bRet = WaitHandle.WaitAll(wh);
            Console.WriteLine("WaitAll did not throw an " +
                "exception, bRet = " + bRet);
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

    private void AbandonMutexPos(Object o)
    {
        wh[Convert.ToInt32(o)].WaitOne();
        myMRE.Set();
        Thread.Sleep(1000);
    }

    private void CreateMutexArray(int numElements)
    {
        wh = new WaitHandle[numElements];
        for(int i=0;i<numElements;i++)
        {
            wh[i] = new Mutex();
        }
    }
}
