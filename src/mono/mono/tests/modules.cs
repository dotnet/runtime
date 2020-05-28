//
// modules.cs:
//
//  Tests for netmodules
//

using System;

public class Tests
{
	public static int Main (string[] args) {
		return TestDriver.RunTests (typeof (Tests), args);
	}

	public static int test_0_gettype_nonpublic () {
		if (typeof (Tests).Assembly.GetType ("Foo+Bar") != null)
			return 0;
		else
			return 1;
	}
}
