using System;
using System.Runtime.InteropServices;

class Hello
{
	[DllImport("test", EntryPoint="mono_test_marshal_string_array")]
	static extern void printhello (string [] array);

	public static int Main ()
	{
		// This throws a Null exception.
		// If we use double [] instead of string [] it works.
		printhello (null);
		return 0;
	}
}

