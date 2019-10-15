// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;
using System.Diagnostics;

public class ARETestClass
{

	public static int Main()
	{
		ARETestClass testAutoReset = new ARETestClass();
		int ret = testAutoReset.Run();
		Console.WriteLine(ret == 100 ? "Test Passed":"Test Failed");
		return ret;
	}

	public int Run()
	{
		Stopwatch sw = new Stopwatch();		
		AutoResetEvent are = new AutoResetEvent(false);
		sw.Start();
		bool ret = are.WaitOne(1000);//,false);
		sw.Stop();
		//We should never get signaled
		if(ret)
		{
			Console.WriteLine("AutoResetEvent should never be signalled.");
			return -1;
		}

		if(sw.ElapsedMilliseconds < 900)
		{
			Console.WriteLine("It should take at least 900 milliseconds to call bool ret = are.WaitOne(1000,false);.");
			Console.WriteLine("sw.ElapsedMilliseconds = " + sw.ElapsedMilliseconds);			
			return -2;
		}
		
		are.Set();
		if(!are.WaitOne(0))//,false))
		{
			Console.WriteLine("Signalled event should always return true on call to !are.WaitOne(0,false).");
			return -3;
		}
		
		sw.Reset();		
		sw.Start();
		ret = are.WaitOne(1000);//,false);
		sw.Stop();
		//We should never get signaled
		if(ret)
		{
			Console.WriteLine("AutoResetEvent should never be signalled after is is AutoReset.");
			return -4;
		}

		if(sw.ElapsedMilliseconds < 900)
		{
			Console.WriteLine("It should take at least 900 milliseconds to call bool ret = are.WaitOne(1000,false);.");
			Console.WriteLine("sw.ElapsedMilliseconds = " + sw.ElapsedMilliseconds);			
			return -5;
		}
		
		return 100;
		
	}
}
