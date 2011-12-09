using System;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

/*
 * Regression tests for the AOT/FULL-AOT code.
 */

class Tests {

	static int Main () {
		return TestDriver.RunTests (typeof (Tests));
	}

	public delegate void ArrayDelegate (int[,] arr);

	static int test_0_array_delegate_full_aot () {
		ArrayDelegate d = delegate (int[,] arr) {
		};
		int[,] a = new int[5, 6];
		d.BeginInvoke (a, null, null);
		return 0;
	}
}
