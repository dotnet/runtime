using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

public class MonoPInvokeCallbackAttribute : Attribute {
	public MonoPInvokeCallbackAttribute (Type delegateType) { }
}

public class T {
	private static bool fired;
	
	[MonoPInvokeCallback (typeof (Action))]
	private static void CallBack ()
	{
		Console.WriteLine ("Called back");
		fired = true;
	}
	
	[DllImport ("proftest_pinvokes", EntryPoint="test_reverse_pinvoke")]
	private static extern void test_reverse_pinvoke (Action cb);


	public static int Main ()
	{
		Helper ();
		if (fired)
			return 0;
		else
			return 1;
	}


	[MethodImpl (MethodImplOptions.NoInlining)]
	private static void Helper ()
	{
		test_reverse_pinvoke (new Action (CallBack));
	}
}
