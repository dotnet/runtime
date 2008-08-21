//
//  synchronized.cs:
//
//    Tests for the 'synchronized' method attribute
//

using System;
using System.Threading;
using System.Runtime.CompilerServices;

class Tests {

	// We use Monitor.Pulse to test that the object is synchronized

	[MethodImplAttribute(MethodImplOptions.Synchronized)]
	public int test () {
		Monitor.Pulse (this);
		//Monitor.Enter (this);
		return 2 + 2;
	}

	[MethodImplAttribute(MethodImplOptions.Synchronized)]
	public static int test_static () {
		Monitor.Pulse (typeof (Tests));
		return 2 + 2;
	}

	[MethodImplAttribute(MethodImplOptions.Synchronized)]
	public int test_exception () {
		Monitor.Exit (this);
		throw new Exception ("A");
	}

	[MethodImplAttribute(MethodImplOptions.Synchronized)]
	public virtual int test_virtual () {
		Monitor.Pulse (this);
		return 2 + 2;
	}

	public static bool is_synchronized (object o) {
		try {
			Monitor.Pulse (o);
		}
		catch (SynchronizationLockException ex) {
			return false;
		}
		return true;
	}

	public delegate int Delegate1 ();

	static public int Main (String[] args) {
		Tests b = new Tests ();
		int res, err;

		Console.WriteLine ("Test1...");
		b.test ();
		if (is_synchronized (b))
			return 1;

		Console.WriteLine ("Test2...");
		test_static ();
		if (is_synchronized (typeof (Tests)))
			return 1;

		Console.WriteLine ("Test3...");
		try {
			b.test_exception ();
		}
		catch (SynchronizationLockException ex) {
			return 1;
		}
		catch (Exception ex) {
			// OK
		}
		if (is_synchronized (b))
			return 1;

		Console.WriteLine ("Test4...");
		b.test_virtual ();
		if (is_synchronized (b))
			return 1;

		Console.WriteLine ("Test5...");
		Delegate1 d = new Delegate1 (b.test);
		res = d ();
		if (is_synchronized (b))
			return 1;

		Console.WriteLine ("Test6...");
		d = new Delegate1 (test_static);
		res = d ();
		if (is_synchronized (typeof (Tests)))
			return 1;

		Console.WriteLine ("Test7...");
		d = new Delegate1 (b.test_virtual);
		res = d ();
		if (is_synchronized (b))
			return 1;

		Console.WriteLine ("Test8...");
		d = new Delegate1 (b.test_exception);
		try {
			d ();
		}
		catch (SynchronizationLockException ex) {
			return 2;
		}
		catch (Exception ex) {
			// OK
		}
		if (is_synchronized (b))
			return 1;

		return 0;
	}
}
