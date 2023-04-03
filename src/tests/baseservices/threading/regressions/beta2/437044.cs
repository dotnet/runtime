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

        for (int i = 0; i < 20000 && !_fTestFailed; i++)
        {
            _mre = new ManualResetEvent(false);
            Timer t = new Timer(new TimerCallback(callback), null, 1000000, Timeout.Infinite);
            _are.Set();

			bool bDisposeSucceeded = false; //Used to improve speed of the test when Dispose has failed
			try
			{
				t.Dispose();
				bDisposeSucceeded = true;
			}
			catch (ObjectDisposedException)
			{
			}

			if (bDisposeSucceeded)
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

    static void callback(object state)
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

            using(are = new AutoResetEvent(false))
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

