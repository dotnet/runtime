// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using Xunit;


public class Beta 
{
	[Fact]
	public static int TestEntryPoint()
	{
    		int rValue = 100;
		Console.WriteLine("Setup an Infinite Wait with negative value other than -1");
    		Console.WriteLine("This can't be done on WaitAny and WaitAll");
		WaitHandle Waiter;
		Waiter = (WaitHandle) new AutoResetEvent(false);

		try{
			Waiter.WaitOne(-2);
			Console.WriteLine("ERROR -- Enabled a wait with -2");
			rValue = 10;
		}catch(ArgumentOutOfRangeException){}		

		try{
			Waiter.WaitOne(Int32.MinValue);
			Console.WriteLine("ERROR -- Enabled a wait with {0}",Int32.MinValue);
			
			rValue = 20;
		}catch(ArgumentOutOfRangeException){}

		try{
			Waiter.WaitOne(-1000000);
			Console.WriteLine("ERROR -- Enabled a wait with -1000000");			
			rValue = 20;
		}catch(ArgumentOutOfRangeException){}
		
		Console.WriteLine("Test {0}",rValue == 100 ? "Passed":"Failed");
		return rValue;
			
	}
}
