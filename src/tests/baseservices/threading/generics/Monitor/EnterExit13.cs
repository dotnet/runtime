// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;


public struct ValX1<T> {}
public class RefX1<T> {}

struct Gen<T> 
{
	public static object staticLock;
	
	public static void EnterExitTest()
	{
		staticLock = new object();
		object monitor = staticLock;

		TestHelper myHelper = new TestHelper(Test.nThreads);
		// MonitorDelegate[] consumer = new MonitorDelegate[Test.nThreads];
		// for(int i=0;i<consumer.Length;i++){
		// 	consumer[i] = new MonitorDelegate(myHelper.Consumer);
		// 	consumer[i].BeginInvoke(staticLock,null,null);
		// }

		for (int i = 0; i < Test.nThreads; i++)
		{
			ThreadPool.QueueUserWorkItem(state =>
			{
				myHelper.Consumer(staticLock);
			});
		}

		for(int i=0;i<6;i++){
			if(myHelper.m_Event.WaitOne(10000))//,true))
				break;
			if(myHelper.Error == true)
				break;
		}
		Test.Eval(!myHelper.Error);
	}
}

public class Test
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
	
	public static int Main()
	{
		Gen<int>.EnterExitTest();	
		Gen<double>.EnterExitTest();
		Gen<string>.EnterExitTest();
		Gen<object>.EnterExitTest();
		Gen<Guid>.EnterExitTest();

		Gen<int[]>.EnterExitTest();
		Gen<double[,]>.EnterExitTest();
		Gen<string[][][]>.EnterExitTest();
		Gen<object[,,,]>.EnterExitTest();
		Gen<Guid[][,,,][]>.EnterExitTest();

		Gen<RefX1<int>[]>.EnterExitTest();
		Gen<RefX1<double>[,]>.EnterExitTest();
		Gen<RefX1<string>[][][]>.EnterExitTest();
		Gen<RefX1<object>[,,,]>.EnterExitTest();
		Gen<RefX1<Guid>[][,,,][]>.EnterExitTest();

		Gen<ValX1<int>[]>.EnterExitTest();
		Gen<ValX1<double>[,]>.EnterExitTest();
		Gen<ValX1<string>[][][]>.EnterExitTest();
		Gen<ValX1<object>[,,,]>.EnterExitTest();
		Gen<ValX1<Guid>[][,,,][]>.EnterExitTest();

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


