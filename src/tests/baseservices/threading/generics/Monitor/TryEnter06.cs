// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using Xunit;


public struct ValX1<T> {}
public class RefX1<T> {}

struct Gen<T> 
{

	public static void TryEnterTest<U>(bool TisU)
	{
		Type monitorT = typeof(Gen<T>);
		Type monitorU = typeof(Gen<U>);

		TestHelper myHelper = new TestHelper(Test_TryEnter06.nThreads);
		TestHelper myHelper2 = new TestHelper(Test_TryEnter06.nThreads);
		WaitHandle[] myWaiter = new WaitHandle[2];
		myWaiter[0] = myHelper.m_Event;
		myWaiter[1] = myHelper2.m_Event;

		for (int i=0; i<Test_TryEnter06.nThreads; i++)
		{
			// 	new MonitorDelegateTS(myHelper.ConsumerTryEnter).BeginInvoke(monitorT,100,null,null);
			// 	new MonitorDelegateTS(myHelper2.ConsumerTryEnter).BeginInvoke(monitorU,100,null,null);
			ThreadPool.QueueUserWorkItem(state =>
			{
				myHelper.ConsumerTryEnter(monitorT, 100);
			});

			ThreadPool.QueueUserWorkItem(state =>
			{
				myHelper2.ConsumerTryEnter(monitorU, 100);
			});
		}

		for(int i=0;i<6;i++)
		{	
			if(WaitHandle.WaitAll(myWaiter,10000))//,true))
				break;
			if(myHelper.Error == true || myHelper2.Error == true)
				break;
		}
		Test_TryEnter06.Eval(!(myHelper.Error || myHelper2.Error));
		
	}


	
}

public class Test_TryEnter06
{
	public static int nThreads = 25;
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
		Gen<int>.TryEnterTest<int>(true);	
		Gen<double>.TryEnterTest<int>(false);
		Gen<string>.TryEnterTest<int>(false);
		Gen<object>.TryEnterTest<int>(false);
		Gen<Guid>.TryEnterTest<int>(false);
		Gen<RefX1<int>>.TryEnterTest<int>(false);
		Gen<RefX1<string>>.TryEnterTest<int>(false);
		Gen<ValX1<int>>.TryEnterTest<int>(false);
		Gen<ValX1<string>>.TryEnterTest<int>(false);
		
		Gen<int>.TryEnterTest<double>(false);	
		Gen<double>.TryEnterTest<double>(true);
		Gen<string>.TryEnterTest<double>(false);
		Gen<object>.TryEnterTest<double>(false);
		Gen<Guid>.TryEnterTest<double>(false);
		Gen<RefX1<int>>.TryEnterTest<double>(false);
		Gen<RefX1<string>>.TryEnterTest<double>(false);
		Gen<ValX1<int>>.TryEnterTest<double>(false);
		Gen<ValX1<string>>.TryEnterTest<double>(false);

		Gen<int>.TryEnterTest<string>(false);	
		Gen<double>.TryEnterTest<string>(false);
		Gen<string>.TryEnterTest<string>(true);
		Gen<object>.TryEnterTest<string>(false);
		Gen<Guid>.TryEnterTest<string>(false);
		Gen<RefX1<int>>.TryEnterTest<string>(false);
		Gen<RefX1<string>>.TryEnterTest<string>(false);
		Gen<ValX1<int>>.TryEnterTest<string>(false);
		Gen<ValX1<string>>.TryEnterTest<string>(false);

		Gen<int>.TryEnterTest<object>(false);	
		Gen<double>.TryEnterTest<object>(false);
		Gen<string>.TryEnterTest<object>(false);
		Gen<object>.TryEnterTest<object>(true);
		Gen<Guid>.TryEnterTest<object>(false);
		Gen<RefX1<int>>.TryEnterTest<object>(false);
		Gen<RefX1<string>>.TryEnterTest<object>(false);
		Gen<ValX1<int>>.TryEnterTest<object>(false);
		Gen<ValX1<string>>.TryEnterTest<object>(false);

		Gen<int>.TryEnterTest<Guid>(false);	
		Gen<double>.TryEnterTest<Guid>(false);
		Gen<string>.TryEnterTest<Guid>(false);
		Gen<object>.TryEnterTest<Guid>(false);
		Gen<Guid>.TryEnterTest<Guid>(true);
		Gen<RefX1<int>>.TryEnterTest<Guid>(false);
		Gen<RefX1<string>>.TryEnterTest<Guid>(false);
		Gen<ValX1<int>>.TryEnterTest<Guid>(false);
		Gen<ValX1<string>>.TryEnterTest<Guid>(false);

		Gen<int>.TryEnterTest<RefX1<int>>(false);	
		Gen<double>.TryEnterTest<RefX1<int>>(false);
		Gen<string>.TryEnterTest<RefX1<int>>(false);
		Gen<object>.TryEnterTest<RefX1<int>>(false);
		Gen<Guid>.TryEnterTest<RefX1<int>>(false);
		Gen<RefX1<int>>.TryEnterTest<RefX1<int>>(true);
		Gen<RefX1<string>>.TryEnterTest<RefX1<int>>(false);
		Gen<ValX1<int>>.TryEnterTest<RefX1<int>>(false);
		Gen<ValX1<string>>.TryEnterTest<RefX1<int>>(false);	

		Gen<int>.TryEnterTest<RefX1<string>>(false);	
		Gen<double>.TryEnterTest<RefX1<string>>(false);
		Gen<string>.TryEnterTest<RefX1<string>>(false);
		Gen<object>.TryEnterTest<RefX1<string>>(false);
		Gen<Guid>.TryEnterTest<RefX1<string>>(false);
		Gen<RefX1<int>>.TryEnterTest<RefX1<string>>(false);
		Gen<RefX1<string>>.TryEnterTest<RefX1<string>>(true);
		Gen<ValX1<int>>.TryEnterTest<RefX1<string>>(false);
		Gen<ValX1<string>>.TryEnterTest<RefX1<string>>(false);

		Gen<int>.TryEnterTest<ValX1<int>>(false);	
		Gen<double>.TryEnterTest<ValX1<int>>(false);
		Gen<string>.TryEnterTest<ValX1<int>>(false); //offending line
		Gen<object>.TryEnterTest<ValX1<int>>(false); //offending line
		Gen<Guid>.TryEnterTest<ValX1<int>>(false);
		Gen<RefX1<int>>.TryEnterTest<ValX1<int>>(false); //offending line
		Gen<RefX1<string>>.TryEnterTest<ValX1<int>>(false); //offending line
		Gen<ValX1<int>>.TryEnterTest<ValX1<int>>(true);
		Gen<ValX1<string>>.TryEnterTest<ValX1<int>>(false); //offending line

		Gen<int>.TryEnterTest<ValX1<string>>(false);	//offending line
		Gen<double>.TryEnterTest<ValX1<string>>(false); //offending line
		Gen<string>.TryEnterTest<ValX1<string>>(false); //offending line
		Gen<object>.TryEnterTest<ValX1<string>>(false); //offending line
		Gen<Guid>.TryEnterTest<ValX1<string>>(false); //offending line
		Gen<RefX1<int>>.TryEnterTest<ValX1<string>>(false); //offending line
		Gen<RefX1<string>>.TryEnterTest<ValX1<string>>(false); //offending line
		Gen<ValX1<int>>.TryEnterTest<ValX1<string>>(false); //offending line
		Gen<ValX1<string>>.TryEnterTest<ValX1<string>>(true); //offending line

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


