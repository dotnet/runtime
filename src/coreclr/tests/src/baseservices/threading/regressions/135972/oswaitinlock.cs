// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

public class Bug
{
	public static int Main()
	{
		Console.WriteLine("Repro for Bug 135972");
		TestCase bugCheck = new TestCase();
		return bugCheck.Run();
	}
}

public class TestCase
{
	private int ret;
	private Object mylock;
	private AutoResetEvent are;

	public TestCase()
	{
		mylock = new Object();
		are = new AutoResetEvent(false);
	}

	public int Run()
	{
		lock(mylock)
		{
			ret = 0;
			Thread t;
			t = new Thread(new ThreadStart(BlockThreadOnWait));
			t.Start();
			Thread.Sleep(3000);
			Console.WriteLine("Signaling");
			are.Set();
			Thread.Sleep(1000);
			Console.WriteLine("Main Waiting");
			Thread.Sleep(1000);
			Console.WriteLine("Aborting New Thread");
			ThreadEx.Abort(t);
			t.Join();
		}
		return ret;

	}
	
	public void BlockThreadOnWait()
	{
		try{
			Console.WriteLine("New Thread Waiting");
			are.WaitOne();
			Console.WriteLine("New Thread Signaled");
			lock(mylock){
				//This line will never print since the lock is held by thread 1
				//Need this here so the jit doesn't optimize this lock away
				ret = -1;
				Console.WriteLine("In The Lock Sleep");
			}
		}
		catch(Exception)
		{
			Interlocked.CompareExchange(ref ret,100,0);
		}
	}
}
