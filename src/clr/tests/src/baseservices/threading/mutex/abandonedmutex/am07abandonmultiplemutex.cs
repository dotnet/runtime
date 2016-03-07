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

    public static int Main()
    {
        WaitAnyEx wae = new WaitAnyEx();
        wae.Run();

        Console.WriteLine(100 == wae.iRet ? "Test Passed" : "Test Failed");
        return wae.iRet;
    }

    private void Run()
    {
        CreateMutexArray(64);
        int[] aPos = new int[5]{6,8,10,12,14};
        Thread t2 = new Thread(new ParameterizedThreadStart(this.AbandonElse));
        t2.Start(aPos[0]);

        Thread t = new Thread(new 
            ParameterizedThreadStart(this.AbandonMutexPos));
        t.Start(aPos);
        mre.WaitOne();
        int i = -1;
        Thread.Sleep(400);

        try
        {
            Console.WriteLine("Waiting...");
            i = WaitHandle.WaitAny(wh, -1);
            Console.WriteLine("No exception thrown - {0}", i);
        }
        catch(AbandonedMutexException am)
        {
            Console.WriteLine("AbandonedMutexException thrown!  Checking values...");
            if(aPos[0] == am.MutexIndex && wh[am.MutexIndex] == am.Mutex)
                iRet = 100;
        }
        catch(Exception e)
        {
            Console.WriteLine("Unexpected exception thrown: " + 
                e.ToString());
        }
        t.Join();
    }

    private void AbandonElse(Object o)
    {
        int iPos = (int)o;

        // Do a wait on all Mutexes up to the 1st one
        for(int i=0;i<iPos;i++)
        {
            wh[i].WaitOne();
            Console.WriteLine("Blocking {0}", i);
        }

        mre.WaitOne();
        Thread.Sleep(5000);
    }

    private void AbandonMutexPos(Object o)
    {
        // This thread needs to complete before the wait
        // otherwise the Wait will return immediately

        int[] iPos = (int[])o;
        for(int i=0;i<iPos.Length;i++)
        {
            wh[iPos[i]].WaitOne();
            Console.WriteLine(iPos[i]);
        }
        mre.Set();
    }

    private void CreateMutexArray(int numElements)
    {
        wh = new WaitHandle[numElements];
        for(int i=0;i<numElements;i++)
        {
            wh[i] = new Mutex(false, Common.GetUniqueName());
        }
    }
}