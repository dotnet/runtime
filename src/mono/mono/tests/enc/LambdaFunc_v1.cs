using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using MonoEnc;

public class LambdaFunc {
	public delegate int TestDelegate (int a);

	public static int Main (string []args) {
		Assembly assm = typeof (LambdaFunc).Assembly;
		var replacer = EncHelper.Make ();

		TestDelegate del = CreateDelegate ();

		int res = del (1);
		if (res != 2) {
			Console.WriteLine ("#1: " + res);
			return 1;
		}

		replacer.Update (assm);

		res = del (1);
		if (res != 3) {
			Console.WriteLine ("#2: " + res);
			return 2;
		}

		return 0;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static TestDelegate CreateDelegate () {
		TestDelegate ret = delegate (int a) { return a + 2; };
		return ret;
	}
}

