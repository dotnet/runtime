using System;
using System.Runtime.InteropServices;

public class Test {

	[DllImport ("libtest", EntryPoint="mono_test_return_string")]
	public static extern String mono_test_return_string (ReturnStringDelegate d);

	public static String managed_return_string (String s) {

		Console.WriteLine ("delegate called: " + s);
		if (s != "TEST")
			return "";
		else
			return "12345";
	}

	public delegate String ReturnStringDelegate (String s);

	public static int Main () {
		ReturnStringDelegate d = new ReturnStringDelegate (managed_return_string);
		String s = mono_test_return_string (d);

		Console.WriteLine ("Received: " + s);

		if (s == "12345")
			return 0;
		
		return 1;
	}
}
