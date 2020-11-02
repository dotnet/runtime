using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using MonoEnc;

public class UserStringSwap {
	public static int Main (string []args) {
		Assembly assm = typeof (UserStringSwap).Assembly;
		var replacer = EncHelper.Make ();

		string res = TargetMethod ();
		if (res != "OLD STRING")
			return 1;

		replacer.Update (assm);

		res = TargetMethod ();
		if (res != "NEW STRING")
			return 2;

		return 0;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static string TargetMethod () {
		string s = "NEW STRING";
		Console.WriteLine (s);
		return s;
	}
}

