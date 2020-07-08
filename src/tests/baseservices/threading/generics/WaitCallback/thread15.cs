// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;

interface IGen<T>
{
	void Target(object p);
	T Dummy(T t);
}

class GenInt : IGen<int>
{
	public int Dummy(int t) { return t; }

	public virtual void Target(object p)
	{		
			ManualResetEvent evt = (ManualResetEvent) p;
			Interlocked.Increment(ref Test.Xcounter);
			evt.Set();
	}
	
	public static void ThreadPoolTest()
	{
		ManualResetEvent[] evts = new ManualResetEvent[Test.nThreads];
		WaitHandle[] hdls = new WaitHandle[Test.nThreads];

		for (int i=0; i<Test.nThreads; i++)
		{
			evts[i] = new ManualResetEvent(false);
			hdls[i] = (WaitHandle) evts[i];
		}

		IGen<int> obj = new GenInt();

		for (int i = 0; i <Test.nThreads; i++)
		{	
			WaitCallback cb = new WaitCallback(obj.Target);
			ThreadPool.QueueUserWorkItem(cb,evts[i]);
		}

		WaitHandle.WaitAll(hdls);
		Test.Eval(Test.Xcounter==Test.nThreads);
		Test.Xcounter = 0;
	}
}

class GenDouble : IGen<double>
{
	public double Dummy(double t) { return t; }

	public virtual void Target(object p)
	{		
			ManualResetEvent evt = (ManualResetEvent) p;

            Interlocked.Increment(ref Test.Xcounter);
            evt.Set();
	}
	
	public static void ThreadPoolTest()
	{
		ManualResetEvent[] evts = new ManualResetEvent[Test.nThreads];
		WaitHandle[] hdls = new WaitHandle[Test.nThreads];

		for (int i=0; i<Test.nThreads; i++)
		{
			evts[i] = new ManualResetEvent(false);
			hdls[i] = (WaitHandle) evts[i];
		}

		IGen<double> obj = new GenDouble();

		for (int i = 0; i <Test.nThreads; i++)
		{	
			WaitCallback cb = new WaitCallback(obj.Target);
			ThreadPool.QueueUserWorkItem(cb,evts[i]);
		}

		WaitHandle.WaitAll(hdls);
		Test.Eval(Test.Xcounter==Test.nThreads);
		Test.Xcounter = 0;
	}
}


class GenString : IGen<string>
{
	public string Dummy(string t) { return t; }

	public virtual void Target(object p)
	{		
			ManualResetEvent evt = (ManualResetEvent) p;

            Interlocked.Increment(ref Test.Xcounter);
            evt.Set();
	}
	
	public static void ThreadPoolTest()
	{
		ManualResetEvent[] evts = new ManualResetEvent[Test.nThreads];
		WaitHandle[] hdls = new WaitHandle[Test.nThreads];

		for (int i=0; i<Test.nThreads; i++)
		{
			evts[i] = new ManualResetEvent(false);
			hdls[i] = (WaitHandle) evts[i];
		}

		IGen<string> obj = new GenString();

		for (int i = 0; i <Test.nThreads; i++)
		{	
			WaitCallback cb = new WaitCallback(obj.Target);
			ThreadPool.QueueUserWorkItem(cb,evts[i]);
		}

		WaitHandle.WaitAll(hdls);
		Test.Eval(Test.Xcounter==Test.nThreads);
		Test.Xcounter = 0;
	}
}

class GenObject : IGen<object>
{
	public object Dummy(object t) { return t; }

	public virtual void Target(object p)
	{		
			ManualResetEvent evt = (ManualResetEvent) p;

            Interlocked.Increment(ref Test.Xcounter);
            evt.Set();
	}
	
	public static void ThreadPoolTest()
	{
		ManualResetEvent[] evts = new ManualResetEvent[Test.nThreads];
		WaitHandle[] hdls = new WaitHandle[Test.nThreads];

		for (int i=0; i<Test.nThreads; i++)
		{
			evts[i] = new ManualResetEvent(false);
			hdls[i] = (WaitHandle) evts[i];
		}

		IGen<object> obj = new GenObject();

		for (int i = 0; i <Test.nThreads; i++)
		{	
			WaitCallback cb = new WaitCallback(obj.Target);
			ThreadPool.QueueUserWorkItem(cb,evts[i]);
		}

		WaitHandle.WaitAll(hdls);
		Test.Eval(Test.Xcounter==Test.nThreads);
		Test.Xcounter = 0;
	}
}

class GenGuid : IGen<Guid>
{
	public Guid Dummy(Guid t) { return t; }

	public virtual void Target(object p)
	{		
			ManualResetEvent evt = (ManualResetEvent) p;

            Interlocked.Increment(ref Test.Xcounter);
            evt.Set();
	}
	
	public static void ThreadPoolTest()
	{
		ManualResetEvent[] evts = new ManualResetEvent[Test.nThreads];
		WaitHandle[] hdls = new WaitHandle[Test.nThreads];

		for (int i=0; i<Test.nThreads; i++)
		{
			evts[i] = new ManualResetEvent(false);
			hdls[i] = (WaitHandle) evts[i];
		}

		IGen<Guid> obj = new GenGuid();

		for (int i = 0; i <Test.nThreads; i++)
		{	
			WaitCallback cb = new WaitCallback(obj.Target);
			ThreadPool.QueueUserWorkItem(cb,evts[i]);
		}

		WaitHandle.WaitAll(hdls);
		Test.Eval(Test.Xcounter==Test.nThreads);
		Test.Xcounter = 0;
	}
}
public class Test
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
	
	public static int Main()
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


