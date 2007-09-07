//
// Test whenever the runtime unwinder can deal with exceptions raised in the method
// prolog or epilog
//
using System;

public class Tests {

	// Async exceptions will be raised while executing this method
	static void foo () {
		for (int i = 0; i < 10; ++i)
			;
	}

	public static int Main (String[] args) {
		try {
			foo ();
		} catch (NullReferenceException ex) {
			return 0;
		}

		return 0;
	}
}
