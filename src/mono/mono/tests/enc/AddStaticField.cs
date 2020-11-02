using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using MonoEnc;

public class AddStaticField {
	public static int field1 = 1;

	public static int Main (string []args) {
		Assembly assm = typeof (AddStaticField).Assembly;
		var replacer = EncHelper.Make ();

		if (AddStaticField.field1 != 1)
			return 1;

		try {
			replacer.Update (assm);
			/* doesn't work yet, thus should throw exception */
			return 2;
		} catch (TargetInvocationException e) {
			Console.WriteLine ("e: " + e);
#if false
			if (e.InnerException.Message.Contains ("cannot add new class"))
				return 0;
#endif
		}
		return 3;
	}
}
