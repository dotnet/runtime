// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

public class Stop
{

    int av;
    AutoResetEvent are = new AutoResetEvent(false);

    unsafe public void T1Start()
    {
        are.Set();
        for (; ; )
        {
            try
            {
                int* p = null;
                *p = 1;
            }
            catch
            {
                av++;
            }
        }
    }

    public static int Main(String[] args)
    {
        Stop tm = new Stop();
        Thread t = new Thread(new ThreadStart(tm.RunTest));
        t.Start();
        //Sleep for 3 minutes
        Thread.Sleep(3 * 60 * 1000);
        ThreadEx.Abort(t);
        return 100;

    }
    public void RunTest()
    {        
        ThreadStart ts = new ThreadStart(this.T1Start);
        for (; ; )
        {
            Thread t1 = new Thread(ts);
            t1.IsBackground = true;
            t1.Start();
            are.WaitOne();
            Console.WriteLine("Aborting t1 " + this.av);
            ThreadEx.Abort(t1);
            Console.WriteLine("Aborted");
        }

    }
}
