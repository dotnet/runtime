using System;
using System.Text;
using System.Runtime.InteropServices;

public class Test {


	[DllImport ("libtest.so", EntryPoint="mono_test_marshal_stringbuilder")]
	public static extern void mono_test_marshal_stringbuilder (StringBuilder sb, int len);

	public static int Main () {
			
		StringBuilder sb = new StringBuilder(255);
		mono_test_marshal_stringbuilder (sb, sb.Capacity);
		Console.Write ("name: ");
		Console.WriteLine (sb.ToString());

		return 0;
	}
}
