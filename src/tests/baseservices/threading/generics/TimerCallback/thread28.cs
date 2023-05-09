// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using Xunit;

struct Gen<T> 
{
	public static void Target<U>(object p)
	{	
		//dummy line to avoid warnings
		Test_thread28.Eval(typeof(U)!=p.GetType());		
		if (Test_thread28.Xcounter>=Test_thread28.nThreads)
		{
			ManualResetEvent evt = (ManualResetEvent) p;	
			evt.Set();
		}
		else
		{
			Interlocked.Increment(ref Test_thread28.Xcounter);	
		}
	}
	
	public static void ThreadPoolTest<U>()
	{
		ManualResetEvent evt = new ManualResetEvent(false);		

		TimerCallback tcb = new TimerCallback(Gen<T>.Target<U>);
		Timer timer = new Timer(tcb,evt,Test_thread28.delay,Test_thread28.period);
	
		evt.WaitOne();
		timer.Dispose();
		Test_thread28.Eval(Test_thread28.Xcounter>=Test_thread28.nThreads);
		Test_thread28.Xcounter = 0;
	}
}

public class Test_thread28
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


