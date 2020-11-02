using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using MonoEnc;

public class TypeTokenSwap {
	public static int Main (string []args) {
		Assembly assm = typeof (TypeTokenSwap).Assembly;
		var replacer = EncHelper.Make ();

		var res = TargetMethod ();
		if (res != typeof (string))
			return 1;

		replacer.Update (assm);

		res = TargetMethod ();
		if (res != typeof (int))
			return 2;

		return 0;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static Type TargetMethod () {
		return typeof (string);
	}
}

