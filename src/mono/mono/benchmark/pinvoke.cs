using System;
using System.Runtime.InteropServices;

public class Test {

	public static int delegate_test (int a)
	{
		if (a == 2)
			return 0;

		return 1;
	}
	
	[DllImport ("libtest", EntryPoint="mono_test_empty_pinvoke")]
	public static extern int mono_test_empty_pinvoke (int i);

	public static int Main (String[] args) {
		int repeat = 1;
				
		if (args.Length == 1)
			repeat = Convert.ToInt32 (args [0]);
		
		Console.WriteLine ("Repeat = " + repeat);

		for (int i = 0; i < (repeat * 5000); i++)
			for (int j = 0; j < 10000; j++)
				mono_test_empty_pinvoke (5);
		
		return 0;
	}
}
