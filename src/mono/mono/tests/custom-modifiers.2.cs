//
// This should be part of the mscorlib tests but there is no way to generate custom
// modifiers with C#.
//
using System;
using System.Reflection;

public class Tests {

	public static int Main () {
		Type[] arr;

		arr = typeof (CustomModifiers).GetField ("field_1").GetRequiredCustomModifiers ();
		if (arr.Length != 1)
			return 1;
		if (arr [0] != typeof (System.Runtime.CompilerServices.IsBoxed))
			return 2;

		arr = typeof (CustomModifiers).GetField ("field_1").GetOptionalCustomModifiers ();
		if (arr.Length != 0)
			return 3;

		arr = typeof (CustomModifiers).GetField ("field_2").GetRequiredCustomModifiers ();
		if (arr.Length != 0)
			return 4;

		arr = typeof (CustomModifiers).GetField ("field_2").GetOptionalCustomModifiers ();
		if (arr.Length != 1)
			return 5;
		if (arr [0] != typeof (System.Runtime.CompilerServices.IsVolatile))
			return 6;
		
		return 0;
	}
}
