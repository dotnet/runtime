//
// thread-2.cs:
//
//  Tests for net 2.0 thread features
//

using System;
using System.Threading;

public class Tests
{
	public static int Main () {
		return TestDriver.RunTests (typeof (Tests));
	}

	public static bool started = false;

	public static void Start (object o) {
		started = true;
	}

	public static int test_0_parameterized_thread_start () {
		Thread t = new Thread (new ParameterizedThreadStart (Start));
		t.Start ("AB");

		return started ? 0 : 1;
	}
}
