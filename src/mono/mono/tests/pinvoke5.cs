using System;
using System.Text;
using System.Runtime.InteropServices;

public class Test {


	[DllImport ("libtest", EntryPoint="mono_test_marshal_stringbuilder")]
	public static extern void mono_test_marshal_stringbuilder (StringBuilder sb, int len);

	public static int Main () {
		StringBuilder sb = new StringBuilder(255);
		mono_test_marshal_stringbuilder (sb, sb.Capacity);
		Console.Write ("name: ");
		String res = sb.ToString();
		Console.WriteLine (res);

		if (res != "This is my message.  Isn't it nice?")
			return 1;  
		
		return 0;
	}
}
