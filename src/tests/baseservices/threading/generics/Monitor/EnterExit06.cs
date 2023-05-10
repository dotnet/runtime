// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using Xunit;


public struct ValX1<T> {}
public class RefX1<T> {}

class Gen<T> 
{
	public static void EnterExitTest<U>()
	{
		Type monitorT = typeof(Gen<T>).GetGenericTypeDefinition();
		Type monitorU = typeof(Gen<U>).GetGenericTypeDefinition();

		if(!monitorU.Equals(monitorT))
			throw new Exception("Invalid use of test case, T must be equal to U - POSSIBLE TYPE SYSTEM BUG");
				
		TestHelper myHelper = new TestHelper(Test_EnterExit06.nThreads);
		TestHelper myHelper2 = new TestHelper(Test_EnterExit06.nThreads);
		WaitHandle[] myWaiter = new WaitHandle[2];
		myWaiter[0] = myHelper.m_Event;
		myWaiter[1] = myHelper2.m_Event;
		
		// for(int i=0;i<Test.nThreads;i++)
		// {
		// 	new MonitorDelegate(myHelper.Consumer).BeginInvoke(monitorT,null,null);
		// 	new MonitorDelegate(myHelper2.Consumer).BeginInvoke(monitorU,null,null);
		// }

		for(int i=0;i<Test_EnterExit06.nThreads;i++)
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
		Test_EnterExit06.Eval(!(myHelper.Error || myHelper2.Error));
	}
	
}

public class Test_EnterExit06
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
		Gen<double>.EnterExitTest<int>();
		Gen<string>.EnterExitTest<int>();
		Gen<object>.EnterExitTest<int>();
		Gen<Guid>.EnterExitTest<int>();
		Gen<RefX1<int>>.EnterExitTest<int>();
		Gen<RefX1<string>>.EnterExitTest<int>();
		Gen<ValX1<int>>.EnterExitTest<int>();
		Gen<ValX1<string>>.EnterExitTest<int>();
		
		Gen<int>.EnterExitTest<double>();	
		Gen<string>.EnterExitTest<double>();
		Gen<object>.EnterExitTest<double>();
		Gen<Guid>.EnterExitTest<double>();
		Gen<RefX1<int>>.EnterExitTest<double>();
		Gen<RefX1<string>>.EnterExitTest<double>();
		Gen<ValX1<int>>.EnterExitTest<double>();
		Gen<ValX1<string>>.EnterExitTest<double>();

		Gen<int>.EnterExitTest<string>();	
		Gen<double>.EnterExitTest<string>();
		Gen<object>.EnterExitTest<string>();
		Gen<Guid>.EnterExitTest<string>();
		Gen<RefX1<int>>.EnterExitTest<string>();
		Gen<RefX1<string>>.EnterExitTest<string>();
		Gen<ValX1<int>>.EnterExitTest<string>();
		Gen<ValX1<string>>.EnterExitTest<string>();

		Gen<int>.EnterExitTest<object>();	
		Gen<double>.EnterExitTest<object>();
		Gen<string>.EnterExitTest<object>();
		Gen<Guid>.EnterExitTest<object>();
		Gen<RefX1<int>>.EnterExitTest<object>();
		Gen<RefX1<string>>.EnterExitTest<object>();
		Gen<ValX1<int>>.EnterExitTest<object>();
		Gen<ValX1<string>>.EnterExitTest<object>();

		Gen<int>.EnterExitTest<Guid>();	
		Gen<double>.EnterExitTest<Guid>();
		Gen<string>.EnterExitTest<Guid>();
		Gen<object>.EnterExitTest<Guid>();
		Gen<RefX1<int>>.EnterExitTest<Guid>();
		Gen<RefX1<string>>.EnterExitTest<Guid>();
		Gen<ValX1<int>>.EnterExitTest<Guid>();
		Gen<ValX1<string>>.EnterExitTest<Guid>();

		Gen<int>.EnterExitTest<RefX1<int>>();	
		Gen<double>.EnterExitTest<RefX1<int>>();
		Gen<string>.EnterExitTest<RefX1<int>>();
		Gen<object>.EnterExitTest<RefX1<int>>();
		Gen<Guid>.EnterExitTest<RefX1<int>>();
		Gen<RefX1<string>>.EnterExitTest<RefX1<int>>();
		Gen<ValX1<int>>.EnterExitTest<RefX1<int>>();
		Gen<ValX1<string>>.EnterExitTest<RefX1<int>>();	

		Gen<int>.EnterExitTest<RefX1<string>>();	
		Gen<double>.EnterExitTest<RefX1<string>>();
		Gen<string>.EnterExitTest<RefX1<string>>();
		Gen<object>.EnterExitTest<RefX1<string>>();
		Gen<Guid>.EnterExitTest<RefX1<string>>();
		Gen<RefX1<int>>.EnterExitTest<RefX1<string>>();
		Gen<ValX1<int>>.EnterExitTest<RefX1<string>>();
		Gen<ValX1<string>>.EnterExitTest<RefX1<string>>();

		Gen<int>.EnterExitTest<ValX1<int>>();	
		Gen<double>.EnterExitTest<ValX1<int>>();
		Gen<string>.EnterExitTest<ValX1<int>>(); //offending line
		Gen<object>.EnterExitTest<ValX1<int>>(); //offending line
		Gen<Guid>.EnterExitTest<ValX1<int>>();
		Gen<RefX1<int>>.EnterExitTest<ValX1<int>>(); //offending line
		Gen<RefX1<string>>.EnterExitTest<ValX1<int>>(); //offending line
		Gen<ValX1<string>>.EnterExitTest<ValX1<int>>(); //offending line

		Gen<int>.EnterExitTest<ValX1<string>>();	//offending line
		Gen<double>.EnterExitTest<ValX1<string>>(); //offending line
		Gen<string>.EnterExitTest<ValX1<string>>(); //offending line
		Gen<object>.EnterExitTest<ValX1<string>>(); //offending line
		Gen<Guid>.EnterExitTest<ValX1<string>>(); //offending line
		Gen<RefX1<int>>.EnterExitTest<ValX1<string>>(); //offending line
		Gen<RefX1<string>>.EnterExitTest<ValX1<string>>(); //offending line
		Gen<ValX1<int>>.EnterExitTest<ValX1<string>>(); //offending line
		

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


