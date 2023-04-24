// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using Xunit;

interface IGen<T>
{
	void Target(object p);
	T Dummy(T t);
}

class GenInt : IGen<int>
{
	public int Dummy(int t) { return t; }

	public void Target(object p)
	{		
			ManualResetEvent evt = (ManualResetEvent) p;
			Interlocked.Increment(ref Test_thread13.Xcounter);
			evt.Set();
	}
	
	public static void ThreadPoolTest()
	{
		ManualResetEvent[] evts = new ManualResetEvent[Test_thread13.nThreads];
		WaitHandle[] hdls = new WaitHandle[Test_thread13.nThreads];

		for (int i=0; i<Test_thread13.nThreads; i++)
		{
			evts[i] = new ManualResetEvent(false);
			hdls[i] = (WaitHandle) evts[i];
		}

		IGen<int> obj = new GenInt();

		for (int i = 0; i <Test_thread13.nThreads; i++)
		{	
			WaitCallback cb = new WaitCallback(obj.Target);
			ThreadPool.QueueUserWorkItem(cb,evts[i]);
		}

		WaitHandle.WaitAll(hdls);
		Test_thread13.Eval(Test_thread13.Xcounter==Test_thread13.nThreads);
		Test_thread13.Xcounter = 0;
	}
}

class GenDouble : IGen<double>
{
	public double Dummy(double t) { return t; }

	public void Target(object p)
	{		
			ManualResetEvent evt = (ManualResetEvent) p;

            Interlocked.Increment(ref Test_thread13.Xcounter);
            evt.Set();
	}
	
	public static void ThreadPoolTest()
	{
		ManualResetEvent[] evts = new ManualResetEvent[Test_thread13.nThreads];
		WaitHandle[] hdls = new WaitHandle[Test_thread13.nThreads];

		for (int i=0; i<Test_thread13.nThreads; i++)
		{
			evts[i] = new ManualResetEvent(false);
			hdls[i] = (WaitHandle) evts[i];
		}

		IGen<double> obj = new GenDouble();

		for (int i = 0; i <Test_thread13.nThreads; i++)
		{	
			WaitCallback cb = new WaitCallback(obj.Target);
			ThreadPool.QueueUserWorkItem(cb,evts[i]);
		}

		WaitHandle.WaitAll(hdls);
		Test_thread13.Eval(Test_thread13.Xcounter==Test_thread13.nThreads);
		Test_thread13.Xcounter = 0;
	}
}


class GenString : IGen<string>
{
	public string Dummy(string t) { return t; }

	public void Target(object p)
	{		
			ManualResetEvent evt = (ManualResetEvent) p;

            Interlocked.Increment(ref Test_thread13.Xcounter);
            evt.Set();
	}
	
	public static void ThreadPoolTest()
	{
		ManualResetEvent[] evts = new ManualResetEvent[Test_thread13.nThreads];
		WaitHandle[] hdls = new WaitHandle[Test_thread13.nThreads];

		for (int i=0; i<Test_thread13.nThreads; i++)
		{
			evts[i] = new ManualResetEvent(false);
			hdls[i] = (WaitHandle) evts[i];
		}

		IGen<string> obj = new GenString();

		for (int i = 0; i <Test_thread13.nThreads; i++)
		{	
			WaitCallback cb = new WaitCallback(obj.Target);
			ThreadPool.QueueUserWorkItem(cb,evts[i]);
		}

		WaitHandle.WaitAll(hdls);
		Test_thread13.Eval(Test_thread13.Xcounter==Test_thread13.nThreads);
		Test_thread13.Xcounter = 0;
	}
}

class GenObject : IGen<object>
{
	public object Dummy(object t) { return t; }

	public void Target(object p)
	{		
			ManualResetEvent evt = (ManualResetEvent) p;

            Interlocked.Increment(ref Test_thread13.Xcounter);
            evt.Set();
	}
	
	public static void ThreadPoolTest()
	{
		ManualResetEvent[] evts = new ManualResetEvent[Test_thread13.nThreads];
		WaitHandle[] hdls = new WaitHandle[Test_thread13.nThreads];

		for (int i=0; i<Test_thread13.nThreads; i++)
		{
			evts[i] = new ManualResetEvent(false);
			hdls[i] = (WaitHandle) evts[i];
		}

		IGen<object> obj = new GenObject();

		for (int i = 0; i <Test_thread13.nThreads; i++)
		{	
			WaitCallback cb = new WaitCallback(obj.Target);
			ThreadPool.QueueUserWorkItem(cb,evts[i]);
		}

		WaitHandle.WaitAll(hdls);
		Test_thread13.Eval(Test_thread13.Xcounter==Test_thread13.nThreads);
		Test_thread13.Xcounter = 0;
	}
}

class GenGuid : IGen<Guid>
{
	public Guid Dummy(Guid t) { return t; }

	public void Target(object p)
	{		
			ManualResetEvent evt = (ManualResetEvent) p;

            Interlocked.Increment(ref Test_thread13.Xcounter);
            evt.Set();
	}
	
	public static void ThreadPoolTest()
	{
		ManualResetEvent[] evts = new ManualResetEvent[Test_thread13.nThreads];
		WaitHandle[] hdls = new WaitHandle[Test_thread13.nThreads];

		for (int i=0; i<Test_thread13.nThreads; i++)
		{
			evts[i] = new ManualResetEvent(false);
			hdls[i] = (WaitHandle) evts[i];
		}

		IGen<Guid> obj = new GenGuid();

		for (int i = 0; i <Test_thread13.nThreads; i++)
		{	
			WaitCallback cb = new WaitCallback(obj.Target);
			ThreadPool.QueueUserWorkItem(cb,evts[i]);
		}

		WaitHandle.WaitAll(hdls);
		Test_thread13.Eval(Test_thread13.Xcounter==Test_thread13.nThreads);
		Test_thread13.Xcounter = 0;
	}
}
public class Test_thread13
{
	public static int nThreads =50;
	public static int counter = 0;
	public static int Xcounter = 0;
	public static bool result = true;
	public static void Eval(bool exp)
	{
		counter++;
		if (!exp)
		{
			result = exp;
			Console.WriteLine("Test Failed at location: " + counter);
		}
	
	}
	
	[Fact]
	public static int TestEntryPoint()
	{
	
		GenInt.ThreadPoolTest();
		GenDouble.ThreadPoolTest();
		GenString.ThreadPoolTest();
		GenObject.ThreadPoolTest(); 
		GenGuid.ThreadPoolTest(); 
		
		if (result)
		{
			Console.WriteLine("Test Passed");
			return 100;
		}
		else
		{
			Console.WriteLine("Test Failed");
			return 1;
		}
	}
}		


