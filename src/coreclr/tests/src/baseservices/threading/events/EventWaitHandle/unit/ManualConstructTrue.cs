// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;

public class ManualConstructTrue : EventWaitHandleTest
{
	public static int Main()
	{
		return RunTest(new Func<int>(Run));
	}

	public static int Run()
	{
		var ewh = new EventWaitHandle(true, EventResetMode.ManualReset);

		// Should signal
		if (!ewh.TestWaitOne(0, null))
			return -1;

		// Should not signal
		ewh.Reset();
		if (ewh.TestWaitOne(1000, null))
			return -2;

		// Should signal
		ewh.Set();
		if (!ewh.TestWaitOne(0, null))
			return -3;

		return TestPassed;		
	}
}
