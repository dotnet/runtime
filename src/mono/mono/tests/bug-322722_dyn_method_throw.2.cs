using System;
using System.Reflection;
using System.Reflection.Emit;

public class MyException : Exception {

}

class Driver {
	public static int Main()
	{
		DynamicMethod method_builder = new DynamicMethod ("ThrowException" , typeof (void), new Type[0], typeof (Driver));
		ILGenerator ilg = method_builder.GetILGenerator ();


		ilg.Emit (OpCodes.Newobj,  typeof (MyException).GetConstructor (new Type[0]));
		ilg.Emit (OpCodes.Throw);

		try {
			method_builder.Invoke (null, null);
			return 2;
		} catch (TargetInvocationException tie) {
			if(! (tie.InnerException is MyException))
				return 3;
		}

		return 0;
	}
}
