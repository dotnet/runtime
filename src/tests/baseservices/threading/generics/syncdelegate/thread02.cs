// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using Xunit;

class Gen<T> 
{
	public virtual void Target()
	{		
		Interlocked.Increment(ref Test_thread02.Xcounter);
	}
	public static void DelegateTest()
	{
		ThreadStart d = new ThreadStart(new Gen<T>().Target);
		d();
		Test_thread02.Eval(Test_thread02.Xcounter==1);
		Test_thread02.Xcounter = 0;
	}
}

public class Test_thread02
{
	public static int nThreads = 50;
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
		Gen<int>.DelegateTest();
		Gen<double>.DelegateTest();
		Gen<string>.DelegateTest();
		Gen<object>.DelegateTest(); 
		Gen<Guid>.DelegateTest(); 

		Gen<int[]>.DelegateTest(); 
		Gen<double[,]>.DelegateTest();
		Gen<string[][][]>.DelegateTest(); 
		Gen<object[,,,]>.DelegateTest();
		Gen<Guid[][,,,][]>.DelegateTest();

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


