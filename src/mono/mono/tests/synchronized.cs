//
//  synchronized.cs:
//
//    Tests for the 'synchronized' method attribute
//

using System;
using System.Threading;
using System.Runtime.CompilerServices;

class Test {

	[MethodImplAttribute(MethodImplOptions.Synchronized)]
	public int test () {
		Monitor.Exit (this);
		Monitor.Enter (this);
		return 2 + 2;
	}

	[MethodImplAttribute(MethodImplOptions.Synchronized)]
	public static int test_static () {
		Monitor.Exit (typeof (Test));
		Monitor.Enter (typeof (Test));
		return 2 + 2;
	}

	[MethodImplAttribute(MethodImplOptions.Synchronized)]
	public int test_exception () {
		Monitor.Exit (this);
		throw new Exception ("A");
	}

	[MethodImplAttribute(MethodImplOptions.Synchronized)]
	public virtual int test_virtual () {
		Monitor.Exit (this);
		Monitor.Enter (this);
		return 2 + 2;
	}

	public delegate int Delegate1 ();

	static public int Main (String[] args) {
		Test b = new Test ();
		int res;

		Console.WriteLine ("Test1...");
		b.test ();
		Console.WriteLine ("Test2...");
		test_static ();
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

		Console.WriteLine ("Test4...");
		b.test_virtual ();

		Console.WriteLine ("Test5...");
		Delegate1 d = new Delegate1 (b.test);
		res = d ();

		Console.WriteLine ("Test6...");
		d = new Delegate1 (test_static);
		res = d ();

		Console.WriteLine ("Test7...");
		d = new Delegate1 (b.test_virtual);
		res = d ();

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

		return 0;
	}
}
