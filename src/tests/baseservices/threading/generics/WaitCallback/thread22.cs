// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using Xunit;

interface IGen<T>
{
	void Target<U>(object p);
	T Dummy(T t);
}

class GenInt : IGen<int>
{
	public int Dummy(int t) { return t; }

	public void Target<U>(object p)
	{		
		//dummy line to avoid warnings
		Test_thread22.Eval(typeof(U)!=p.GetType());
		ManualResetEvent evt = (ManualResetEvent) p;
		Interlocked.Increment(ref Test_thread22.Xcounter);
		evt.Set();
	}
	
	public static void ThreadPoolTest<U>()
	{
		ManualResetEvent[] evts = new ManualResetEvent[Test_thread22.nThreads];
		WaitHandle[] hdls = new WaitHandle[Test_thread22.nThreads];

		for (int i=0; i<Test_thread22.nThreads; i++)
		{
			evts[i] = new ManualResetEvent(false);
			hdls[i] = (WaitHandle) evts[i];
		}

		IGen<int> obj = new GenInt();

		for (int i = 0; i <Test_thread22.nThreads; i++)
		{	
			WaitCallback cb = new WaitCallback(obj.Target<U>);
			ThreadPool.QueueUserWorkItem(cb,evts[i]);
		}

		WaitHandle.WaitAll(hdls);
		Test_thread22.Eval(Test_thread22.Xcounter==Test_thread22.nThreads);
		Test_thread22.Xcounter = 0;
	}
}

class GenDouble : IGen<double>
{
	public double Dummy(double t) { return t; }

	public void Target<U>(object p)
	{		
		//dummy line to avoid warnings
		Test_thread22.Eval(typeof(U)!=p.GetType());
		ManualResetEvent evt = (ManualResetEvent) p;

        Interlocked.Increment(ref Test_thread22.Xcounter);
        evt.Set();
	}
	
	public static void ThreadPoolTest<U>()
	{
		ManualResetEvent[] evts = new ManualResetEvent[Test_thread22.nThreads];
		WaitHandle[] hdls = new WaitHandle[Test_thread22.nThreads];

		for (int i=0; i<Test_thread22.nThreads; i++)
		{
			evts[i] = new ManualResetEvent(false);
			hdls[i] = (WaitHandle) evts[i];
		}

		IGen<double> obj = new GenDouble();

		for (int i = 0; i <Test_thread22.nThreads; i++)
		{	
			WaitCallback cb = new WaitCallback(obj.Target<U>);
			ThreadPool.QueueUserWorkItem(cb,evts[i]);
		}

		WaitHandle.WaitAll(hdls);
		Test_thread22.Eval(Test_thread22.Xcounter==Test_thread22.nThreads);
		Test_thread22.Xcounter = 0;
	}
}


class GenString : IGen<string>
{
	public string Dummy(string t) { return t; }

	public void Target<U>(object p)
	{		
		//dummy line to avoid warnings
		Test_thread22.Eval(typeof(U)!=p.GetType());
		ManualResetEvent evt = (ManualResetEvent) p;

        Interlocked.Increment(ref Test_thread22.Xcounter);
        evt.Set();
	}
	
	public static void ThreadPoolTest<U>()
	{
		ManualResetEvent[] evts = new ManualResetEvent[Test_thread22.nThreads];
		WaitHandle[] hdls = new WaitHandle[Test_thread22.nThreads];

		for (int i=0; i<Test_thread22.nThreads; i++)
		{
			evts[i] = new ManualResetEvent(false);
			hdls[i] = (WaitHandle) evts[i];
		}

		IGen<string> obj = new GenString();

		for (int i = 0; i <Test_thread22.nThreads; i++)
		{	
			WaitCallback cb = new WaitCallback(obj.Target<U>);
			ThreadPool.QueueUserWorkItem(cb,evts[i]);
		}

		WaitHandle.WaitAll(hdls);
		Test_thread22.Eval(Test_thread22.Xcounter==Test_thread22.nThreads);
		Test_thread22.Xcounter = 0;
	}
}

class GenObject : IGen<object>
{
	public object Dummy(object t) { return t; }

	public void Target<U>(object p)
	{		
		//dummy line to avoid warnings
		Test_thread22.Eval(typeof(U)!=p.GetType());
		ManualResetEvent evt = (ManualResetEvent) p;

        Interlocked.Increment(ref Test_thread22.Xcounter);
        evt.Set();
	}
	
	public static void ThreadPoolTest<U>()
	{
		ManualResetEvent[] evts = new ManualResetEvent[Test_thread22.nThreads];
		WaitHandle[] hdls = new WaitHandle[Test_thread22.nThreads];

		for (int i=0; i<Test_thread22.nThreads; i++)
		{
			evts[i] = new ManualResetEvent(false);
			hdls[i] = (WaitHandle) evts[i];
		}

		IGen<object> obj = new GenObject();

		for (int i = 0; i <Test_thread22.nThreads; i++)
		{	
			WaitCallback cb = new WaitCallback(obj.Target<U>);
			ThreadPool.QueueUserWorkItem(cb,evts[i]);
		}

		WaitHandle.WaitAll(hdls);
		Test_thread22.Eval(Test_thread22.Xcounter==Test_thread22.nThreads);
		Test_thread22.Xcounter = 0;
	}
}

class GenGuid : IGen<Guid>
{
	public Guid Dummy(Guid t) { return t; }

	public void Target<U>(object p)
	{		
		//dummy line to avoid warnings
		Test_thread22.Eval(typeof(U)!=p.GetType());
		ManualResetEvent evt = (ManualResetEvent) p;

        Interlocked.Increment(ref Test_thread22.Xcounter);
        evt.Set();
	}
	
	public static void ThreadPoolTest<U>()
	{
		ManualResetEvent[] evts = new ManualResetEvent[Test_thread22.nThreads];
		WaitHandle[] hdls = new WaitHandle[Test_thread22.nThreads];

		for (int i=0; i<Test_thread22.nThreads; i++)
		{
			evts[i] = new ManualResetEvent(false);
			hdls[i] = (WaitHandle) evts[i];
		}

		IGen<Guid> obj = new GenGuid();

		for (int i = 0; i <Test_thread22.nThreads; i++)
		{	
			WaitCallback cb = new WaitCallback(obj.Target<U>);
			ThreadPool.QueueUserWorkItem(cb,evts[i]);
		}

		WaitHandle.WaitAll(hdls);
		Test_thread22.Eval(Test_thread22.Xcounter==Test_thread22.nThreads);
		Test_thread22.Xcounter = 0;
	}
}
public class Test_thread22
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
	
		GenInt.ThreadPoolTest<int>();
		GenDouble.ThreadPoolTest<int>();
		GenString.ThreadPoolTest<int>();
		GenObject.ThreadPoolTest<int>(); 
		GenGuid.ThreadPoolTest<int>(); 

		GenInt.ThreadPoolTest<double>();
		GenDouble.ThreadPoolTest<double>();
		GenString.ThreadPoolTest<double>();
		GenObject.ThreadPoolTest<double>(); 
		GenGuid.ThreadPoolTest<double>(); 

		GenInt.ThreadPoolTest<string>();
		GenDouble.ThreadPoolTest<string>();
		GenString.ThreadPoolTest<string>();
		GenObject.ThreadPoolTest<string>(); 
		GenGuid.ThreadPoolTest<string>(); 

		GenInt.ThreadPoolTest<object>();
		GenDouble.ThreadPoolTest<object>();
		GenString.ThreadPoolTest<object>();
		GenObject.ThreadPoolTest<object>(); 
		GenGuid.ThreadPoolTest<object>(); 

		GenInt.ThreadPoolTest<Guid>();
		GenDouble.ThreadPoolTest<Guid>();
		GenString.ThreadPoolTest<Guid>();
		GenObject.ThreadPoolTest<Guid>(); 
		GenGuid.ThreadPoolTest<Guid>(); 

	
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


