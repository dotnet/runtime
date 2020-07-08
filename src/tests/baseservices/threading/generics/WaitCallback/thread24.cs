// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;

interface IGen<T>
{
	void Target<U>(object p);
	T Dummy(T t);
}

class GenInt : IGen<int>
{
	public int Dummy(int t) { return t; }

	public virtual void Target<U>(object p)
	{		
		//dummy line to avoid warnings
		Test.Eval(typeof(U)!=p.GetType());
		ManualResetEvent evt = (ManualResetEvent) p;
		Interlocked.Increment(ref Test.Xcounter);
		evt.Set();
	}
	
	public static void ThreadPoolTest<U>()
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
			WaitCallback cb = new WaitCallback(obj.Target<U>);
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

	public virtual void Target<U>(object p)
	{		
		//dummy line to avoid warnings
		Test.Eval(typeof(U)!=p.GetType());
		ManualResetEvent evt = (ManualResetEvent) p;
        Interlocked.Increment(ref Test.Xcounter);
        evt.Set();
    }
	
	public static void ThreadPoolTest<U>()
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
			WaitCallback cb = new WaitCallback(obj.Target<U>);
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

	public virtual void Target<U>(object p)
	{		
		//dummy line to avoid warnings
		Test.Eval(typeof(U)!=p.GetType());
		ManualResetEvent evt = (ManualResetEvent) p;
		Interlocked.Increment(ref Test.Xcounter);
		evt.Set();
	}
	
	public static void ThreadPoolTest<U>()
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
			WaitCallback cb = new WaitCallback(obj.Target<U>);
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

	public virtual void Target<U>(object p)
	{		
		//dummy line to avoid warnings
		Test.Eval(typeof(U)!=p.GetType());
		ManualResetEvent evt = (ManualResetEvent) p;
		Interlocked.Increment(ref Test.Xcounter);
		evt.Set();
	}
	
	public static void ThreadPoolTest<U>()
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
			WaitCallback cb = new WaitCallback(obj.Target<U>);
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

	public virtual void Target<U>(object p)
	{		
		//dummy line to avoid warnings
		Test.Eval(typeof(U)!=p.GetType());
		ManualResetEvent evt = (ManualResetEvent) p;
		Interlocked.Increment(ref Test.Xcounter);
		evt.Set();
	}
	
	public static void ThreadPoolTest<U>()
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
			WaitCallback cb = new WaitCallback(obj.Target<U>);
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


