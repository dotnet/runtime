// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

// This is a non-9x test.

class WaitAnyEx
{
    WaitHandle[] wh;

    public static int Main()
    {
        WaitAnyEx wae = new WaitAnyEx();
        return wae.Run();
    }

    private int Run()
    {
        int iRet = -1;
        Console.WriteLine("Abandoning only one Mutex " +
            "in array with other WaitHandles");
        CreateArray(64);
        Thread t = new Thread(new ThreadStart(this.AbandonOne));
        t.Start();
        t.Join();
        try
        {
            Console.WriteLine("Waiting...");
            int i = WaitHandle.WaitAny(wh, 30000);
            Console.WriteLine("WaitAny did not throw AbandonedMutexException. Result: {0}", i);
        }
        catch(AbandonedMutexException)
        {
            // Expected
            iRet = 100;
        }
        Console.WriteLine(100 == iRet ? "Test Passed" : "Test Failed");
        return iRet;
    }

    private void AbandonOne()
    {
        foreach(WaitHandle w in wh)
        {
            if(w is Mutex)
            {
                w.WaitOne();
                break;
            }
        }
    }

    private void CreateArray(int numElements)
    {
        wh = new WaitHandle[numElements];
        for(int i=0;i<numElements;i++)
        {
            switch(i%3)
            {
                case 0:
                    wh[i] = new ManualResetEvent(false);
                    break;
                case 1:
                    wh[i] = new Mutex(false, Common.GetUniqueName());
                    break;
                case 2:
                    wh[i] = new Semaphore(0,5);
                    break;
            }
        }
    }
}
