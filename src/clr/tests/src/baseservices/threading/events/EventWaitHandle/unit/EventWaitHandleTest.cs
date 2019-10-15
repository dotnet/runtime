// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;
using System.Diagnostics;

public class EventWaitHandleTest
{
	public const int TestPassed = 100; // Returned by Run() on success

	public static int RunTest(Func<int> test) {
		var rv = test();
		Console.WriteLine(rv == TestPassed ? "Test passed" : "Test failed");
		return rv;
	}

	public static bool TryOpenExisting<Expected>(string name) where Expected : Exception
	{
		EventWaitHandle ewh;
		return TryOpenExisting<Expected>(name, out ewh);
	}

	public static bool TryOpenExisting<Expected>(string name, out EventWaitHandle ewh) where Expected : Exception
	{
		ewh = null;
		try {
			ewh = EventWaitHandle.OpenExisting(name);
		} catch (Expected) {
		} catch (Exception ne) {
			Console.WriteLine("Caught unexpected exception: {0}", ne);
			return false;
		}
		return true;
	}

	public class NoException : Exception {}
}

public static class EventWaitHandleExtensions
{
	const int FudgeFactor = 100; // Account for timing uncertainties

	public static bool TestWaitOne(this EventWaitHandle ewh, int timeout, Stopwatch sw)
	{
		if (timeout == 0)
			return ewh.WaitOne();

		if (sw == null)
			sw = new Stopwatch();
		else
			sw.Reset();

		sw.Start();
		bool signaled = ewh.WaitOne(timeout);
		sw.Stop();

		return signaled || sw.ElapsedMilliseconds < timeout - FudgeFactor;
	}
}
