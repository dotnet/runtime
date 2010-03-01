/*
 * Test case for my patch:
 *
 * 2010-02-26  Martin Baulig  <martin@ximian.com>
 *
 *	* class-internals.h (MonoVTable): Added `init_aborted'.
 *
 *	* object.c
 *	(mono_runtime_class_init_full): Set `vtable->init_aborted' in
 *	addition to `vtable->init_failed' if we hit a `ThreadAbortException'
 *	while running the class .cctor and reset `init_failed' next time
 *	we're called.
 *
 */
using System;
using System.Threading;
using System.Reflection;

public class X
{
	static bool first = true;

	static X ()
	{
		Thread.Sleep (100);
		Console.WriteLine ("X.cctor: {0}", first);

		/*
		 * The .cctor throws a ThreadAbortException when it's run
		 * the first time.
		 *
		 * Without my patch, this makes the class unusable.
		 */

		if (first) {
			first = false;
			Thread.CurrentThread.Abort ();
		}
	}

	public static void Test ()
	{ }
}

public class Y
{
	static int Main (string[] args)
	{
		return TestDriver.RunTests (typeof (Y), args);
	}

	static int Test ()
	{
		Type t = typeof (X);
		MethodInfo m = t.GetMethod ("Test");

		bool aborted = false;

		try {
			m.Invoke (null, new object [0]);
		} catch (TargetInvocationException) {
			aborted = true;
		}

		if (!aborted)
			return 11;

		X.Test ();
		return 0;
	}
}
