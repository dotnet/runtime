using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using MonoEnc;

public class AddClass {
	public static int Main (string []args) {
		Assembly assm = typeof (AddClass).Assembly;
		var replacer = EncHelper.Make ();

		var secondClassType = ReplaceMe ();
		if (secondClassType != typeof (AddClass))
			return 1;

		try {
			replacer.Update (assm);
			/* doesn't work yet, thus should throw exception */
			return 2;
		} catch (TargetInvocationException e) {
			Console.WriteLine ("e: " + e);
			if (e.InnerException.Message.Contains ("cannot add new class"))
				return 0;
		}
		/* we should not get here, yet */
		secondClassType = ReplaceMe ();
		if (secondClassType != typeof (AddClass)) {
			/* would be expected if it would work */
			return 4;
		}
		return 3;
	}

	public static System.Type ReplaceMe () {
		return typeof (AddClass);
	}
}
