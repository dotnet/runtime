// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Xunit;


public class Test_30032 {

	[Fact]
	public static int TestEntryPoint(){

		int rValue = 100;
		Timer[] tArray = new Timer[100];
		int val = 0;
		while(val < 10){
			try{
				Interlocked.Increment(ref val);
				Console.WriteLine("Loop {0}",val);
				for(int i = 0;i<tArray.Length;i++)
					tArray[i] = new Timer(new TimerCallback(TFunc),0,1000,100000000);		

				Thread.Sleep(1000);
				GC.Collect();
				GC.WaitForPendingFinalizers();
			}
			catch(Exception e){
				Console.WriteLine(e.ToString());
				rValue = -1;
			}
		}
		Console.WriteLine("Test {0}",100 == rValue ? "Passed":"Failed");		
		return rValue;
	}

	public static void TFunc(Object o)
	{
		Thread.Sleep(1);
	}
}
