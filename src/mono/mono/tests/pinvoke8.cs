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

	[DllImport ("libtest", EntryPoint="mono_test_return_vtype2")]
	public static extern SimpleStruct mono_test_return_vtype2 (ReturnVTypeDelegate d);

	public static SimpleStruct managed_return_vtype (SimpleStruct ss) {
		SimpleStruct res;

		Console.WriteLine ("delegate called");
		Console.WriteLine ("A: " + ss.a);
		Console.WriteLine ("B: " + ss.b);
		Console.WriteLine ("C: " + ss.c);
		Console.WriteLine ("D: " + ss.d);

		res.a = !ss.a;
		res.b = !ss.b;
		res.c = !ss.c;
		res.d = "TEST5";

		return res;
	}

	public delegate SimpleStruct ReturnVTypeDelegate (SimpleStruct ss);

	public static int Main () {
		ReturnVTypeDelegate d = new ReturnVTypeDelegate (managed_return_vtype);
		SimpleStruct ss = mono_test_return_vtype2 (d);

		Console.WriteLine ("A: " + ss.a);
		Console.WriteLine ("B: " + ss.b);
		Console.WriteLine ("C: " + ss.c);
		Console.WriteLine ("D: " + ss.d);

		if (!ss.a && ss.b && !ss.c && ss.d == "TEST5")
			return 0;
		
		return 1;
	}
}
