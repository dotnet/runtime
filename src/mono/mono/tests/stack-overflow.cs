using System;
using System.Collections.Generic;

public class Tests {

	struct A1 {
		public long a1, a2, a3, a4;
	}

	struct A2 {
		public A1 a1, a2, a3, a4;
	}

	struct A3 {
		public A2 a1, a2, a3, a4;
	}

	struct A4 {
		public A3 a1, a2, a3, a4;
	}

	struct A5 {
		public A4 a1, a2, a3, a4;
	}

	public static int foo () {
		A5 a5;

		/* Prevent a5 from being optimized away */
		a5 = new A5 ();
		a5.a1.a1.a1.a1.a1 = 5;

		return foo () + 1;
	}

	// call an icall so we have a big chance to hit the
	// stack overflow in unmanaged code
	static void Recurse () {
		Type t = typeof (Dictionary<,>);
		t.GetGenericArguments ();
		Recurse ();
	}

	public static int Main () {
		// Try overflow in managed code
		int count = 0;
		try {
			foo ();
		}
		catch (StackOverflowException) {
			Console.WriteLine ("Stack overflow caught.");
			count ++;
		}
		if (count != 1)
			return 1;

		// Try overflow in unmanaged code
		count = 0;
		try {
			Recurse ();
		} catch (Exception ex) {
			Console.WriteLine ("Handled: {0}", ex.Message);
			count++;
		}
		// Check that the stack protection is properly restored
		try {
			Recurse ();
		} catch (Exception ex) {
			Console.WriteLine ("Again: {0}", ex.Message);
			count++;
		}
		if (count != 2)
			return 2;

		return 0;
	}
}
