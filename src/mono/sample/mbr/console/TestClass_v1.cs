using System;
using System.Runtime.CompilerServices;

public class TestClass {
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static string TargetMethod () {
		var o = new Inner();
		return o.GetIt();
        }

	public class Inner {
		public string GetIt() => "NEW STRING";
	}
}
