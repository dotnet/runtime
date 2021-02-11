// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;

class ManualResetCtor
{
    EventWaitHandle ewh;

    public static int Main()
    {
        ManualResetCtor mrc = new ManualResetCtor();
        return mrc.Run();
    }

    private int Run()
    {
        // Testing the initialState = true for a ManualResetEvent
        int iRet = -1;
        ewh = (EventWaitHandle)new ManualResetEvent(true);

        Thread t = new Thread(new ThreadStart(ThreadWorker));
        t.Start();
        t.Join();

        // This should return immediately
        ewh.WaitOne();
        ewh.Reset();

        // when doing another wait, it should not return until set.
        bool b = ewh.WaitOne(5000);//, false);
        if(b)
            Console.WriteLine("Event didn't reset!");
        else
            iRet = 100;

        Console.WriteLine(100 == iRet ? "Test Passed" : "Test Failed");
        return iRet;
    }
    
    private void ThreadWorker()
    {
        // This should return immediately
        ewh.WaitOne();
    }
}
