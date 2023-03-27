// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Xunit;

public class Test
{    
    static bool _fTestFailed = false;
    static bool _fTestDone = false;
    static ManualResetEvent _mre;
    static AutoResetEvent _are = new AutoResetEvent(false);

    [Fact]
    public static int TestEntryPoint()
    {
        Thread th = new Thread(new ThreadStart(Thread2));
        th.Start();
        Thread th2 = new Thread(new ThreadStart(Thread3));
        th2.Start();
	System.Diagnostics.Stopwatch myTimer = new System.Diagnostics.Stopwatch();
	myTimer.Start();

        int i = 0;
	while (!_fTestFailed && myTimer.Elapsed.Minutes < 5 && i < 25000)
        {
            i++;
            ManualResetEvent mre = new ManualResetEvent(false);
            _mre = new ManualResetEvent(false);
            RegisteredWaitHandle rwh = ThreadPool.RegisterWaitForSingleObject(mre, new WaitOrTimerCallback(callback), null, -1, false);
            _are.Set();

            bool bUnregisterSucceeded = false; //Used to improve speed of the test when Unregister has failed
            try
            {
            	rwh.Unregister(_mre);
				bUnregisterSucceeded = true;
            }
            catch (ObjectDisposedException)
            {
            }

            if (bUnregisterSucceeded)
            {
               try
               {
                  if (_mre.WaitOne(0))
                  {
                    Console.Write("@");
                  }
               }
               catch (ObjectDisposedException)
               {
               }
            }

            if (i % 100 == 0) Console.WriteLine(i);
        }
        _fTestDone = true;
        _are.Set();
        th.Join();
        th2.Join();

		if (!_fTestFailed)
		{
			Console.WriteLine("Test Passed");
			return 100;
		}
		
		Console.WriteLine("Test Failed");
		return 101;

    }

    static void callback(object state, bool fTimedOut)
    {
        Console.Write("!");
        _fTestFailed = true;
    }

    public static void Thread3()
    {
        while (true & !_fTestDone)
        {
            _are.WaitOne();
            _mre.Dispose();
        }
    }

    public static void Thread2()
    {
        while (true & !_fTestDone)
        {
            Console.Write("#");
            AutoResetEvent are;
            using (are = new AutoResetEvent(false))
            {
                if (are.WaitOne(0))
                {
                    Console.WriteLine("ARE Signaled!");
                    _fTestFailed = true;
                }
            }

            using (are = new AutoResetEvent(false))
            {
                if (are.WaitOne(0))
                {
                    Console.WriteLine("ARE Signaled!");
                    _fTestFailed = true;
                }
            }

            using(are = new AutoResetEvent(false))
            {
                if (are.WaitOne(0))
                {
                    Console.WriteLine("ARE Signaled!");
                    _fTestFailed = true;
                }
            }
        }
    }
}

