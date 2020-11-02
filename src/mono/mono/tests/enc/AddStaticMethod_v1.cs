using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using MonoEnc;

public class AddStaticMethod {
	public static int Main (string []args) {
		Assembly assm = typeof (AddStaticMethod).Assembly;
		var replacer = EncHelper.Make ();

		int res = TargetMethod ();
		if (res != 1)
			return 1;

		try {
			replacer.Update (assm);
			/* doesn't work yet, thus should throw exception */
		} catch (TargetInvocationException e) {
			if (e.InnerException.Message.Contains ("cannot add new method"))
				return 0;
		}
		return 3;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static int TargetMethod () {
		return NewMethod ();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static int NewMethod () {
		Console.WriteLine ("Hello NEW World");
		return 2;
	}
}

