using System;
using System.Runtime.InteropServices;

public class Test {

	[DllImport("libtest")]
	[return: MarshalAs(UnmanagedType.LPWStr)]
	private static extern string test_lpwstr_marshal(
		[MarshalAs(UnmanagedType.LPWStr)] string s,
		int length );


	public static int Main () {

		string s = "ABC";
		
		Console.WriteLine(s.Length);
		string res = test_lpwstr_marshal (s, s.Length);

		Console.WriteLine (res);

		if (res != "ABC")
			return 1;
		
		return 0;		
	}
}
