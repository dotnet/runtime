//
// loader.cs:
//
//  Tests for assembly loading
//

using System;
using System.Reflection;

public class Tests {

	public static int Main (string[] args)
	{
		return TestDriver.RunTests (typeof (Tests), args);
	}

	public static int test_0_load_partial_name ()
	{
		if (Assembly.LoadWithPartialName ("mscorlib") == null)
			return 1;
		else
			return 0;
	}
}

		
