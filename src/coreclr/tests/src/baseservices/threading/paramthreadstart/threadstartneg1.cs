// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;

class ThreadStartInt
{
    public static int Main()
    {
        ThreadStartInt tsi = new ThreadStartInt();
        return tsi.Run();
    }

    private int Run()
    {
        int iRet = -1;
        Thread t = new Thread(new ParameterizedThreadStart(ThreadWorker));
        t.Start(12345);
        try
        {
            t.Start(12345);
        }
        catch(ThreadStateException)
        {
            // Expected
            iRet = 100;
        }
        catch(Exception ex)
        {
            Console.WriteLine("Unexpected exception thrown: " + ex.ToString());
        }
        t.Join();
        Console.WriteLine(100 == iRet ? "Test Passed" : "Test Failed");
        return iRet;
    }

    private void ThreadWorker(Object o)
    {
        Console.WriteLine(o);
        Thread.Sleep(1000);
    }
}