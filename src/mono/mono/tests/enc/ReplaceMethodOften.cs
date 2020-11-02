using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using MonoEnc;

public class Sample3 {
	public static int Main (string []args) {
		Assembly assm = typeof (Sample3).Assembly;
		var replacer = EncHelper.Make ();

		int res = DiffTestMethod1 (3, 8);
		if (res != 0)
			return 0;
		replacer.Update (assm);

		res = DiffTestMethod1 (3, 8);
		if (res != 1)
			return 1;
		replacer.Update (assm);

		res = DiffTestMethod1 (3, 8);
		if (res != 2)
			return 2;
		replacer.Update (assm);

		res = DiffTestMethod1 (3, 8);
		if (res != 3)
			return 3;
		replacer.Update (assm);

		res = DiffTestMethod1 (3, 8);
		if (res != 4)
			return 4;
		replacer.Update (assm);

		res = DiffTestMethod1 (3, 8);
		if (res != 5)
			return 5;
		replacer.Update (assm);

		res = DiffTestMethod1 (3, 8);
		if (res != 6)
			return 6;

		return 0;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static int DiffTestMethod1 (int x, int y) {
		// Console.WriteLine ("Version 0");
		return 0;
	}
}

