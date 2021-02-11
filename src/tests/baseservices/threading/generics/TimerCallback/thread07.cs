// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;

struct Gen<T> 
{
	public void Target(object p)
	{			
		if (Test.Xcounter>=Test.nThreads)
		{
			ManualResetEvent evt = (ManualResetEvent) p;	
			evt.Set();
		}
		else
		{
			Interlocked.Increment(ref Test.Xcounter);	
		}
	}
	
	public static void ThreadPoolTest()
	{
		ManualResetEvent evt = new ManualResetEvent(false);		
		
		Gen<T> obj = new Gen<T>();

		TimerCallback tcb = new TimerCallback(obj.Target);
		Timer timer = new Timer(tcb,evt,Test.delay,Test.period);
	
		evt.WaitOne();
		timer.Dispose();
		Test.Eval(Test.Xcounter>=Test.nThreads);
		Test.Xcounter = 0;
	}
}

public class Test
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
	
	public static int Main()
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


