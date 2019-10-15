// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;
using System.Diagnostics;

public class AutoConstructTrue : EventWaitHandleTest
{
	public static int Main()
	{
		return RunTest(new Func<int>(Run));
	}

	public static int Run()
	{
		var ewh = new EventWaitHandle(true, EventResetMode.AutoReset);
		var sw = new Stopwatch();
		
		if (!ewh.TestWaitOne(0, null))
			return -1;

		if (ewh.TestWaitOne(1000, sw))
			return -2;

		return TestPassed;
	}
}
