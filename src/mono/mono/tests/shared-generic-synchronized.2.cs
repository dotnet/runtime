//
//  shared-generic-synchronized.2.cs:
//
//    Tests for the 'synchronized' method attribute in shared generic methods
//

using System;
using System.Threading;
using System.Runtime.CompilerServices;

public class Test<T> {

	[MethodImplAttribute(MethodImplOptions.Synchronized)]
	public int test () {
		Monitor.Exit (this);
		Monitor.Enter (this);
		return 2 + 2;
	}

	[MethodImplAttribute(MethodImplOptions.Synchronized)]
	public static int test_static () {
		Monitor.Exit (typeof (Test<T>));
		Monitor.Enter (typeof (Test<T>));
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
}

class main {
	public delegate int Delegate1 ();

	static public int Main (String[] args) {
		Test<string> b = new Test<string> ();
		int res;

		Console.WriteLine ("Test1...");
		b.test ();
		Console.WriteLine ("Test2...");
		Test<string>.test_static ();
		Console.WriteLine ("Test3...");
		try {
			b.test_exception ();
		}
		catch (SynchronizationLockException ex) {
			// OK
		}
		catch (Exception ex) {
			// The other exception should be overwritten by the lock one
			return 1;
		}

		Console.WriteLine ("Test4...");
		b.test_virtual ();

		Console.WriteLine ("Test5...");
		Delegate1 d = new Delegate1 (b.test);
		res = d ();

		Console.WriteLine ("Test6...");
		d = new Delegate1 (Test<string>.test_static);
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
			// OK
		}
		catch (Exception ex) {
			return 2;
		}

		return 0;
	}
}
