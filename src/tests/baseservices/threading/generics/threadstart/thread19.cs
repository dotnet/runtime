// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using Xunit;


interface IGen<T>
{
	void Target<U>();
	T Dummy(T t);
}

class Gen<T> : IGen<T>
{
	public T Dummy(T t) {return t;}

	public void Target<U>()
	{		
		//dummy line to avoid warnings
		Test_thread19.Eval(typeof(U)!=null);
		Interlocked.Increment(ref Test_thread19.Xcounter);
	}
	public static void ThreadPoolTest<U>()
	{
		Thread[] threads = new Thread[Test_thread19.nThreads];
		IGen<T> obj = new Gen<T>();

		for (int i = 0; i < Test_thread19.nThreads; i++)
		{	
			threads[i]  = new Thread(new ThreadStart(obj.Target<U>));
			threads[i].Start();
		}

		for (int i = 0; i < Test_thread19.nThreads; i++)
		{	
			threads[i].Join();
		}
		
		Test_thread19.Eval(Test_thread19.Xcounter==Test_thread19.nThreads);
		Test_thread19.Xcounter = 0;
	}
}

public class Test_thread19
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
		Gen<int>.ThreadPoolTest<object>();
		Gen<double>.ThreadPoolTest<string>();
		Gen<string>.ThreadPoolTest<Guid>();
		Gen<object>.ThreadPoolTest<int>(); 
		Gen<Guid>.ThreadPoolTest<double>(); 

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


