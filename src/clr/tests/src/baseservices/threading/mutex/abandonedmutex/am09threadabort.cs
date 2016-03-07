// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Threading;

class WaitAllEx
{
    private Mutex myMutex01 = new Mutex(false, Common.GetUniqueName());
    private Mutex myMutex02 = new Mutex(false, Common.GetUniqueName());
    private Mutex myMutex03 = new Mutex(false, Common.GetUniqueName());
    private ManualResetEvent myMRE = new ManualResetEvent(false);
    private int iRet = -1;

    private static void ThreadAbort(Thread thread)
    {
        MethodInfo abort = null;
        foreach(MethodInfo m in thread.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (m.Name.Equals("AbortInternal") && m.GetParameters().Length == 0) abort = m;
        }
        if (abort == null)
        {
            throw new Exception("Failed to get Thread.Abort method");
        }
        abort.Invoke(thread, new object[0]);
    }

    public static int Main()
    {
        WaitAllEx wae = new WaitAllEx();
        wae.Run();
        Console.WriteLine(wae.iRet == 100 ? "Test Passed!" : "Test Failed");
        return wae.iRet;
    }

    private void Run()
    {
        Thread t = new Thread(new ThreadStart(this.AbandonTheMutex));
        t.Start();
        myMRE.WaitOne();
        Thread.Sleep(500);
        ThreadAbort(t);

        try
        {
            Console.WriteLine("Waiting...");
            int ret = WaitHandle.WaitAny(
                new WaitHandle[]{myMutex01, myMutex02, myMutex03}, 5000);
            Console.WriteLine("WaitAll did not throw an " +
                "exception, return = " + ret);
        }
        catch(AbandonedMutexException am)
        {
            Console.WriteLine("AbandonedMutexException thrown! Checking values...");
            if(0 == am.MutexIndex && myMutex01 == am.Mutex)
                iRet = 100;
        }
    }

    private void AbandonTheMutex()
    {
        myMutex01.WaitOne();
        myMutex02.WaitOne();
        myMutex03.WaitOne();
        myMRE.Set();
        Thread.Sleep(10000);
    }
}
