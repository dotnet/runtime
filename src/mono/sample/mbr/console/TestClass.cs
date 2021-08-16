using System;
using System.Runtime.CompilerServices;

public class TestClass {
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static string TargetMethod () {
		string s = "OLD STRING";
		Console.WriteLine (s);
		return s;
        }
}
