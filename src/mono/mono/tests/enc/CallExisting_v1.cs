using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using MonoEnc;

public class CallExisting {
	public static int Main (string []args) {
		Assembly assm = typeof (CallExisting).Assembly;
		var replacer = EncHelper.Make ();

		int res = TargetMethod ();
		if (res != 2)
			return 1;

		replacer.Update (assm);

		res = TargetMethod ();
		if (res != 4)
			return 2;

		return 0;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static int TargetMethod () {
		Console.WriteLine ("Hello new World");
		return 3 + SecondMethod ();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static int SecondMethod () {
		Console.WriteLine ("SecondMethod");
		return 1;
	}
}

