// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;

interface IGen<T>
{
	void Target();
	T Dummy(T t);
}

struct GenInt : IGen<int>
{
	public int Dummy(int t) { return t; }

	public void Target()
	{		
		Interlocked.Increment(ref Test.Xcounter);
	}
	
	public static void ThreadPoolTest()
	{
		Thread[] threads = new Thread[Test.nThreads];
		IGen<int> obj = new GenInt();

		for (int i = 0; i < Test.nThreads; i++)
		{	
			threads[i]  = new Thread(new ThreadStart(obj.Target));
			threads[i].Start();
		}

		for (int i = 0; i < Test.nThreads; i++)
		{	
			threads[i].Join();
		}
		
		Test.Eval(Test.Xcounter==Test.nThreads);
		Test.Xcounter = 0;
	}
}

struct GenDouble : IGen<double>
{
	public double Dummy(double t) { return t; }

	public void Target()
	{		
		Interlocked.Increment(ref Test.Xcounter);
	}
	
	public static void ThreadPoolTest()
	{
		Thread[] threads = new Thread[Test.nThreads];
		IGen<double> obj = new GenDouble();

		for (int i = 0; i < Test.nThreads; i++)
		{	
			threads[i]  = new Thread(new ThreadStart(obj.Target));
			threads[i].Start();
		}

		for (int i = 0; i < Test.nThreads; i++)
		{	
			threads[i].Join();
		}
		
		Test.Eval(Test.Xcounter==Test.nThreads);
		Test.Xcounter = 0;
	}
}

struct GenString : IGen<string>
{
	public string Dummy(string t) { return t; }

	public void Target()
	{		
		Interlocked.Increment(ref Test.Xcounter);
	}
	
	public static void ThreadPoolTest()
	{
		Thread[] threads = new Thread[Test.nThreads];
		IGen<string> obj = new GenString();

		for (int i = 0; i < Test.nThreads; i++)
		{	
			threads[i]  = new Thread(new ThreadStart(obj.Target));
			threads[i].Start();
		}

		for (int i = 0; i < Test.nThreads; i++)
		{	
			threads[i].Join();
		}
		
		Test.Eval(Test.Xcounter==Test.nThreads);
		Test.Xcounter = 0;
	}
}

struct GenObject : IGen<object>
{
	public object Dummy(object t) { return t; }

	public void Target()
	{		
		Interlocked.Increment(ref Test.Xcounter);
	}
	
	public static void ThreadPoolTest()
	{
		Thread[] threads = new Thread[Test.nThreads];
		IGen<object> obj = new GenObject();

		for (int i = 0; i < Test.nThreads; i++)
		{	
			threads[i]  = new Thread(new ThreadStart(obj.Target));
			threads[i].Start();
		}

		for (int i = 0; i < Test.nThreads; i++)
		{	
			threads[i].Join();
		}
		
		Test.Eval(Test.Xcounter==Test.nThreads);
		Test.Xcounter = 0;
	}
}

struct GenGuid : IGen<Guid>
{
	public Guid Dummy(Guid t) { return t; }

	public void Target()
	{		
		Interlocked.Increment(ref Test.Xcounter);
	}
	
	public static void ThreadPoolTest()
	{
		Thread[] threads = new Thread[Test.nThreads];
		IGen<Guid> obj = new GenGuid();

		for (int i = 0; i < Test.nThreads; i++)
		{	
			threads[i]  = new Thread(new ThreadStart(obj.Target));
			threads[i].Start();
		}

		for (int i = 0; i < Test.nThreads; i++)
		{	
			threads[i].Join();
		}
		
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


