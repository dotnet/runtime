// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;

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
		ManualResetEvent mre = new ManualResetEvent(true);
		
		if(!mre.WaitOne(0))//,false)) //are.WaitOne returns true if signaled
			return -1;
		mre.Reset();
		if(mre.WaitOne(1000))//,false))
			return -3;
		mre.Set();		
		if(mre.WaitOne(0))//,false))
			return 100;
		
		Console.WriteLine("ManualResetEvent Broken");
		return -3;
		
	}
}
