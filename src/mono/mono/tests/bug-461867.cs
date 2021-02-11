using System;
using System.Runtime.InteropServices;

[AttributeUsage (AttributeTargets.Method)]
sealed class MonoPInvokeCallbackAttribute : Attribute {
	public MonoPInvokeCallbackAttribute (Type t) {}
}

namespace TestApp
{
	public delegate char MyDelegate(int x);

    class Driver
    {
		[MonoPInvokeCallbackAttribute (typeof (MyDelegate))]
		static char Test (int x) { return (char)x; }

		static int Main()
		{
			MyDelegate m = Driver.Test;
			Marshal.GetFunctionPointerForDelegate (m);
			return 0;
		}
	}
}
