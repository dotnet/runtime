using System;
using System.Runtime.CompilerServices;

public class TestClass {
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static string TargetMethod () {
#if ADD_INNER_INST
		var o = new Inner();
		return o.GetIt();
#endif
#if true
		return NewFunc (() => "NEW STRING");
#endif
#if ADD_MUTUALS
		return ClassOne.F1.s;
#endif
        }

#if true
	public static string NewFunc(Func<string> f) => f ();
#endif

#if ADD_MUTUALS
	public class ClassOne {
		public static ClassTwo F1;
	}

	public class ClassTwo {
		public static ClassOne F2;
		public string s;
	}
#endif

#if ADD_INNER_INST
	public class Inner {
		public string GetIt() => "NEW STRING";
	}
#endif
}
