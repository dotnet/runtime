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
        Console.WriteLine("Abandon same named mutex");
        // Create array with the same name
        wh = new Mutex[2];
        string sName = Common.GetUniqueName();
        wh[0] = new Mutex(false, sName);
        wh[1] = new Mutex(false, sName);

        Thread t = new Thread(new 
            ParameterizedThreadStart(this.AbandonMutexPos));
        t.Start(0);
        t.Join();
        try
        {
            Console.WriteLine("Waiting...");
            int i = WaitHandle.WaitAny(wh);
            Console.WriteLine("WaitAny did not throw an " +
                "exception, i = " + i);
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
    }
}
