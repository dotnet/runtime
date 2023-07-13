// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using Xunit;


public struct ValX1<T> {}
public class RefX1<T> {}


class Gen<T> 
{
	public static void EnterExitTest()
	{
		Gen<T> monitorT = new Gen<T>();
		Gen<T> monitorU = new Gen<T>();
		
		if(monitorU.Equals(monitorT))
			throw new Exception("Invalid use of test case, T must not be equal to U - POSSIBLE TYPE SYSTEM BUG");
				
		TestHelper myHelper = new TestHelper(Test_EnterExit10.nThreads);
		TestHelper myHelper2 = new TestHelper(Test_EnterExit10.nThreads);
		WaitHandle[] myWaiter = new WaitHandle[2];
		myWaiter[0] = myHelper.m_Event;
		myWaiter[1] = myHelper2.m_Event;
		// for(int i=0;i<Test.nThreads;i++)
		// {
		// 	new MonitorDelegate(myHelper.Consumer).BeginInvoke(monitorT,null,null);
		// 	new MonitorDelegate(myHelper2.Consumer).BeginInvoke(monitorU,null,null);
		// }

		for(int i=0;i<Test_EnterExit10.nThreads;i++)
		{
			ThreadPool.QueueUserWorkItem(state =>
			{
				myHelper.Consumer(monitorT);
			});

			ThreadPool.QueueUserWorkItem(state =>
			{
				myHelper2.Consumer(monitorU);
			});
		}

		for(int i=0;i<6;i++)
		{	
			if(WaitHandle.WaitAll(myWaiter,10000))//,true))
				break;
			if(myHelper.Error == true || myHelper2.Error == true)
				break;
		}
		Test_EnterExit10.Eval(!(myHelper.Error || myHelper2.Error));
	}

}

public class Test_EnterExit10
{
	public static int nThreads = 10;
	public static int counter = 0;
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


