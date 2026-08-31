using System;

public unsafe class Driver {
	static void* foo;
	static int bar;

	public static  int Main (string[] args) {
		if ((void*)bar > foo)
			return 1;
		return 0;
	}
}
