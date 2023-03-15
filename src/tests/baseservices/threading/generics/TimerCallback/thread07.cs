// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using Xunit;

struct Gen<T> 
{
	public void Target(object p)
	{			
		if (Test_thread07.Xcounter>=Test_thread07.nThreads)
		{
			ManualResetEvent evt = (ManualResetEvent) p;	
			evt.Set();
		}
		else
		{
			Interlocked.Increment(ref Test_thread07.Xcounter);	
		}
	}
	
	public static void ThreadPoolTest()
	{
		ManualResetEvent evt = new ManualResetEvent(false);		
		
		Gen<T> obj = new Gen<T>();

		TimerCallback tcb = new TimerCallback(obj.Target);
		Timer timer = new Timer(tcb,evt,Test_thread07.delay,Test_thread07.period);
	
		evt.WaitOne();
		timer.Dispose();
		Test_thread07.Eval(Test_thread07.Xcounter>=Test_thread07.nThreads);
		Test_thread07.Xcounter = 0;
	}
}

public class Test_thread07
{
	public static int delay = 0;
	public static int period = 2;
	public static int nThreads = 5;
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
		Gen<int>.ThreadPoolTest();
		Gen<double>.ThreadPoolTest();
		Gen<string>.ThreadPoolTest();
		Gen<object>.ThreadPoolTest(); 
		Gen<Guid>.ThreadPoolTest(); 

		Gen<int[]>.ThreadPoolTest(); 
		Gen<double[,]>.ThreadPoolTest();
		Gen<string[][][]>.ThreadPoolTest(); 
		Gen<object[,,,]>.ThreadPoolTest();
		Gen<Guid[][,,,][]>.ThreadPoolTest();

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


