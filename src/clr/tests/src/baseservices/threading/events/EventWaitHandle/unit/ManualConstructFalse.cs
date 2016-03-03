using System;
using System.Threading;
using System.Diagnostics;

public class ManualConstructFalse : EventWaitHandleTest
{
	public static int Main()
	{
		return RunTest(new Func<int>(Run));
	}

	public static int Run()
	{
		var sw = new Stopwatch();		
		var ewh = new EventWaitHandle(false, EventResetMode.ManualReset);

		// Should not signal
		if (ewh.TestWaitOne(1000, sw))
			return -1;

		// Should signal
		ewh.Set();
		if (!ewh.TestWaitOne(0, sw))
			return -2;

		// Should not signal
		ewh.Reset();
		if (ewh.TestWaitOne(1000, sw))
			return -3;

		return TestPassed;
	}
}
