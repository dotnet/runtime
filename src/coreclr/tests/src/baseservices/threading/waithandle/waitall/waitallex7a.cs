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

    public static int Main()
    {
        WaitAllEx wae = new WaitAllEx();
        return wae.Run();
    }

    private int Run()
    {
        int iRet = -1;
        Console.WriteLine("Abandon all mutexes in array");
        CreateMutexArray(64);
        Thread t = new Thread(new ThreadStart(this.AbandonAllMutexes));
        t.Start();
        myMRE.WaitOne();
        bool bRet = false;
        try
        {
            Console.WriteLine("Waiting...");
            bRet = WaitHandle.WaitAll(wh);
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

    private void AbandonAllMutexes()
    {
        Mutex m = new Mutex();
        AutoResetEvent are = new AutoResetEvent(false);

        foreach(WaitHandle w in wh)
        {
            if(w.GetType() == m.GetType())
                w.WaitOne();
        }
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
