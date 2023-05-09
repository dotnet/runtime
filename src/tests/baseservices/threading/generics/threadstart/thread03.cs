// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using Xunit;

class Gen 
{
	public void Target<U>()
	{		
			//dummy line to avoid warnings
			Test_thread03.Eval(typeof(U)!=null);
			Interlocked.Increment(ref Test_thread03.Xcounter);
	}
	public static void ThreadPoolTest<U>()
	{
		Thread[] threads = new Thread[Test_thread03.nThreads];
		Gen obj = new Gen();

		for (int i = 0; i < Test_thread03.nThreads; i++)
		{	
			threads[i]  = new Thread(new ThreadStart(obj.Target<U>));
			threads[i].Start();
		}

		for (int i = 0; i < Test_thread03.nThreads; i++)
		{	
			threads[i].Join();
		}
		
		Test_thread03.Eval(Test_thread03.Xcounter==Test_thread03.nThreads);
		Test_thread03.Xcounter = 0;
	}
}

public class Test_thread03
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
		Gen.ThreadPoolTest<object>();
		Gen.ThreadPoolTest<string>();
		Gen.ThreadPoolTest<Guid>();
		Gen.ThreadPoolTest<int>(); 
		Gen.ThreadPoolTest<double>(); 

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


