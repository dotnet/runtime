using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

public class Test
{
	public int test (IEqualityComparer comparer) {
		int k = 0;
		switch (k) {
		case 0:
			return comparer.GetHashCode(null);
		case 1:
			return test2(comparer.GetHashCode(null), comparer.GetHashCode(null));
		}
		return -1;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	internal static int test2 (int h1, int h2) {
		return 0;
	}

	[Fact]
	public static int TestEntryPoint() {
		try {
			var t = new Test ();
			t.test (null);
		} catch (NullReferenceException) {
			return 100;
		}
		return 101;
	}
}
