using System;
using System.Runtime.InteropServices;

public class Test {

	[DllImport ("libtest.so", EntryPoint="mono_test_marshal_delegate_struct")]
	public static extern int mono_test_marshal_delegate_struct (DelegateStruct s);

	public delegate int WndProc (int a);

	public static int test_func (int a)
	{
		return a;
	}
	
	[StructLayout (LayoutKind.Sequential)]
	public struct DelegateStruct {
		public int a;
		public WndProc func;
	}
	
	public unsafe static int Main () {
		DelegateStruct ss = new DelegateStruct ();
		int size = Marshal.SizeOf (typeof (DelegateStruct));
		
		Console.WriteLine ("DelegateStruct:" + size);
		if (size != 8)
			return 1;
		
		ss.a = 123;
		ss.func = new WndProc(test_func);

		if (mono_test_marshal_delegate_struct (ss) != 123)
			return 1;
		
		return 0;
	}
}

