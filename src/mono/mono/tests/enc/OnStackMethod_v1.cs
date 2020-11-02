using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using MonoEnc;

public class OnStackMethod {
	public static EncHelper replacer = null;
	public static Assembly assm = null;
	public static int state = 0;

	public static int Main (string []args) {
		assm = typeof (OnStackMethod).Assembly;
		replacer = EncHelper.Make ();

		int res = DiffTestMethod1 ();
		if (res != 1)
			return 1;

		res = DiffTestMethod1 ();
		if (res != 2)
			return 2;

		return 0;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static int DiffTestMethod1 () {
		Console.WriteLine ("Hello new World");
		return 2;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void DoTheUpdate () {
		if (state == 0) {
			replacer.Update (assm);
			state++;
		}
	}
}

