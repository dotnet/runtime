using System;
using System.Runtime.InteropServices;

public class Test {

	[StructLayout (LayoutKind.Sequential)]
	public struct SimpleStruct {
		public bool a;
		public bool b;
		public bool c;
		public string d;
	}

	[DllImport ("libtest.so", EntryPoint="mono_test_ref_vtype")]
	public static extern int mono_test_ref_vtype (int a, ref SimpleStruct ss, int b, TestDelegate d);

	public static int managed_test_ref_vtype (int a, ref SimpleStruct ss, int b)
	{
		Console.WriteLine ("Delegate called");
		
		if (a == 1 && b == 2 && ss.a && !ss.b && ss.c && ss.d == "TEST2")
			return 0;

		return 1;
	}

	public delegate int TestDelegate (int a, ref SimpleStruct ss, int b);


	public static int Main () {
		SimpleStruct ss = new SimpleStruct ();
		TestDelegate d = new TestDelegate (managed_test_ref_vtype);
		
		ss.b = true;
		ss.d = "TEST1";

		if (mono_test_ref_vtype (1, ref ss, 2, d) != 0)
			return 1;
		
		if (ss.a && !ss.b && ss.c && ss.d == "TEST2")
			return 0;
		
		return 1;
	}
}
