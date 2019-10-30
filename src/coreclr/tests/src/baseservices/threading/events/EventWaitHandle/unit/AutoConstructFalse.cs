// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;
using System.Diagnostics;

public class AutoConstructFalse : EventWaitHandleTest
{
	public static int Main()
	{
		return RunTest(new Func<int>(Run));
	}

	public static int Run()
	{
		var sw = new Stopwatch();
		var ewh = new EventWaitHandle(false, System.Threading.EventResetMode.AutoReset);

		// Should not signal
		if (ewh.TestWaitOne(1000, sw))
			return -1;

		// Should signal
		ewh.Set();
		if (!ewh.TestWaitOne(0, null))
			return -2;

		// Should not signal
		if (ewh.TestWaitOne(1000, sw))
			return -3;
		
		return TestPassed;
	}
}
