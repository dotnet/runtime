using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using MonoEnc;

public class Sample3 {
	public static int Main (string []args) {
		Assembly assm = typeof (Sample3).Assembly;
		var replacer = EncHelper.Make ();

		int res = DiffTestMethod1 (3, 8);
		if (res != (3 + 1))
			return 1;

		replacer.Update (assm);

		res = DiffTestMethod1 (3, 8);
		if (res != (8 + 2))
			return 2;

		return 0;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static int DiffTestMethod1 (int x, int y) {
		return y + SecondMethod ();;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static int SecondMethod () {
		Console.WriteLine ("HELLO NEW WORLD");
		return 2;
	}
}
