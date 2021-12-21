using System;
using System.Runtime.CompilerServices;

public class TestClass {
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static string TargetMethod () {
#if false
		var o = new Inner();
		return o.GetIt();
#endif
		return NewFunc (() => "NEW STRING");
        }

	public static string NewFunc(Func<string> f) => f ();

#if false
	public class Inner {
		public string GetIt() => "NEW STRING";
	}
#endif
}
