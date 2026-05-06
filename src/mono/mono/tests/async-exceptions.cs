//
// Test whenever the runtime unwinder can deal with exceptions raised in the method
// prolog or epilog
//
using System;

public class Tests {

	// Async exceptions will be raised while executing this method
	// On amd64, this doesn't have a frame pointer
	static void foo () {
		for (int i = 0; i < 10; ++i)
			;
	}

	// This does have a frame pointer
	static void bar () {
		for (int i = 0; i < 10; ++i)
			;

		try {
		} catch {
		}
	}

	public static int Main (String[] args) {
		try {
			foo ();
		} catch (NullReferenceException ex) {
		}

		try {
			bar ();
		} catch (NullReferenceException ex) {
		}

		return 0;
	}
}
