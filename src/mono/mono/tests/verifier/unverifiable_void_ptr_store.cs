using System;

public unsafe class Driver {
	static int foo;
	static void * bla;
	public static int Main (string[] args) {
		void * test = (void*)foo;
		bla = test;
		return 1;
	}
}
