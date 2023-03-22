// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Xunit;


public class Test_13662_b {

	[Fact]
	public static int TestEntryPoint(){

		int rValue = 100;
		Timer[] tArray = new Timer[1000];
			for(int i = 0;i<tArray.Length;i++)
				tArray[i] = new Timer(new TimerCallback(TFunc),0,1000,100000);		

			Thread.Sleep(1000);
			GC.Collect();
			GC.WaitForPendingFinalizers();
		for(int i = 0;i<tArray.Length;i++)
				tArray[i] = new Timer(new TimerCallback(TFunc),0,1000,100000);		

			Thread.Sleep(1000);
			GC.Collect();
			GC.WaitForPendingFinalizers();
		for(int i = 0;i<tArray.Length;i++)
				tArray[i] = new Timer(new TimerCallback(TFunc),0,1000,100000);		

			Thread.Sleep(1000);
			GC.Collect();
			GC.WaitForPendingFinalizers();
		for(int i = 0;i<tArray.Length;i++)
				tArray[i] = new Timer(new TimerCallback(TFunc),0,1000,100000);		

			Thread.Sleep(1000);
			GC.Collect();
			GC.WaitForPendingFinalizers();
		for(int i = 0;i<tArray.Length;i++)
				tArray[i] = new Timer(new TimerCallback(TFunc),0,1000,100000);		

			Thread.Sleep(1000);
			GC.Collect();
			GC.WaitForPendingFinalizers();
		for(int i = 0;i<tArray.Length;i++)
				tArray[i] = new Timer(new TimerCallback(TFunc),0,1000,100000);		

			Thread.Sleep(1000);
			GC.Collect();
			GC.WaitForPendingFinalizers();
		for(int i = 0;i<tArray.Length;i++)
				tArray[i] = new Timer(new TimerCallback(TFunc),0,1000,100000);		

			Thread.Sleep(1000);
			GC.Collect();
			GC.WaitForPendingFinalizers();
		for(int i = 0;i<tArray.Length;i++)
				tArray[i] = new Timer(new TimerCallback(TFunc),0,1000,100000);		

			Thread.Sleep(1000);
			GC.Collect();
			GC.WaitForPendingFinalizers();
		for(int i = 0;i<tArray.Length;i++)
				tArray[i] = new Timer(new TimerCallback(TFunc),0,1000,100000);		

			Thread.Sleep(1000);
			GC.Collect();
			GC.WaitForPendingFinalizers();
		for(int i = 0;i<tArray.Length;i++)
				tArray[i] = new Timer(new TimerCallback(TFunc),0,1000,100000);		

			Thread.Sleep(1000);
			GC.Collect();
			GC.WaitForPendingFinalizers();
		for(int i = 0;i<tArray.Length;i++)
				tArray[i] = new Timer(new TimerCallback(TFunc),0,1000,100000);		

			Thread.Sleep(1000);
			GC.Collect();
			GC.WaitForPendingFinalizers();
		
		return rValue;
	}

	public static void TFunc(Object o)
	{
		Thread.Sleep(1);
	}
}
