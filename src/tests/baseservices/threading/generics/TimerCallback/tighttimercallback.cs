// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using System.Diagnostics;
using Xunit;

//namespace TimerCallbackTests ////////////// added this namesp

class Gen<T> 
{
	public static Type objType;

	public void Target(object p)
	{			
		Interlocked.Increment(ref Test.Xcounter);
		if (p.GetType() != objType)
		{
			Test.result = false;
			Console.WriteLine("Expected parameter type: " + objType + ", but found type: " + p.GetType());
		}

		if (this.GetType() != objType)
		{
			Test.result = false;
			Console.WriteLine("Expected this type: " + objType + ", but found type: " + this.GetType());
		}
		
	}
	
	public static void ThreadPoolTest()
	{			
		Gen<T> obj = new Gen<T>();
		objType = obj.GetType();

		TimerCallback tcb = new TimerCallback(obj.Target);
		Stopwatch testWatch = new Stopwatch();
		testWatch.Start();
		Timer timer = new Timer(tcb,obj,Test.delay,Test.period);
		while (testWatch.ElapsedMilliseconds < Test.timeToRun)
		{
			Thread.Sleep(0);
		}

		timer.Dispose();
		testWatch.Stop();

		if (Test.Xcounter > ((testWatch.ElapsedMilliseconds / Test.period)+2))
		{
			Test.result = false;
			Console.WriteLine("Expected Timer to run at most " + ((testWatch.ElapsedMilliseconds / Test.period)+2) + " times, but found " + Test.Xcounter + " runs in " + obj.GetType() + " type object.");
		}		

		Test.Xcounter = 0;
	}
}

public class Test
{
	public static int delay = 0;
	public static int period = 30;
	public static int Xcounter = 0;
	public static bool result = true;
	public static int timeToRun = 5000;
	
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
