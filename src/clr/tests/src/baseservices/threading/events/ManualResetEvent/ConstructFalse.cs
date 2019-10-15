// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;
using System.Diagnostics;

public class MRETestClass
{

	public static int Main()
	{
		MRETestClass testManualReset = new MRETestClass();
		int ret = testManualReset.Run();
		Console.WriteLine(ret == 100 ? "Test Passed":"Test Failed");
		return ret;
	}

	public int Run()
	{
		Stopwatch sw = new Stopwatch();		
		ManualResetEvent mre = new ManualResetEvent(false);
		sw.Start();
		bool ret = mre.WaitOne(1000);//,false);
		sw.Stop();
		//We should never get signaled
		Console.WriteLine("Expect WaitOne to return False and time-out after 1000 milliseconds waiting for signal.");
		Console.WriteLine("Actual return is: " + ret.ToString());
		Console.WriteLine("Expect Stopwatch to use entire 1000 milliseconds.");
		Console.WriteLine("Actual time taken is: " + sw.ElapsedMilliseconds.ToString());
		Console.WriteLine();
		if(ret || sw.ElapsedMilliseconds < 900)
			return -1;
		Console.WriteLine("Manual Reset Event signalled.");		
		mre.Set();
		ret = mre.WaitOne(0);//,false);
		Console.WriteLine("Expect WaitOne to return True and time-out after 1000 milliseconds waiting for signal.");
		Console.WriteLine("Actual return is: " + ret.ToString());
		Console.WriteLine();		
		if(!ret)			
			return -3;
		mre.Reset();
		sw.Reset();
		sw.Start();
		ret = mre.WaitOne(1000);//,false);
		sw.Stop();
		//We should never get signaled
		Console.WriteLine("Expect WaitOne to return false and time-out after 1000 milliseconds waiting for signal.");
		Console.WriteLine("Actual return is: " + ret.ToString());
		Console.WriteLine("Expect Stopwatch to use entire 1000 milliseconds.");
		Console.WriteLine("Actual time taken is: " + sw.ElapsedMilliseconds.ToString());				
		if(ret || sw.ElapsedMilliseconds < 900)
			return -1;
		
		return 100;
		
	}
}