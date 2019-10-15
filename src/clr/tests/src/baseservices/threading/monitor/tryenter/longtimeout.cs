// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading;

//Repro for VSWhidbey 565042

public class Repro
{
	public static object _monitor = new Object();
	private ManualResetEvent _event;
	private ManualResetEvent _event2;	
	public static int _status = 1;

	public static int Main(string[] arguments)
	{
		return (new Repro().RunTest());
	}

	public int RunTest()
	{
		_event = new ManualResetEvent(false);
		_event2 = new ManualResetEvent(false);
		
		Thread thread1 = new Thread(this.Thread1);
		thread1.Start();

		Thread thread2 = new Thread(this.Thread2);
		thread2.Start();

		thread2.Join();
		_event2.Set();

		if (_status == 100)
			Console.WriteLine("Test Passed");
		else
			Console.WriteLine("Test Failed");
		
		return _status;
	}

	private void Thread1()
	{
		Monitor.Enter(_monitor);
		_event.Set();

		_event2.WaitOne();
		Monitor.Exit(_monitor);
	}

	private void Thread2()
	{
		Stopwatch timer = new Stopwatch();
		_event.WaitOne();
		
		timer.Start();
		for (int i=10;i<=30;i=i+10)
		{
			bool tookLock = false;
			Monitor.TryEnter(_monitor, TimeSpan.FromSeconds(i), ref tookLock);

			if(tookLock)
			{
				_status = 98;
				Console.WriteLine("TryEnter got monitor that it should not have been able to....");
				Monitor.Exit(_monitor);
				break;
			}
			else
			{
				timer.Stop();
				if ((timer.Elapsed + TimeSpan.FromSeconds(1)) < TimeSpan.FromSeconds(i))
				{
					Console.WriteLine("TryEnter returned early in {0}, but expected {1}", timer.Elapsed, TimeSpan.FromSeconds(i));
					_status = 99;
					break;
				}
				else
				{
					Console.WriteLine("TryEnter returned as expected in {0}, with expected {1}", timer.Elapsed, TimeSpan.FromSeconds(i));
				}
				timer = Stopwatch.StartNew();
			}
		}

		if (_status == 1)
			_status=100;
	}
}
