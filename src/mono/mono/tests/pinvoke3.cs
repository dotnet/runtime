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

	public static int delegate_test (SimpleStruct ss)
	{
		Console.WriteLine ("delegate called");
		Console.WriteLine ("A: " + ss.a);
		Console.WriteLine ("B: " + ss.b);
		Console.WriteLine ("C: " + ss.c);
		Console.WriteLine ("D: " + ss.d);
		
		if (!ss.a && ss.b && !ss.c && ss.d == "TEST")
			return 0;
		
		return 1;
	}
	
	[DllImport ("libtest.so", EntryPoint="mono_test_marshal_delegate2")]
	public static extern int mono_test_marshal_delegate2 (SimpleDelegate2 d);

	public delegate int SimpleDelegate2 (SimpleStruct ss);

	public static int Main () {
		
		SimpleDelegate2 d = new SimpleDelegate2 (delegate_test);

		if (mono_test_marshal_delegate2 (d) != 0)
			return 1;
		
		return 0;
	}
}
