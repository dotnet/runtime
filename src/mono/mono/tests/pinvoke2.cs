using System;
using System.Runtime.InteropServices;

public class Test {

	public static int delegate_test (int a)
	{
		Console.WriteLine ("Delegate: " + a);
		
		if (a == 2)
			return 0;

		return 1;
	}
	
	[StructLayout (LayoutKind.Sequential)]
	public struct SimpleStruct {
		public bool a;
		public bool b;
		public bool c;
		public string d;
	}

	[DllImport ("libtest.so", EntryPoint="mono_test_marshal_char")]
	public static extern int mono_test_marshal_char (char a1);

	[DllImport ("libtest.so", EntryPoint="mono_test_marshal_array")]
	public static extern int mono_test_marshal_array (int [] a1);
	
	[DllImport ("libtest.so", EntryPoint="mono_test_marshal_struct")]
	public static extern int mono_test_marshal_struct (SimpleStruct ss);

	[DllImport ("libtest.so", EntryPoint="mono_test_marshal_delegate")]
	public static extern int mono_test_marshal_delegate (SimpleDelegate d);

	public delegate int SimpleDelegate (int a);

	public static int Main () {
		if (mono_test_marshal_char ('a') != 0)
			return 1;

		int [] a1 = new int [50];
		for (int i = 0; i < 50; i++)
			a1 [i] = i;

		if (mono_test_marshal_array (a1) != 1225)
			return 2;

		SimpleStruct ss = new  SimpleStruct ();
		ss.b = true;
		ss.d = "TEST";
		mono_test_marshal_struct (ss);
		
		SimpleDelegate d = new SimpleDelegate (delegate_test);

		if (mono_test_marshal_delegate (d) != 0)
			return 1;
		
		return 0;
	}
}
