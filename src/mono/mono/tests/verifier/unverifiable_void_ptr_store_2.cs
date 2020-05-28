using System;

public unsafe class Driver {
	static int foo;
	public static int Main (string[] args) {
		void * test = (void*)foo;
		return 1;
	}
}
